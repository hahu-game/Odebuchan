using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;
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

    // === 行動選択エリアの名前表示 ===
    public TextMeshProUGUI selectMyNameText;    // 行動選択エリアの自分の名前
    public TextMeshProUGUI selectOppNameText;   // 行動選択エリアの相手の名前

    // === 行動選択ボタン（3.1で追加） ===
    public UnityEngine.UI.Button eatButton;      // たべるボタン
    public UnityEngine.UI.Button sleepButton;    // ねむるボタン
    public UnityEngine.UI.Button playButton;     // あそぶボタン
    public UnityEngine.UI.Button clinicButton;   // つういんボタン

    // === 行動選択ボタンのOutline（選択フェーズ中に表示） ===
    [Header("Action Button Outlines")]
    public UnityEngine.UI.Outline eatButtonOutline;
    public UnityEngine.UI.Outline sleepButtonOutline;
    public UnityEngine.UI.Outline playButtonOutline;
    public UnityEngine.UI.Outline clinicButtonOutline;

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

    // === 6.7で追加: 状態異常テキスト表示 ===
    [Header("Status Ailment Texts")]
    public TextMeshProUGUI[] myStatusAilmentTexts = new TextMeshProUGUI[4];
    public TextMeshProUGUI[] oppStatusAilmentTexts = new TextMeshProUGUI[4];

    // === 3.4で追加: ブラックアウトパネルとログエリア ===
    [Header("Blackout Panel")]
    public GameObject blackoutPanel;
    public TextMeshProUGUI blackoutText;
    private Coroutine _blackoutCoroutine;

    [Header("Log Area")]
    public UnityEngine.UI.ScrollRect logScrollRect;
    public Transform logContent;
    public GameObject logTextPrefab; // TextMeshProUGUIを持つプレハブ

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
        // 初期状態では行動ボタンのOutlineを非表示
        HideActionButtonOutlines();
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
    public void UpdateMyName(string newName)
    {
        myNameText.text = newName;
        // 行動選択エリアの名前も更新
        if (selectMyNameText != null)
        {
            selectMyNameText.text = newName;
        }
    }

    public void UpdateOpponentName(string newName)
    {
        opponentNameText.text = newName;
        // 行動選択エリアの名前も更新
        if (selectOppNameText != null)
        {
            selectOppNameText.text = newName;
        }
    }

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

    /// <summary>
    /// ブラックアウトパネルを表示
    /// </summary>
    /// <param name="text">表示するテキスト</param>
    /// <param name="duration">表示時間（秒）</param>
    public void ShowBlackout(string text, float duration)
    {
        Debug.Log($"[UIController] ブラックアウト表示: {text}, duration={duration}秒");

        if (blackoutPanel == null || blackoutText == null)
        {
            Debug.LogWarning("[UIController] BlackoutPanel or BlackoutText is not assigned!");
            return;
        }

        // 前のコルーチンが実行中なら停止
        if (_blackoutCoroutine != null)
        {
            StopCoroutine(_blackoutCoroutine);
            Debug.Log("[UIController] 前のブラックアウトコルーチンを停止しました");
        }

        blackoutText.text = text;
        blackoutPanel.SetActive(true);

        // duration秒後に非表示
        _blackoutCoroutine = StartCoroutine(HideBlackoutAfterDelay(duration));
    }

    /// <summary>
    /// ブラックアウトパネルを非表示
    /// </summary>
    private IEnumerator HideBlackoutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (blackoutPanel != null)
        {
            blackoutPanel.SetActive(false);
            Debug.Log($"[UIController] ブラックアウトを非表示にしました（{delay}秒後）");
        }
        _blackoutCoroutine = null;
    }

    /// <summary>
    /// ログエリアにメッセージを追加
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    public void AddLog(string message)
    {
        Debug.Log($"[UIController] ログ追加: {message}");

        if (logTextPrefab == null || logContent == null)
        {
            Debug.LogWarning("[UIController] LogTextPrefab or LogContent is not assigned!");
            return;
        }

        // ログテキストのプレハブをインスタンス化
        GameObject logObj = Instantiate(logTextPrefab, logContent);
        RectTransform rectTransform = logObj.GetComponent<RectTransform>();
        TextMeshProUGUI logText = logObj.GetComponent<TextMeshProUGUI>();

        // RectTransformの設定を最初に行う（幅を親に合わせる）
        if (rectTransform != null)
        {
            // アンカーを左上から右上に伸ばす（横幅を親に合わせる）
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(0, 0);

            // sizeDeltaで横幅を明示的に設定（親の幅-10px（左右5pxずつマージン））
            RectTransform parentRect = logContent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;
                rectTransform.sizeDelta = new Vector2(parentWidth - 10f, 0);
            }
            else
            {
                // フォールバック: デフォルトの幅を設定
                rectTransform.sizeDelta = new Vector2(280f, 0);
            }
        }

        if (logText != null)
        {
            // タイムスタンプ付きでログを表示
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            logText.text = $"[{timestamp}] {message}";

            // ログテキストの色を黒に設定
            logText.color = Color.black;

            // 自動的に折り返しを有効化
            logText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            logText.overflowMode = TMPro.TextOverflowModes.Overflow;

            // テキストを更新してレイアウトを再計算
            logText.ForceMeshUpdate();
        }

        // ContentSizeFitterを追加してテキストの高さに応じて自動調整
        UnityEngine.UI.ContentSizeFitter fitter = logObj.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = logObj.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        }
        // 横幅は固定（RectTransformの設定に従う）、高さだけ自動調整
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        // Layout Elementを追加（最小高さを設定）
        UnityEngine.UI.LayoutElement layoutElement = logObj.GetComponent<UnityEngine.UI.LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = logObj.AddComponent<UnityEngine.UI.LayoutElement>();
        }
        layoutElement.minHeight = 20f;
        // preferredHeightやflexibleWidthは設定しない（ContentSizeFitterに任せる）

        // 自動的に最下部にスクロール
        StartCoroutine(ScrollToBottom());
    }

    /// <summary>
    /// ログエリアを最下部にスクロール
    /// </summary>
    private IEnumerator ScrollToBottom()
    {
        // レイアウト更新を強制
        Canvas.ForceUpdateCanvases();

        // 1フレーム待ってからスクロール（レイアウト更新を待つため）
        yield return null;

        if (logScrollRect != null)
        {
            // 最下部にスクロール（0 = 最下部、1 = 最上部）
            logScrollRect.verticalNormalizedPosition = 0f;

            // 念のためもう1フレーム待って再度設定
            yield return null;
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// ログをクリア
    /// </summary>
    public void ClearLog()
    {
        if (logContent == null) return;

        foreach (Transform child in logContent)
        {
            Destroy(child.gameObject);
        }
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
        // 午前・午後の行動が選択されているかチェック
        bool morningSelected = _morningAction.HasValue;
        bool afternoonSelected = _afternoonAction.HasValue;

        // 未選択の行動がある場合はエラーメッセージを表示して処理を中断
        if (!morningSelected && !afternoonSelected)
        {
            AddLog("午前と午後の行動を決めてから確定ボタンを押してください");
            Debug.Log("[UIController] 午前と午後の行動が未選択のため、確定できません");
            return;
        }
        else if (!morningSelected)
        {
            AddLog("午前の行動を決めてから確定ボタンを押してください");
            Debug.Log("[UIController] 午前の行動が未選択のため、確定できません");
            return;
        }
        else if (!afternoonSelected)
        {
            AddLog("午後の行動を決めてから確定ボタンを押してください");
            Debug.Log("[UIController] 午後の行動が未選択のため、確定できません");
            return;
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

            // 確定後、行動ボタンと確定・クリアボタンを無効化（ロック）
            LockActionButtons();
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
    /// 行動表示を更新（テキスト直接指定版）
    /// </summary>
    public void UpdateActionDisplay(PlayerRef player, bool isMorning, ActionType action, string customText)
    {
        Debug.Log($"[UIController] UpdateActionDisplay (customText): player={player}, isMorning={isMorning}, action={action}, text='{customText}'");
        
        // 自分のプレイヤーか判定
        bool isMyPlayer = IsMyPlayer(player);

        if (isMyPlayer)
        {
            if (isMorning)
            {
                myTodayMorningActionText.text = customText;
            }
            else
            {
                myTodayAfternoonActionText.text = customText;
            }
        }
        else
        {
            if (isMorning)
            {
                oppTodayMorningActionText.text = customText;
            }
            else
            {
                oppTodayAfternoonActionText.text = customText;
            }
        }
    }

    /// <summary>
    /// 選択中の時間帯を太枠で表示
    /// </summary>
    /// <summary>
    /// 昨日の午後の行動表示を更新
    /// </summary>
    public void UpdateYesterdayAfternoonDisplay(PlayerRef player, ActionType action)
    {
        Debug.Log($"[UIController] UpdateYesterdayAfternoonDisplay: player={player}, action={action}");
        string actionText = GetActionText(action);
        bool isMyPlayer = IsMyPlayer(player);
        
        if (isMyPlayer)
        {
            if (myYesterdayAfternoonActionText != null)
                myYesterdayAfternoonActionText.text = actionText;
        }
        else
        {
            if (oppYesterdayAfternoonActionText != null)
                oppYesterdayAfternoonActionText.text = actionText;
        }
    }

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
            case ActionType.None: return "";
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

    // === 行動ボタンのOutline制御 ===

    /// <summary>
    /// 行動ボタンのOutlineを表示する（選択フェーズ開始時に呼ぶ）
    /// </summary>
    public void ShowActionButtonOutlines()
    {
        Debug.Log("[UIController] 行動ボタンのOutlineを表示");

        // Outline設定: EffectColor=#FFB101, EffectDistance=(10, -10)
        Color outlineColor;
        if (!ColorUtility.TryParseHtmlString("#FFB101", out outlineColor))
        {
            outlineColor = new Color(1f, 0.694f, 0.004f); // フォールバック
        }
        Vector2 outlineDistance = new Vector2(10f, -10f);

        if (eatButtonOutline != null)
        {
            eatButtonOutline.effectColor = outlineColor;
            eatButtonOutline.effectDistance = outlineDistance;
            eatButtonOutline.enabled = true;
        }

        if (sleepButtonOutline != null)
        {
            sleepButtonOutline.effectColor = outlineColor;
            sleepButtonOutline.effectDistance = outlineDistance;
            sleepButtonOutline.enabled = true;
        }

        if (playButtonOutline != null)
        {
            playButtonOutline.effectColor = outlineColor;
            playButtonOutline.effectDistance = outlineDistance;
            playButtonOutline.enabled = true;
        }

        if (clinicButtonOutline != null)
        {
            clinicButtonOutline.effectColor = outlineColor;
            clinicButtonOutline.effectDistance = outlineDistance;
            clinicButtonOutline.enabled = true;
        }
    }

    /// <summary>
    /// 行動ボタンのOutlineを非表示にする（選択フェーズ終了時に呼ぶ）
    /// </summary>
    /// <summary>
    /// 行動ボタンの操作可否を設定
    /// </summary>
    /// <param name="interactable">trueで操作可能、falseで操作不可</param>
    public void SetActionButtonsInteractable(bool interactable)
    {
        Debug.Log($"[UIController] 行動ボタンの操作を{(interactable ? "有効" : "無効")}に設定");

        if (eatButton != null) eatButton.interactable = interactable;
        if (sleepButton != null) sleepButton.interactable = interactable;
        if (playButton != null) playButton.interactable = interactable;
        if (clinicButton != null) clinicButton.interactable = interactable;

        // 確定・クリアボタンも同時に制御
        if (fixButton != null) fixButton.interactable = interactable;
        if (clearButton != null) clearButton.interactable = interactable;
    }


    /// <summary>
    /// 行動確定後、すべてのボタンをロック（無効化）
    /// </summary>
    private void LockActionButtons()
    {
        Debug.Log("[UIController] 行動確定後、すべてのボタンをロックします");

        // 行動ボタンを無効化
        if (eatButton != null) eatButton.interactable = false;
        if (sleepButton != null) sleepButton.interactable = false;
        if (playButton != null) playButton.interactable = false;
        if (clinicButton != null) clinicButton.interactable = false;

        // 確定・クリアボタンを無効化
        if (fixButton != null) fixButton.interactable = false;
        if (clearButton != null) clearButton.interactable = false;
    }

    public void HideActionButtonOutlines()
    {
        Debug.Log("[UIController] 行動ボタンのOutlineを非表示");

        if (eatButtonOutline != null) eatButtonOutline.enabled = false;
        if (sleepButtonOutline != null) sleepButtonOutline.enabled = false;
        if (playButtonOutline != null) playButtonOutline.enabled = false;
        if (clinicButtonOutline != null) clinicButtonOutline.enabled = false;
    }

    /// <summary>
    /// 行動選択状態をリセット（新しい日の選択フェーズ開始時に呼ぶ）
    /// </summary>
    public void ResetActionSelection()
    {
        Debug.Log("[UIController] 行動選択状態をリセット");

        _morningAction = null;
        _afternoonAction = null;
        _isMorningSelected = false;

        // 午前選択中のハイライトに戻す
        HighlightCurrentSelection(false);

        Debug.Log("[UIController] 行動選択状態のリセット完了");
    }

    /// <summary>
    /// ジャンケン結果エフェクトを表示（後で実装）
    /// </summary>
    public void ShowJankenEffect(PlayerRef winner, Genre winGenre)
    {
        Debug.Log($"[UIController] ジャンケン結果エフェクト: Winner={winner}, Genre={winGenre}");
        // 実装は後で（フェーズ10）
    }

    /// <summary>
    /// 行動アニメーションを再生（後で実装）
    /// </summary>
    public void PlayActionAnimation(PlayerRef player, ActionType action)
    {
        Debug.Log($"[UIController] 行動アニメーション: Player={player}, Action={action}");
        // 実装は後で（フェーズ10）
    }

    // === 6.7で追加: 状態異常テキスト表示メソッド ===

    /// <summary>
    /// 状態異常テキストを更新
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="ailments">状態異常配列</param>
    public void UpdateStatusAilmentDisplay(PlayerRef player, byte[] ailments)
    {
        // 自分のプレイヤーか判定
        bool isMyPlayer = IsMyPlayer(player);
        TextMeshProUGUI[] texts = isMyPlayer ? myStatusAilmentTexts : oppStatusAilmentTexts;

        // すべての親オブジェクト（first, second, third, fourth）を非表示
        foreach (var text in texts)
        {
            if (text != null && text.transform.parent != null)
            {
                text.transform.parent.gameObject.SetActive(false);
            }
        }

        // 状態異常があれば対応するテキストを表示
        int textIndex = 0;
        for (int i = 0; i < ailments.Length && textIndex < texts.Length; i++)
        {
            if (ailments[i] != 0) // 状態異常あり
            {
                // インデックスから状態異常の種類を判定
                StatusAilment ailment = IndexToStatusAilment(i);
                string ailmentText = GetStatusAilmentText(ailment);

                if (!string.IsNullOrEmpty(ailmentText) && texts[textIndex] != null)
                {
                    texts[textIndex].text = ailmentText;
                    // 親オブジェクト（first, second, third, fourth）を表示
                    if (texts[textIndex].transform.parent != null)
                    {
                        texts[textIndex].transform.parent.gameObject.SetActive(true);
                    }
                    textIndex++;
                }
            }
        }

        Debug.Log($"[UIController] Player {player} の状態異常テキストを更新しました（表示数: {textIndex}）");
    }

    /// <summary>
    /// 配列インデックスから状態異常の種類を取得
    /// StatusAilments配列はインデックスで状態異常の種類を表す
    /// [0] = SleepApnea, [1] = Diabetes, [2] = BackPain, [3] = Heatstroke
    /// </summary>
    private StatusAilment IndexToStatusAilment(int index)
    {
        switch (index)
        {
            case 0: return StatusAilment.SleepApnea;
            case 1: return StatusAilment.Diabetes;
            case 2: return StatusAilment.BackPain;
            case 3: return StatusAilment.Heatstroke;
            default: return StatusAilment.SleepApnea; // フォールバック
        }
    }

    /// <summary>
    /// 状態異常に対応するテキストを取得
    /// </summary>
    private string GetStatusAilmentText(StatusAilment ailment)
    {
        switch (ailment)
        {
            case StatusAilment.SleepApnea: return "無呼吸";
            case StatusAilment.Diabetes: return "糖尿病";
            case StatusAilment.BackPain: return "腰痛";
            case StatusAilment.Heatstroke: return "熱中症";
            default: return null;
        }
    }
}
