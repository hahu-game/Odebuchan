using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーのアバターであるうーぴょんのステータスをネットワーク同期する
/// </summary>
// Trigger recompilation
public class UyopyonState : NetworkBehaviour
{
    // === 既存プロパティ ===
    [Networked]
    public PlayerRef OwnerPlayer { get; set; }

    [Networked]
    public int Weight { get; set; }

    [Networked]
    public int Energy { get; set; }

    [Networked]
    public byte CurrentStatus { get; set; } = 0;

    // === 新規追加プロパティ ===

    /// <summary>
    /// 状態異常フラグ（4種類）
    /// [0] = SleepApnea (睡眠時無呼吸症候群)
    /// [1] = Diabetes (糖尿病)
    /// [2] = BackPain (腰痛)
    /// [3] = Heatstroke (熱中症)
    /// </summary>
    [Networked, Capacity(4)]
    public NetworkArray<byte> StatusAilments { get; }

    /// <summary>
    /// あそぶによる重さバフ（永久バフ）
    /// </summary>
    [Networked]
    public int PlayBuffWeight { get; set; } = 0;

    /// <summary>
    /// あそぶによる元気バフ（永久バフ）
    /// </summary>
    [Networked]
    public int PlayBuffEnergy { get; set; } = 0;

    /// <summary>
    /// べんきょうの連続回数（べんきょう以外を選択するとリセット）
    /// </summary>
    [Networked]
    public int StudyCombo { get; set; } = 0;

    /// <summary>
    /// 選択した特殊能力の名前（進化後に設定）
    /// </summary>
    [Networked]
    public NetworkString<_16> SpecialAbilityName { get; set; }

    /// <summary>
    /// 進化済みフラグ（重さ200kgで進化）
    /// </summary>
    [Networked]
    public bool HasEvolved { get; set; } = false;

    /// <summary>
    /// ビジュアルタイプ（"UyopyonBaby" or "UyopyonChild"）
    /// </summary>
    [Networked]
    public NetworkString<_16> VisualType { get; set; }

    /// <summary>
    /// きんとれ使用後のバフ倍率（デフォルト1.0、きんとれ後1.5）
    /// </summary>
    [Networked]
    public float BuffMultiplier { get; set; } = 1.0f;

    // === 変更検知用の前回値 ===
    private int _lastWeight;
    private int _lastEnergy;
    private int _lastPlayBuffWeight;
    private int _lastPlayBuffEnergy;
    private bool _lastHasEvolved;
    private string _lastVisualType;
    private byte[] _lastStatusAilments = new byte[4];

    // === 位置管理 ===
    private bool _hasSetPosition = false;
    private Vector3 _targetPosition;

    /// <summary>
    /// スポーン時の初期化処理
    /// </summary>
    public override void Spawned()
    {
        // OwnerPlayerはonBeforeSpawnedで設定済みなので、ここでは設定しない
        // ※ Object.InputAuthorityはFusion Shared Modeで正しく同期されない場合があるため使用しない
        Debug.Log($"[UyopyonState] Spawned: GameObject名='{gameObject.name}', OwnerPlayer={OwnerPlayer}, InputAuthority={Object.InputAuthority}");

        // NetworkTransformを無効化（各クライアントで独立した位置を持つため）
        var networkTransform = GetComponent<Fusion.NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.enabled = false;
            Debug.Log($"[UyopyonState] NetworkTransformを無効化しました");
        }

        // GameObjectの名前を変更（デバッグ用）
        gameObject.name = $"UyopyonPrefab_{OwnerPlayer}";
        Debug.Log($"[UyopyonState.Spawned] GameObject名を変更: '{gameObject.name}'");

        // UIオブジェクトとして機能させるため、Canvasの下に配置する
        // 親子関係はネットワーク同期されないので、各クライアントで個別に設定する必要がある
        if (transform.parent == null || transform.parent.GetComponent<Canvas>() == null)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                transform.SetParent(mainCanvas.transform, false);
                Debug.Log($"[UyopyonState.Spawned] '{gameObject.name}' をCanvas '{mainCanvas.gameObject.name}' の子に設定しました");
            }
            else
            {
                Debug.LogWarning($"[UyopyonState.Spawned] '{gameObject.name}' Canvasが見つかりません！表示されない可能性があります。");
            }
        }

        // 位置設定はRender()で行う（ネットワーク同期を待つため）
        Debug.Log($"[UyopyonState.Spawned] 位置設定はRender()で行います。OwnerPlayer={OwnerPlayer}, LocalPlayer={(Runner != null ? Runner.LocalPlayer.ToString() : "N/A")}");

        // UIControllerにこのオブジェクトの参照を登録
        if (UIController.Instance != null)
        {
            UIController.Instance.RegisterUyopyon(this);
        }

        // GameParametersから初期値を設定（ホストのみ）
        if (Object.HasStateAuthority)
        {
            InitializeValues();
        }

        // 前回値を初期化
        _lastWeight = Weight;
        _lastEnergy = Energy;
        _lastPlayBuffWeight = PlayBuffWeight;
        _lastPlayBuffEnergy = PlayBuffEnergy;
        _lastHasEvolved = HasEvolved;
        _lastVisualType = VisualType.ToString();
        for (int i = 0; i < 4; i++)
        {
            _lastStatusAilments[i] = StatusAilments[i];
        }

        // UI の初期表示をトリガー
        UpdateDisplay();
    }

    /// <summary>
    /// 初期値を設定する（ホストのみ実行）
    /// GameParametersから初期値を取得する（必須）
    /// </summary>
    private void InitializeValues()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameParams != null)
        {
            Weight = GameManager.Instance.gameParams.InitialWeight;
            Energy = GameManager.Instance.gameParams.InitialEnergy;
        }
        else
        {
            Debug.LogWarning("GameParameters is not set! Weight and Energy will remain at default (0).");
        }
        VisualType = "UyopyonBaby";
    }

    /// <summary>
    /// 全クライアントで毎フレーム実行されるレンダリング処理
    /// データの変更を検知してUI更新を行う
    /// </summary>
    public override void Render()
    {
        // まだ位置が設定されていない場合、Render()で設定を試みる
        // OwnerPlayerが有効（PlayerRef.Noneでない）になるまで待つ
        if (!_hasSetPosition && Runner != null && Runner.LocalPlayer != PlayerRef.None && GameManager.Instance != null)
        {
            // OwnerPlayerがまだ同期されていない（PlayerRef.Noneの）場合は待つ
            if (OwnerPlayer == PlayerRef.None)
            {
                Debug.Log($"[UyopyonState.Render] '{gameObject.name}' OwnerPlayerがまだ同期されていません。待機中...");
                return; // 次のフレームで再試行
            }

            if (OwnerPlayer == Runner.LocalPlayer)
            {
                _targetPosition = GameManager.Instance.player1SpawnPosition;
                Debug.Log($"[UyopyonState.Render] '{gameObject.name}' (自分) OwnerPlayer={OwnerPlayer}, LocalPlayer={Runner.LocalPlayer} → 位置を設定: {_targetPosition}");
            }
            else
            {
                _targetPosition = GameManager.Instance.player2SpawnPosition;
                Debug.Log($"[UyopyonState.Render] '{gameObject.name}' (相手) OwnerPlayer={OwnerPlayer}, LocalPlayer={Runner.LocalPlayer} → 位置を設定: {_targetPosition}");
            }

            // UIオブジェクトの場合はRectTransformを使用
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(_targetPosition.x, _targetPosition.y);
                Debug.Log($"[UyopyonState.Render] '{gameObject.name}' RectTransform.anchoredPositionを設定: {rectTransform.anchoredPosition}");
            }
            else
            {
                transform.position = _targetPosition;
                Debug.Log($"[UyopyonState.Render] '{gameObject.name}' Transform.positionを設定: {transform.position}");
            }

            Debug.Log($"[UyopyonState.Render] '{gameObject.name}' 位置設定完了");
            _hasSetPosition = true;
        }

        // 位置が設定されている場合、毎フレーム位置をチェック（何かが上書きしていないか確認）
        if (_hasSetPosition)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 targetPos2D = new Vector2(_targetPosition.x, _targetPosition.y);
                float distance = Vector2.Distance(rectTransform.anchoredPosition, targetPos2D);
                if (distance > 0.1f)
                {
                    Debug.LogWarning($"[UyopyonState.Render] '{gameObject.name}' 位置がずれています！ 現在位置: {rectTransform.anchoredPosition}, ターゲット: {targetPos2D}, 距離: {distance} → 修正します");
                    rectTransform.anchoredPosition = targetPos2D;
                    Debug.Log($"[UyopyonState.Render] '{gameObject.name}' 位置修正後: {rectTransform.anchoredPosition}");
                }
            }
            else
            {
                float distance = Vector3.Distance(transform.position, _targetPosition);
                if (distance > 0.01f)
                {
                    Debug.LogWarning($"[UyopyonState.Render] '{gameObject.name}' 位置がずれています！ 現在位置: {transform.position}, ターゲット: {_targetPosition}, 距離: {distance} → 修正します");
                    transform.position = _targetPosition;
                    Debug.Log($"[UyopyonState.Render] '{gameObject.name}' 位置修正後: {transform.position}");
                }
            }
        }

        // Weight の変更をチェック
        if (_lastWeight != Weight)
        {
            UIController.Instance?.UpdateWeightDisplay(OwnerPlayer, Weight);
            _lastWeight = Weight;
        }

        // Energy の変更をチェック
        if (_lastEnergy != Energy)
        {
            UIController.Instance?.UpdateEnergyDisplay(OwnerPlayer, Energy);
            _lastEnergy = Energy;
        }

        // PlayBuffWeight の変更をチェック
        if (_lastPlayBuffWeight != PlayBuffWeight)
        {
            UIController.Instance?.UpdatePlayBuffDisplay(OwnerPlayer, PlayBuffWeight, PlayBuffEnergy);
            _lastPlayBuffWeight = PlayBuffWeight;
        }

        // PlayBuffEnergy の変更をチェック
        if (_lastPlayBuffEnergy != PlayBuffEnergy)
        {
            UIController.Instance?.UpdatePlayBuffDisplay(OwnerPlayer, PlayBuffWeight, PlayBuffEnergy);
            _lastPlayBuffEnergy = PlayBuffEnergy;
        }

        // HasEvolved の変更をチェック
        if (_lastHasEvolved != HasEvolved)
        {
            UIController.Instance?.UpdateEvolutionDisplay(OwnerPlayer, HasEvolved, SpecialAbilityName.ToString());
            _lastHasEvolved = HasEvolved;
        }

        // VisualType の変更をチェック
        string currentVisualType = VisualType.ToString();
        if (_lastVisualType != currentVisualType)
        {
            UIController.Instance?.UpdateUyopyonVisual(OwnerPlayer, currentVisualType);
            _lastVisualType = currentVisualType;
        }

        // StatusAilments の変更をチェック
        bool statusChanged = false;
        for (int i = 0; i < 4; i++)
        {
            if (_lastStatusAilments[i] != StatusAilments[i])
            {
                statusChanged = true;
                _lastStatusAilments[i] = StatusAilments[i];
            }
        }
        if (statusChanged)
        {
            byte[] ailments = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ailments[i] = StatusAilments[i];
            }
            UIController.Instance?.UpdateStatusAilmentDisplay(OwnerPlayer, ailments);
        }
    }

    /// <summary>
    /// UIController に通知して表示を更新する補助メソッド（Spawned での初期表示用）
    /// </summary>
    private void UpdateDisplay()
    {
        Debug.Log($"[UyopyonState] UpdateDisplay: OwnerPlayer={OwnerPlayer}, Weight={Weight}, Energy={Energy}");

        if (UIController.Instance != null)
        {
            UIController.Instance.UpdateWeightDisplay(OwnerPlayer, Weight);
            UIController.Instance.UpdateEnergyDisplay(OwnerPlayer, Energy);
            UIController.Instance?.UpdatePlayBuffDisplay(OwnerPlayer, PlayBuffWeight, PlayBuffEnergy);
            UIController.Instance?.UpdateEvolutionDisplay(OwnerPlayer, HasEvolved, SpecialAbilityName.ToString());
            UIController.Instance?.UpdateUyopyonVisual(OwnerPlayer, VisualType.ToString());

            byte[] ailments = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ailments[i] = StatusAilments[i];
            }
            UIController.Instance?.UpdateStatusAilmentDisplay(OwnerPlayer, ailments);
        }
    }

    // === ヘルパーメソッド ===

    /// <summary>
    /// 特定の状態異常を持っているかチェック
    /// </summary>
    public bool HasStatusAilment(StatusAilment ailment)
    {
        return StatusAilments[(int)ailment] == 1;
    }

    /// <summary>
    /// 状態異常を設定
    /// </summary>
    public void SetStatusAilment(StatusAilment ailment, bool value)
    {
        if (Object.HasStateAuthority)
        {
            StatusAilments.Set((int)ailment, (byte)(value ? 1 : 0));
        }
    }

    /// <summary>
    /// 全ての状態異常をクリア（つういん実行時に使用）
    /// </summary>
    public void ClearAllStatusAilments()
    {
        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < 4; i++)
            {
                StatusAilments.Set(i, 0);
            }
        }
    }
}
