using Fusion;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ゲーム内の全てのUI要素を管理し、ネットワーク同期されたデータに基づいて表示を更新する。
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    // === 自分のUI要素 ===
    public TextMeshProUGUI myNameText;
    public TextMeshProUGUI myWeightText;
    public TextMeshProUGUI myEnergyText;

    // === 相手のUI要素 ===
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI opponentWeightText;
    public TextMeshProUGUI opponentEnergyText;

    // === 行動選択ボタン（3.1で追加） ===
    public UnityEngine.UI.Button eatButton;      // たべるボタン
    public UnityEngine.UI.Button sleepButton;    // ねむるボタン
    public UnityEngine.UI.Button playButton;     // あそぶボタン
    public UnityEngine.UI.Button clinicButton;   // つういんボタン

    // === 確定・クリアボタン（3.1で追加） ===
    public UnityEngine.UI.Button fixButton;      // 確定ボタン
    public UnityEngine.UI.Button clearButton;    // クリアボタン

    private Dictionary<PlayerRef, UyopyonState> _uyopyons = new Dictionary<PlayerRef, UyopyonState>();
    private PlayerRef _localPlayerRef;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        var runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.LocalPlayer.PlayerId != 0)
        {
            _localPlayerRef = runner.LocalPlayer;
        }
    }

    /// <summary>
    /// UyopyonState.Spawned()から呼ばれ、うーぴょんの参照を登録する
    /// </summary>
    public void RegisterUyopyon(UyopyonState uyopyon)
    {
        if (uyopyon.OwnerPlayer != PlayerRef.None && !_uyopyons.ContainsKey(uyopyon.OwnerPlayer))
        {
            _uyopyons.Add(uyopyon.OwnerPlayer, uyopyon);

            // 初期表示の更新を明示的に実行（OnChangedが呼ばれない初回値の設定に対応）
            UpdateWeightDisplay(uyopyon.OwnerPlayer, uyopyon.Weight);
            UpdateEnergyDisplay(uyopyon.OwnerPlayer, uyopyon.Energy);
        }
    }

    // === プレイヤー名更新（NetworkPlayerから呼ばれる） ===
    public void UpdateMyName(string newName) => myNameText.text = newName;
    public void UpdateOpponentName(string newName) => opponentNameText.text = newName;

    // === ステータス更新（UyopyonStateから呼ばれる） ===

    public void UpdateWeightDisplay(PlayerRef player, int weight)
    {
        if (player == _localPlayerRef)
        {
            myWeightText.text = $"重さ: {weight}";
        }
        else
        {
            opponentWeightText.text = $"重さ: {weight}";
        }
    }

    public void UpdateEnergyDisplay(PlayerRef player, int energy)
    {
        if (player == _localPlayerRef)
        {
            myEnergyText.text = $"元気: {energy}%";
        }
        else
        {
            opponentEnergyText.text = $"元気: {energy}%";
        }
    }

    // TODO: フェーズ3で実装予定 - あそぶバフの表示
    public void UpdatePlayBuffDisplay(PlayerRef player, int buffWeight, int buffEnergy) { }

    // TODO: フェーズ7で実装予定 - 進化状態の表示
    public void UpdateEvolutionDisplay(PlayerRef player, bool hasEvolved, string abilityName) { }

    // TODO: フェーズ9で実装予定 - ビジュアル変更
    public void UpdateUyopyonVisual(PlayerRef player, string visualType) { }

    // TODO: フェーズ6で実装予定 - 状態異常アイコン表示
    public void UpdateStatusAilmentDisplay(PlayerRef player, byte[] ailments) { }

    // TODO: フェーズ3.4で実装予定 - ブラックアウトパネルの表示
    public void ShowBlackout(string text, float duration)
    {
        Debug.Log($"[UIController] ブラックアウト表示（未実装）: {text}");
        // 実装は3.4で行う予定
    }

    // TODO: フェーズ3.4で実装予定 - ログメッセージの追加
    public void AddLog(string message)
    {
        Debug.Log($"[UIController] ログ追加（未実装）: {message}");
        // 実装は3.4で行う予定
    }

    // === 3.1で追加: 行動選択ボタンのイベントハンドラー ===

    /// <summary>
    /// 「たべる」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    public void OnEatButtonClicked()
    {
        Debug.Log("[UIController] たべるボタンがクリックされました（処理は未実装）");
        // TODO: 3.2で実装予定
    }

    /// <summary>
    /// 「ねむる」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    public void OnSleepButtonClicked()
    {
        Debug.Log("[UIController] ねむるボタンがクリックされました（処理は未実装）");
        // TODO: 3.2で実装予定
    }

    /// <summary>
    /// 「あそぶ」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    public void OnPlayButtonClicked()
    {
        Debug.Log("[UIController] あそぶボタンがクリックされました（処理は未実装）");
        // TODO: 3.2で実装予定
    }

    /// <summary>
    /// 「つういん」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    public void OnClinicButtonClicked()
    {
        Debug.Log("[UIController] つういんボタンがクリックされました（処理は未実装）");
        // TODO: 3.2で実装予定
    }

    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    public void OnFixButtonClicked()
    {
        Debug.Log("[UIController] 確定ボタンがクリックされました（処理は未実装）");
        // TODO: 3.2で実装予定
    }

    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    public void OnClearButtonClicked()
    {
        Debug.Log("[UIController] クリアボタンがクリックされました（処理は未実装）");
        // TODO: 3.2で実装予定
    }

    // ... その他、ラウンド表示、メッセージ表示などのメソッド ...
}
