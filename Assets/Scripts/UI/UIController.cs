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
    public UnityEngine.UI.Button clearButton;   

    // === 3.3で追加: 行動表示パネル ===
    [Header("Action Display Panels")]
    public GameObject myYesterdayAfternoonActionPanel;
    public TextMeshProUGUI myYesterdayAfternoonActionText;
    public GameObject oppYesterdayAfternoonActionPanel;
    public TextMeshProUGUI oppYesterdayAfternoonActionText;

    public GameObject myTodayMorningActionPanel;
    public TextMeshProUGUI myTodayMorningActionText;
    public UnityEngine.UI.Image myTodayMorningBorder; // 太枠表示用

    public GameObject myTodayAfternoonActionPanel;
    public TextMeshProUGUI myTodayAfternoonActionText;
    public UnityEngine.UI.Image myTodayAfternoonBorder; // 太枠表示用

    public GameObject oppTodayMorningActionPanel;
    public TextMeshProUGUI oppTodayMorningActionText;

    public GameObject oppTodayAfternoonActionPanel;
    public TextMeshProUGUI oppTodayAfternoonActionText; // クリアボタン

    private Dictionary<PlayerRef, UyopyonState> _uyopyons = new Dictionary<PlayerRef, UyopyonState>();
    

    // === 3.2で追加: 行動選択状態 ===
    private ActionData? _morningAction = null;      // 午前に選択した行動
    private ActionData? _afternoonAction = null;    // 午後に選択した行動
    private bool _isMorningSelected = false;        // 午前が選択済みか
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

    private void Start()
    {
        Debug.Log("[UIController] Start: 初期化開始");
        // 初期状態で午前選択中のハイライトを表示
        HighlightCurrentSelection(false);
        Debug.Log("[UIController] Start: 初期化完了");
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
            myWeightText.text = $"{weight}";
        }
        else
        {
            opponentWeightText.text = $"{weight}";
        }
    }

    public void UpdateEnergyDisplay(PlayerRef player, int energy)
    {
        if (player == _localPlayerRef)
        {
            myEnergyText.text = $"{energy}";
        }
        else
        {
            opponentEnergyText.text = $"{energy}";
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
    /// <summary>
    /// 「たべる」ボタンがクリックされた時の処理
    /// </summary>
    public void OnEatButtonClicked()
    {
        Debug.Log("[UIController] OnEatButtonClicked が呼ばれました");
        OnActionButtonClicked(ActionType.Eat);
    }

    /// <summary>
    /// 「ねむる」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    /// <summary>
    /// 「ねむる」ボタンがクリックされた時の処理
    /// </summary>
    public void OnSleepButtonClicked()
    {
        OnActionButtonClicked(ActionType.Sleep);
    }

    /// <summary>
    /// 「あそぶ」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    /// <summary>
    /// 「あそぶ」ボタンがクリックされた時の処理
    /// </summary>
    public void OnPlayButtonClicked()
    {
        OnActionButtonClicked(ActionType.Play);
    }

    /// <summary>
    /// 「つういん」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    /// <summary>
    /// 「つういん」ボタンがクリックされた時の処理
    /// </summary>
    public void OnClinicButtonClicked()
    {
        OnActionButtonClicked(ActionType.Clinic);
    }

    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理
    /// 選択した行動をPlayerActionDataに送信する
    /// </summary>
    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理
    /// 選択した行動をPlayerActionDataに送信する
    /// </summary>
    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理
    /// 選択した行動をPlayerActionDataに送信する
    /// </summary>
    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理
    /// 選択した行動をPlayerActionDataに送信する
    /// </summary>
    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理
    /// 選択した行動をPlayerActionDataに送信する
    /// </summary>
    /// <summary>
    /// 「確定」ボタンがクリックされた時の処理
    /// 選択した行動をPlayerActionDataに送信する
    /// </summary>
    public void OnFixButtonClicked()
    {
        // 午前・午後の両方が選択されていない場合はデフォルト（ねむる）を設定
        if (!_morningAction.HasValue)
        {
            _morningAction = ActionData.Default();
            Debug.Log("[UIController] 午前が未選択のため、デフォルト（ねむる）を設定");
        }
        if (!_afternoonAction.HasValue)
        {
            _afternoonAction = ActionData.Default();
            Debug.Log("[UIController] 午後が未選択のため、デフォルト（ねむる）を設定");
        }

        // NetworkRunnerから現在のLocalPlayerを取得
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[UIController] NetworkRunnerが見つかりません");
            return;
        }

        PlayerRef localPlayer = runner.LocalPlayer;

        // 自分のPlayerActionDataを取得（OwnerPlayerで判定）
        PlayerActionData myActionData = null;
        if (GameManager.Instance != null)
        {
            var allActionData = FindObjectsByType<PlayerActionData>(FindObjectsSortMode.None);
            
            foreach (var actionData in allActionData)
            {
                // OwnerPlayerがLocalPlayerと一致するものを探す
                if (actionData.OwnerPlayer == localPlayer)
                {
                    myActionData = actionData;
                    break;
                }
            }
        }

        if (myActionData != null)
        {
            // RPC経由で行動を送信
            myActionData.RPC_SetMorningAction(_morningAction.Value);
            myActionData.RPC_SetAfternoonAction(_afternoonAction.Value);
            myActionData.RPC_FixActions();

            Debug.Log($"[UIController] 行動を確定しました: Player={myActionData.OwnerPlayer}, 午前={_morningAction.Value.Type}, 午後={_afternoonAction.Value.Type}");
        }
        else
        {
            Debug.LogError($"[UIController] OwnerPlayer={localPlayer}のPlayerActionDataが見つかりません");
        }
    }

    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理（スタブ）
    /// </summary>
    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理
    /// 選択した行動をリセットする
    /// </summary>
    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理
    /// 選択した行動をリセットする
    /// </summary>
    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理
    /// 選択した行動をリセットする
    /// </summary>
    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理
    /// 選択した行動をリセットする
    /// </summary>
    public void OnClearButtonClicked()
    {
        _morningAction = null;
        _afternoonAction = null;
        _isMorningSelected = false;

        Debug.Log("[UIController] 選択をクリアしました");

        // パネル表示もクリア
        if (myTodayMorningActionText != null)
            myTodayMorningActionText.text = "";
        if (myTodayAfternoonActionText != null)
            myTodayAfternoonActionText.text = "";

        // 午前選択中のハイライトに戻す
        HighlightCurrentSelection(false);

        // NetworkRunnerから現在のLocalPlayerを取得
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            PlayerRef localPlayer = runner.LocalPlayer;
            
            // 自分のPlayerActionDataを取得（OwnerPlayerで判定）
            if (GameManager.Instance != null)
            {
                var allActionData = FindObjectsByType<PlayerActionData>(FindObjectsSortMode.None);
                foreach (var actionData in allActionData)
                {
                    if (actionData.OwnerPlayer == localPlayer)
                    {
                        actionData.RPC_ClearActions();
                        break;
                    }
                }
            }
        }
    }

    // ... その他、ラウンド表示、メッセージ表示などのメソッド ...



    /// <summary>
    /// 行動ボタンがクリックされた時の共通処理（3.2で追加）
    /// </summary>
    /// <param name="actionType">選択された行動の種類</param>
    private void OnActionButtonClicked(ActionType actionType)
    {
        Debug.Log($"[UIController] OnActionButtonClicked 開始: actionType={actionType}");

        // ActionTypeに対応するActionDataを生成
        ActionData selectedAction = actionType switch
        {
            ActionType.Eat => ActionData.CreateEat(),
            ActionType.Sleep => ActionData.CreateSleep(),
            ActionType.Play => ActionData.CreatePlay(),
            ActionType.Clinic => ActionData.CreateClinic(),
            _ => ActionData.Default()
        };

        // 午前が未選択なら午前に設定、選択済みなら午後に設定
        if (!_isMorningSelected)
        {
            _morningAction = selectedAction;
            _isMorningSelected = true;
            Debug.Log($"[UIController] 午前の行動を選択: {actionType}");

            // パネル表示を更新（自分の午前）
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null)
            {
                UpdateActionDisplay(runner.LocalPlayer, true, actionType);
                HighlightCurrentSelection(false); // 午前選択中を表示
            }
        }
        else
        {
            _afternoonAction = selectedAction;
            Debug.Log($"[UIController] 午後の行動を選択: {actionType}");

            // パネル表示を更新（自分の午後）
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null)
            {
                UpdateActionDisplay(runner.LocalPlayer, false, actionType);
                HighlightCurrentSelection(true); // 午後選択中を表示
            }
        }
    }

    // === 3.3で追加: 行動表示メソッド ===

    /// <summary>
    /// 行動表示を更新
    /// </summary>
    public void UpdateActionDisplay(PlayerRef player, bool isMorning, ActionType action)
    {
        Debug.Log($"[UIController] UpdateActionDisplay: player={player}, isMorning={isMorning}, action={action}");
        string actionText = GetActionText(action);

        // 自分のプレイヤーか判定
        bool isMyPlayer = IsMyPlayer(player);

        if (isMyPlayer)
        {
            if (isMorning)
            {
                myTodayMorningActionText.text = actionText;
            }
            else
            {
                myTodayAfternoonActionText.text = actionText;
            }
        }
        else
        {
            if (isMorning)
            {
                oppTodayMorningActionText.text = actionText;
            }
            else
            {
                oppTodayAfternoonActionText.text = actionText;
            }
        }
    }

    /// <summary>
    /// 選択中の時間帯を太枠で表示
    /// </summary>
    public void HighlightCurrentSelection(bool isAfternoon)
    {
        Debug.Log($"[UIController] HighlightCurrentSelection: isAfternoon={isAfternoon}");

        if (myTodayMorningBorder == null || myTodayAfternoonBorder == null)
        {
            Debug.LogWarning("[UIController] Border参照がnullです。Unityエディタで設定してください。");
            return;
        }

        // 午前のOutlineを取得
        var morningOutline = myTodayMorningBorder.GetComponent<UnityEngine.UI.Outline>();
        if (morningOutline == null)
        {
            Debug.LogError("[UIController] myTodayMorningBorderにOutlineコンポーネントがありません！");
            return;
        }

        // 午後のOutlineを取得
        var afternoonOutline = myTodayAfternoonBorder.GetComponent<UnityEngine.UI.Outline>();
        if (afternoonOutline == null)
        {
            Debug.LogError("[UIController] myTodayAfternoonBorderにOutlineコンポーネントがありません！");
            return;
        }

        if (isAfternoon)
        {
            // 午後を選択中 - 午前は薄いグレー、午後は黄色
            morningOutline.effectColor = Color.gray;
            morningOutline.enabled = true;  // 両方とも表示する
            Debug.Log($"[UIController] 午前Outline設定: effectColor=Gray, enabled=true");

            afternoonOutline.effectColor = Color.yellow;
            afternoonOutline.enabled = true;
            Debug.Log($"[UIController] 午後Outline設定: effectColor=Yellow, enabled=true");
        }
        else
        {
            // 午前を選択中 - 午前は黄色、午後は薄いグレー
            morningOutline.effectColor = Color.yellow;
            morningOutline.enabled = true;
            Debug.Log($"[UIController] 午前Outline設定: effectColor=Yellow, enabled=true");

            afternoonOutline.effectColor = Color.gray;
            afternoonOutline.enabled = true;  // 両方とも表示する
            Debug.Log($"[UIController] 午後Outline設定: effectColor=Gray, enabled=true");
        }
    }

    /// <summary>
    /// ActionTypeをテキストに変換
    /// </summary>
    private string GetActionText(ActionType action)
    {
        switch (action)
        {
            case ActionType.Eat: return "たべる";
            case ActionType.Sleep: return "ねむる";
            case ActionType.Play: return "あそぶ";
            case ActionType.Clinic: return "つういん";
            case ActionType.SpecialAbility: return "特殊能力";
            default: return "";
        }
    }

    /// <summary>
    /// 自分のプレイヤーかどうか判定
    /// </summary>
    private bool IsMyPlayer(PlayerRef player)
    {
        // NetworkRunnerから自分のPlayerRefを取得して比較
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            return runner.LocalPlayer == player;
        }
        return false;
    }
}
