using Fusion;
using UnityEngine;
using TMPro;

/// <summary>
/// プレイヤーのアバターであるうーぴょんのステータスをネットワーク同期する
/// </summary>
// Trigger recompilation
public class UyopyonState : NetworkBehaviour
{
    // === UI参照 ===
    [SerializeField]
    [Tooltip("プレイヤー名を表示するTextMeshProコンポーネント")]
    private TextMeshProUGUI playerNameText;

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
    private string _lastSpecialAbilityName = "";
    private byte[] _lastStatusAilments = new byte[4];
    private string _lastPlayerName = "";

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
        DebugLogger.Log($"[UyopyonState] Spawned: GameObject名='{gameObject.name}', OwnerPlayer={OwnerPlayer}, InputAuthority={Object.InputAuthority}");

        // NetworkTransformを無効化（各クライアントで独立した位置を持つため）
        var networkTransform = GetComponent<Fusion.NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.enabled = false;
            DebugLogger.Log($"[UyopyonState] NetworkTransformを無効化しました");
        }

        // GameObjectの名前を変更（デバッグ用）
        gameObject.name = $"UyopyonPrefab_{OwnerPlayer}";
        DebugLogger.Log($"[UyopyonState.Spawned] GameObject名を変更: '{gameObject.name}'");

        // UIオブジェクトとして機能させるため、Canvasの下に配置する
        // 親子関係はネットワーク同期されないので、各クライアントで個別に設定する必要がある
        if (transform.parent == null || transform.parent.GetComponent<Canvas>() == null)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                transform.SetParent(mainCanvas.transform, false);
                DebugLogger.Log($"[UyopyonState.Spawned] '{gameObject.name}' をCanvas '{mainCanvas.gameObject.name}' の子に設定しました");
            }
            else
            {
                Debug.LogWarning($"[UyopyonState.Spawned] '{gameObject.name}' Canvasが見つかりません！表示されない可能性があります。");
            }
        }

        // 位置設定はRender()で行う（ネットワーク同期を待つため）
        DebugLogger.Log($"[UyopyonState.Spawned] 位置設定はRender()で行います。OwnerPlayer={OwnerPlayer}, LocalPlayer={(Runner != null ? Runner.LocalPlayer.ToString() : "N/A")}");

        // UIControllerにこのオブジェクトの参照を登録
        if (UIController.Instance != null)
        {
            UIController.Instance.RegisterUyopyon(this);
        }

        // GameManagerに自分自身を登録（全クライアントで実行）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUyopyonState(this);
        }
        else
        {
            Debug.LogWarning($"[UyopyonState.Spawned] GameManager.Instance が null のため、登録できませんでした。Player={OwnerPlayer}");
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

        // プレイヤー名の初期表示
        UpdatePlayerNameDisplay();

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
                return; // 次のフレームで再試行
            }

            if (OwnerPlayer == Runner.LocalPlayer)
            {
                _targetPosition = GameManager.Instance.player1SpawnPosition;
                DebugLogger.Log($"[UyopyonState.Render] '{gameObject.name}' (自分) OwnerPlayer={OwnerPlayer}, LocalPlayer={Runner.LocalPlayer} → 位置を設定: {_targetPosition}");
            }
            else
            {
                _targetPosition = GameManager.Instance.player2SpawnPosition;
                DebugLogger.Log($"[UyopyonState.Render] '{gameObject.name}' (相手) OwnerPlayer={OwnerPlayer}, LocalPlayer={Runner.LocalPlayer} → 位置を設定: {_targetPosition}");
            }

            // UIオブジェクトの場合はRectTransformを使用
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(_targetPosition.x, _targetPosition.y);
            }
            else
            {
                transform.position = _targetPosition;
            }

            _hasSetPosition = true;
        }

        // 位置チェックは削除（毎フレーム実行する必要がないため）
        // 位置がずれる問題が発生した場合は、イベントベースで修正する

        // Weight の変更をチェック
        if (_lastWeight != Weight)
        {
            UIController.Instance?.UpdateWeightDisplay(OwnerPlayer, Weight);
            // 重さが変わったら、じゅくすい・どかぐいのボタン状態を更新
            UIController.Instance?.UpdateSpecialAbilityButtonState();
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

        // SpecialAbilityName の変更をチェック
        string currentAbilityName = SpecialAbilityName.ToString();
        if (_lastSpecialAbilityName != currentAbilityName)
        {
            DebugLogger.Log($"[UyopyonState.Render] Player {OwnerPlayer} の SpecialAbilityName が変更されました: '{_lastSpecialAbilityName}' -> '{currentAbilityName}'");
            UIController.Instance?.UpdateEvolutionDisplay(OwnerPlayer, HasEvolved, currentAbilityName);
            _lastSpecialAbilityName = currentAbilityName;
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

        // プレイヤー名の変更をチェック（NetworkPlayerの名前が後から設定される場合に対応）
        if (GameManager.Instance != null)
        {
            string currentPlayerName = GameManager.Instance.GetPlayerName(OwnerPlayer);
            if (_lastPlayerName != currentPlayerName)
            {
                UpdatePlayerNameDisplay();
                _lastPlayerName = currentPlayerName;
            }
        }
    }


    /// <summary>
    /// Despawn時にGameManagerから登録解除
    /// </summary>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUyopyonState(OwnerPlayer);
        }
    }

    /// <summary>
    /// UIController に通知して表示を更新する補助メソッド（Spawned での初期表示用）
    /// </summary>
    private void UpdateDisplay()
    {
        DebugLogger.Log($"[UyopyonState] UpdateDisplay: OwnerPlayer={OwnerPlayer}, Weight={Weight}, Energy={Energy}");

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

    /// <summary>
    /// プレイヤー名表示を更新する
    /// 全クライアントで実行され、各クライアントのローカルUIに反映される
    /// </summary>
    private void UpdatePlayerNameDisplay()
    {
        if (playerNameText == null)
        {
            // TextMeshProコンポーネントが未設定の場合は警告を出して終了
            // （エディタでアサインするまではこの警告が出る）
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UyopyonState] GameManager.Instance が null のため、プレイヤー名を取得できません");
            return;
        }

        // GameManagerからプレイヤー名を取得
        string playerName = GameManager.Instance.GetPlayerName(OwnerPlayer);

        if (!string.IsNullOrEmpty(playerName))
        {
            playerNameText.text = playerName;
            DebugLogger.Log($"[UyopyonState] プレイヤー名を表示: OwnerPlayer={OwnerPlayer}, Name='{playerName}'");
        }
        else
        {
            // 名前がまだ設定されていない場合は空白にする
            playerNameText.text = "";
            DebugLogger.Log($"[UyopyonState] プレイヤー名がまだ設定されていません: OwnerPlayer={OwnerPlayer}");
        }
    }
}
