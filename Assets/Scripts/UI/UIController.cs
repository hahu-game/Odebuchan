using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲーム内の全てのUI要素を管理し、ネットワーク同期されたデータに基づいて表示を更新する。
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    // === プレイヤー名の文字色設定 ===
    [Header("Player Name Text Colors")]
    [Tooltip("ホストプレイヤーの名前文字色（デフォルト: #800000 濃い赤）")]
    public Color hostPlayerNameColor = new Color(0.502f, 0.0f, 0.0f, 1.0f); // #800000

    [Tooltip("クライアントプレイヤーの名前文字色（デフォルト: #008000 緑）")]
    public Color clientPlayerNameColor = new Color(0.0f, 0.502f, 0.0f, 1.0f); // #008000

    // === 自分のUI要素 ===
    public TextMeshProUGUI myNameText;
    public TextMeshProUGUI myWeightText;
    public TextMeshProUGUI myEnergyText;
    public TextMeshProUGUI myEatWeightGainText;
    public TextMeshProUGUI mySleepEnergyGainText;

    // === 相手のUI要素 ===
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI opponentWeightText;
    public TextMeshProUGUI opponentEnergyText;
    public TextMeshProUGUI opponentEatWeightGainText;
    public TextMeshProUGUI opponentSleepEnergyGainText;

    // === 行動選択エリアの名前表示 ===
    public TextMeshProUGUI selectMyNameText;    // 行動選択エリアの自分の名前
    public TextMeshProUGUI selectOppNameText;   // 行動選択エリアの相手の名前

    // === 行動選択ボタン（3.1で追加） ===
    public UnityEngine.UI.Button eatButton;      // たべるボタン
    public UnityEngine.UI.Button sleepButton;    // ねむるボタン
    public UnityEngine.UI.Button playButton;     // あそぶボタン
    public UnityEngine.UI.Button clinicButton;   // つういんボタン

    // === 行動選択ボタン（特殊能力） ===
    // 9.3: specialAbilityButton/Text は削除されました（6つの個別ボタンに置き換え）

    // === 行動選択ボタンのOutline（選択フェーズ中に表示） ===
    [Header("Action Button Outlines")]
    public UnityEngine.UI.Outline eatButtonOutline;
    public UnityEngine.UI.Outline sleepButtonOutline;
    public UnityEngine.UI.Outline playButtonOutline;
    public UnityEngine.UI.Outline clinicButtonOutline;
    // 9.3: 特殊能力ボタンのアウトライン（6つ個別に）
    public UnityEngine.UI.Outline gaishokuButtonOutline;
    public UnityEngine.UI.Outline kintreButtonOutline;
    public UnityEngine.UI.Outline gamusharaButtonOutline;
    public UnityEngine.UI.Outline benkyouButtonOutline;
    public UnityEngine.UI.Outline jukusuiButtonOutline;
    public UnityEngine.UI.Outline dokaguiButtonOutline;

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
    // 9.3修正: パネルを4つに分割（病気とケガを分離）
    [Header("Status Ailment Panels")]
    public GameObject mySicknessPanel;     // 自分の病気パネル
    public GameObject myInjuryPanel;       // 自分のケガパネル
    public GameObject oppSicknessPanel;    // 相手の病気パネル
    public GameObject oppInjuryPanel;      // 相手のケガパネル

    [Header("Status Ailment Texts")]
    public TextMeshProUGUI[] mySicknessTexts = new TextMeshProUGUI[2];  // [0]=睡眠時無呼吸, [1]=糖尿病
    public TextMeshProUGUI[] myInjuryTexts = new TextMeshProUGUI[2];    // [0]=腰痛, [1]=熱中症
    public TextMeshProUGUI[] oppSicknessTexts = new TextMeshProUGUI[2]; // [0]=睡眠時無呼吸, [1]=糖尿病
    public TextMeshProUGUI[] oppInjuryTexts = new TextMeshProUGUI[2];   // [0]=腰痛, [1]=熱中症

    // === 7.1で追加: 特殊能力選択UI ===
    [Header("Special Ability Choice Panel")]
    public GameObject specialAbilityChoicePanel;
    public UnityEngine.UI.Button[] specialAbilityButtons = new UnityEngine.UI.Button[3];
    public TextMeshProUGUI[] specialAbilityButtonTexts = new TextMeshProUGUI[3];
    
    private SpecialAbilityType? _selectedAbility = null;

    // === 7.2で追加: 待機中パネル ===
    [Header("Waiting Panel")]
    public GameObject waitingForOpponentPanel;
    public TextMeshProUGUI waitingMessageText;
    private bool _isAbilitySelectionComplete = false;

    // === 8.3で追加: リザルトパネル ===
    [Header("Result Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultWinnerText;
    public TextMeshProUGUI resultDayText;
    public TextMeshProUGUI resultMyNameText;
    public TextMeshProUGUI resultMyWeightText;
    public TextMeshProUGUI resultMyEnergyText;
    public TextMeshProUGUI resultMyAbilityText;
    public TextMeshProUGUI resultMyJankenWinText;
    public TextMeshProUGUI resultOppNameText;
    public TextMeshProUGUI resultOppWeightText;
    public TextMeshProUGUI resultOppEnergyText;
    public TextMeshProUGUI resultOppAbilityText;
    public TextMeshProUGUI resultOppJankenWinText;
    public UnityEngine.UI.Button resultReturnToTitleButton;

    // === 9.2で追加: 設定パネル ===
    // SettingControllerはシングルトン（DontDestroyOnLoad）になったため、
    // SettingController.Instanceを使用する

    // === 9.3で追加: 行動選択時の効果予測表示（アイコン+数値分離表示） ===
    // アイコンは常に表示されるため、数値テキストのみをスクリプトから制御
    [Header("Action Effect Display - Eat (たべる)")]
    public TextMeshProUGUI eatEnergyValueText;
    public TextMeshProUGUI eatWeightValueText;
    public TextMeshProUGUI eatSicknessProbabilityText;

    [Header("Action Effect Display - Sleep (ねむる)")]
    public TextMeshProUGUI sleepEnergyValueText;

    [Header("Action Effect Display - Play (あそぶ)")]
    public TextMeshProUGUI playEnergyValueText;
    public TextMeshProUGUI playEnergyBuffText;
    public TextMeshProUGUI playWeightBuffText;
    public TextMeshProUGUI playInjuryProbabilityText;

    [Header("Action Effect Display - Clinic (つういん)")]
    public TextMeshProUGUI clinicEnergyValueText;

    // === 連続使用デバフ表示 ===
    [Header("Consecutive Use Penalty Indicators - Basic Actions")]
    public GameObject eatConsecutiveIndicator;
    public GameObject sleepConsecutiveIndicator;
    public GameObject playConsecutiveIndicator;
    public GameObject clinicConsecutiveIndicator;


    // === 9.3で追加: 特殊能力ボタン（6つの個別ボタン） ===
    [Header("Special Ability Buttons (がいしょく)")]
    public UnityEngine.UI.Button gaishokuButton;
    public TextMeshProUGUI gaishokuEnergyValueText;
    public TextMeshProUGUI gaishokuWeightValueText;
    public TextMeshProUGUI gaishokuSicknessProbabilityText;

    [Header("Special Ability Buttons (きんとれ)")]
    public UnityEngine.UI.Button kintreButton;
    public TextMeshProUGUI kintreEnergyValueText;
    public TextMeshProUGUI kintreWeightValueText;
    public TextMeshProUGUI kintreEnergyBuffText;
    public TextMeshProUGUI kintreWeightBuffText;
    public TextMeshProUGUI kintreInjuryProbabilityText;

    [Header("Special Ability Buttons (がむしゃら)")]
    public UnityEngine.UI.Button gamusharaButton;

    [Header("Special Ability Buttons (べんきょう)")]
    public UnityEngine.UI.Button benkyouButton;
    public TextMeshProUGUI benkyouEnergyValueText;
    public TextMeshProUGUI benkyouWeightBuffText;
    public TextMeshProUGUI benkyouStudyComboText;

    [Header("Special Ability Buttons (じゅくすい)")]
    public UnityEngine.UI.Button jukusuiButton;
    public TextMeshProUGUI jukusuiEnergyValueText;

    [Header("Special Ability Buttons (どかぐい)")]
    public UnityEngine.UI.Button dokaguiButton;
    public TextMeshProUGUI dokaguiEnergyValueText;
    public TextMeshProUGUI dokaguiWeightValueText;
    public TextMeshProUGUI dokaguiSicknessProbabilityText;

    [Header("Consecutive Use Penalty Indicators - Special Abilities")]
    public GameObject specialAbilityConsecutiveIndicator;


    [Header("Action Tooltip Panels (ホバー時の詳細表示)")]
    public GameObject eatTooltipPanel;
    public GameObject sleepTooltipPanel;
    public GameObject playTooltipPanel;
    public GameObject clinicTooltipPanel;
    public GameObject specialAbilityTooltipPanel;

    public TextMeshProUGUI eatTooltipText;
    public TextMeshProUGUI sleepTooltipText;
    public TextMeshProUGUI playTooltipText;
    public TextMeshProUGUI clinicTooltipText;
    public TextMeshProUGUI specialAbilityTooltipText;

    // === 3.4で追加: ブラックアウトパネルとログエリア ===
    [Header("Blackout Panel")]
    public GameObject blackoutPanel;
    public TextMeshProUGUI blackoutText;
    private Coroutine _blackoutCoroutine;

    [Header("Log Area")]
    public UnityEngine.UI.ScrollRect logScrollRect;
    public Transform logContent;
    public GameObject logTextPrefab; // TextMeshProUGUIを持つプレハブ
    
    [Header("Log Settings")]
    [Tooltip("ログエントリの最大数（この数を超えると古いエントリが自動削除されます）")]
    public int maxLogEntries = 100; // デフォルト100件

    private Dictionary<PlayerRef, UyopyonState> _uyopyons = new Dictionary<PlayerRef, UyopyonState>();
    
    // スクロールコルーチンの参照（重複実行を防ぐため）
    private Coroutine _scrollToBottomCoroutine = null;

    // === パラメータアニメーション関連 ===
    private ParameterAnimation _parameterAnimation;
    private Dictionary<PlayerRef, int> _lastWeights = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, int> _lastEnergies = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, int> _lastEatWeightGains = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, int> _lastSleepEnergyGains = new Dictionary<PlayerRef, int>();

    /// <summary>
    /// 前回値を強制的に更新（初期化時のアニメーション実行を防ぐ）
    /// </summary>
    public void UpdateLastValues(PlayerRef player, int weight, int energy, int eatWeightGain, int sleepEnergyGain)
    {
        _lastWeights[player] = weight;
        _lastEnergies[player] = energy;
        _lastEatWeightGains[player] = eatWeightGain;
        _lastSleepEnergyGains[player] = sleepEnergyGain;
    }

    // === 3.2で追加: 行動選択状態 ===
    private ActionData? _morningAction = null;      // 午前に選択した行動
    private ActionData? _afternoonAction = null;    // 午後に選択した行動
    private bool _isMorningSelected = false;        // 午前が選択済みか
    private PlayerRef _localPlayerRef;

    // === 選択フェーズ開始時の重さ合計（じゅくすい・どかぐいの条件判定用） ===
    private int _selectionPhaseStartWeightTotal = 0;

    // === フォントサイズのデフォルト値保存 ===
    // 9.3: _defaultSpecialAbilityButtonTextSize は削除されました
    private float _defaultActionTextSize;



    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        var runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.LocalPlayer.PlayerId != 0)
        {
         _localPlayerRef = runner.LocalPlayer;
        }

        // デフォルトのフォントサイズを保存
        // 9.3: specialAbilityButtonText は削除されました
        if (myTodayMorningActionText != null)
        {
            _defaultActionTextSize = myTodayMorningActionText.fontSize;
        }

        // パラメータアニメーションコンポーネントを追加
        _parameterAnimation = gameObject.AddComponent<ParameterAnimation>();
    }

    private void Start()
    {
        Debug.Log("[UIController] Start: 初期化開始");
        // 初期状態で午前選択中のハイライトを表示
        HighlightCurrentSelection(false);
        // 初期状態では行動ボタンのOutlineを非表示
        HideActionButtonOutlines();

        // 8.3: リザルトパネルの初期化
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
            Debug.Log("[UIController] リザルトパネルを非表示に設定");
        }
        
        // ゲーム開始時にログをクリア（メモリリーク防止）
        ClearLog();

        // 9.3: 6つの特殊能力ボタンにクリックリスナーを追加（それぞれの特殊能力タイプを指定）
        // 古いリスナーを削除してから新しいリスナーを追加
        if (gaishokuButton != null)
        {
            gaishokuButton.onClick.RemoveAllListeners();
            gaishokuButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Gaishoku));
        }
        if (kintreButton != null)
        {
            kintreButton.onClick.RemoveAllListeners();
            kintreButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Kintre));
        }
        if (gamusharaButton != null)
        {
            gamusharaButton.onClick.RemoveAllListeners();
            gamusharaButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Gamushara));
        }
        if (benkyouButton != null)
        {
            benkyouButton.onClick.RemoveAllListeners();
            benkyouButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Benkyou));
        }
        if (jukusuiButton != null)
        {
            jukusuiButton.onClick.RemoveAllListeners();
            jukusuiButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Jukusui));
        }
        if (dokaguiButton != null)
        {
            dokaguiButton.onClick.RemoveAllListeners();
            dokaguiButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Dokagui));
        }

        // 9.3: 状態異常パネルの初期化（初期状態では全て非表示）
        if (mySicknessPanel != null) mySicknessPanel.SetActive(false);
        if (myInjuryPanel != null) myInjuryPanel.SetActive(false);
        if (oppSicknessPanel != null) oppSicknessPanel.SetActive(false);
        if (oppInjuryPanel != null) oppInjuryPanel.SetActive(false);

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

            // 注: 前回値はUpdateDisplay()の後にUpdateLastValues()で設定されるため、ここでは登録しない
            // これにより、RegisterUyopyon()とInitializeValues()の間の値変更でアニメーションが実行されることを防ぐ
        }
    }

    // === プレイヤー名更新（NetworkPlayerから呼ばれる） ===
    public void UpdateMyName(string newName, PlayerRef playerRef)
    {
        bool isHost = IsHostPlayer(playerRef);

        myNameText.text = newName;
        SetPlayerNameTextColor(myNameText, isHost);

        // 行動選択エリアの名前も更新
        if (selectMyNameText != null)
        {
            selectMyNameText.text = newName;
            SetPlayerNameTextColor(selectMyNameText, isHost);
        }
    }

    public void UpdateOpponentName(string newName, PlayerRef playerRef)
    {
        bool isHost = IsHostPlayer(playerRef);

        opponentNameText.text = newName;
        SetPlayerNameTextColor(opponentNameText, isHost);

        // 行動選択エリアの名前も更新
        if (selectOppNameText != null)
        {
            selectOppNameText.text = newName;
            SetPlayerNameTextColor(selectOppNameText, isHost);
        }
    }

    // === ステータス更新（UyopyonStateから呼ばれる） ===

    public void UpdateWeightDisplay(PlayerRef player, int weight)
    {
        // アニメーション対象のテキストコンポーネントを取得
        TextMeshProUGUI targetText = (player == _localPlayerRef) ? myWeightText : opponentWeightText;

        if (targetText == null)
        {
            return;
        }

        // 前回の値を取得（初回の場合は現在の値をそのまま使用）
        if (!_lastWeights.ContainsKey(player))
        {
            _lastWeights[player] = weight;
            SetTextIfChanged(targetText, weight.ToString());
            return;
        }

        int fromValue = _lastWeights[player];

        // アニメーション実行
        if (_parameterAnimation != null)
        {
            _parameterAnimation.AnimateParameter(targetText, fromValue, weight);
        }
        else
        {
            // フォールバック: アニメーションコンポーネントがない場合は直接表示
            SetTextIfChanged(targetText, weight.ToString());
        }

        // 前回値を更新
        _lastWeights[player] = weight;
    }

    public void UpdateEnergyDisplay(PlayerRef player, int energy)
    {
        // アニメーション対象のテキストコンポーネントを取得
        TextMeshProUGUI targetText = (player == _localPlayerRef) ? myEnergyText : opponentEnergyText;

        if (targetText == null)
        {
            return;
        }

        // 前回の値を取得（初回の場合は現在の値をそのまま使用）
        if (!_lastEnergies.ContainsKey(player))
        {
            _lastEnergies[player] = energy;
            SetTextIfChanged(targetText, energy.ToString());
            return;
        }

        int fromValue = _lastEnergies[player];

        // アニメーション実行
        if (_parameterAnimation != null)
        {
            _parameterAnimation.AnimateParameter(targetText, fromValue, energy);
        }
        else
        {
            // フォールバック: アニメーションコンポーネントがない場合は直接表示
            SetTextIfChanged(targetText, energy.ToString());
        }

        // 前回値を更新
        _lastEnergies[player] = energy;
    }

    // TODO: フェーズ3で実装予定 - あそぶバフの表示
    public void UpdatePlayBuffDisplay(PlayerRef player, int buffWeight, int buffEnergy) { }

    /// <summary>
    /// たべる使用時の重さ上昇量を表示
    /// </summary>
    public void UpdateEatWeightGainDisplay(PlayerRef player, int eatWeightGain)
    {
        // アニメーション対象のテキストコンポーネントを取得
        TextMeshProUGUI targetText = (player == _localPlayerRef) ? myEatWeightGainText : opponentEatWeightGainText;

        if (targetText == null)
        {
            return;
        }

        // 前回の値を取得（初回の場合は現在の値をそのまま使用）
        if (!_lastEatWeightGains.ContainsKey(player))
        {
            _lastEatWeightGains[player] = eatWeightGain;
            string sign = eatWeightGain >= 0 ? "+" : "";
            targetText.text = $"{sign}{eatWeightGain}";
            targetText.color = Color.black;
            return;
        }

        int fromValue = _lastEatWeightGains[player];

        // アニメーション実行
        if (_parameterAnimation != null)
        {
            _parameterAnimation.AnimateParameterWithSign(targetText, fromValue, eatWeightGain);
        }
        else
        {
            // フォールバック: アニメーションコンポーネントがない場合は直接表示
            string sign = eatWeightGain >= 0 ? "+" : "";
            targetText.text = $"{sign}{eatWeightGain}";
        }

        // 前回値を更新
        _lastEatWeightGains[player] = eatWeightGain;
    }

    /// <summary>
    /// ねむる使用時の元気上昇量を表示
    /// </summary>
    public void UpdateSleepEnergyGainDisplay(PlayerRef player, int sleepEnergyGain)
    {
        // アニメーション対象のテキストコンポーネントを取得
        TextMeshProUGUI targetText = (player == _localPlayerRef) ? mySleepEnergyGainText : opponentSleepEnergyGainText;

        if (targetText == null)
        {
            return;
        }

        // 前回の値を取得（初回の場合は現在の値をそのまま使用）
        if (!_lastSleepEnergyGains.ContainsKey(player))
        {
            _lastSleepEnergyGains[player] = sleepEnergyGain;
            string sign = sleepEnergyGain >= 0 ? "+" : "";
            targetText.text = $"{sign}{sleepEnergyGain}";
            targetText.color = Color.black;
            return;
        }

        int fromValue = _lastSleepEnergyGains[player];

        // アニメーション実行
        if (_parameterAnimation != null)
        {
            _parameterAnimation.AnimateParameterWithSign(targetText, fromValue, sleepEnergyGain);
        }
        else
        {
            // フォールバック: アニメーションコンポーネントがない場合は直接表示
            string sign = sleepEnergyGain >= 0 ? "+" : "";
            targetText.text = $"{sign}{sleepEnergyGain}";
        }

        // 前回値を更新
        _lastSleepEnergyGains[player] = sleepEnergyGain;
    }

    /// <summary>
    /// 進化状態の表示を更新（TODO 3実装）
    /// 9.3: specialAbilityButton は削除され、6つの個別ボタンに置き換えられました
    /// 各ボタンの表示/非表示は UpdateAllActionEffectDisplay() で管理されます
    /// </summary>
    public void UpdateEvolutionDisplay(PlayerRef player, bool hasEvolved, string abilityName)
    {
        Debug.Log($"[UIController] UpdateEvolutionDisplay: player={player}, hasEvolved={hasEvolved}, abilityName={abilityName}");

        // 自分のプレイヤーの場合のみ特殊能力ボタンを制御
        if (!IsMyPlayer(player))
        {
            Debug.Log($"[UIController] Player {player} は自分ではないため、特殊能力ボタンの制御をスキップ");
            return;
        }

        // 9.3: specialAbilityButton は削除されました
        // 特殊能力ボタンの表示/非表示は UpdateAllActionEffectDisplay() で自動的に管理されます
        Debug.Log($"[UIController] 進化状態更新: hasEvolved={hasEvolved}, abilityName={abilityName}");

        // ボタンの状態更新は選択フェーズ開始時（UpdateActionButtonsBasedOnEnergy）で行われる
        // 進化時は選択フェーズではないため、ここでは呼ばない
    }

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

        // ログエントリの最大数を超えている場合、古いエントリを削除
        int currentLogCount = logContent.childCount;
        if (currentLogCount >= maxLogEntries)
        {
            // 古いエントリを削除（最初の子要素から順に削除）
            int entriesToRemove = currentLogCount - maxLogEntries + 1; // +1は新しいエントリの分
            for (int i = 0; i < entriesToRemove; i++)
            {
                Transform oldestChild = logContent.GetChild(0);
                if (oldestChild != null)
                {
                    Destroy(oldestChild.gameObject);
                }
            }
            Debug.Log($"[UIController] ログエントリが最大数({maxLogEntries})を超えたため、古いエントリ{entriesToRemove}件を削除しました");
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
            // ログを表示（タイムスタンプなし）
            logText.text = message;

            // ログテキストの色を黒に設定（デフォルト色、リッチテキストタグで上書き可能）
            logText.color = Color.black;

            // リッチテキストを有効化（プレイヤー名の色付けのため）
            logText.richText = true;

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

        // 既存の余白スペーサーを削除（あれば）
        Transform existingSpacer = logContent.Find("BottomSpacer");
        if (existingSpacer != null)
        {
            Destroy(existingSpacer.gameObject);
        }

        // 新しい余白スペーサーを追加（最後のログの下に余白を作る）
        GameObject spacer = new GameObject("BottomSpacer");
        spacer.transform.SetParent(logContent, false);
        RectTransform spacerRect = spacer.AddComponent<RectTransform>();
        spacerRect.anchorMin = new Vector2(0, 1);
        spacerRect.anchorMax = new Vector2(1, 1);
        spacerRect.pivot = new Vector2(0, 1);
        spacerRect.sizeDelta = new Vector2(0, 60f); // 高さ60pxの余白
        spacer.transform.SetAsLastSibling(); // 最後に配置

        // 自動的に最下部にスクロール（重複実行を防ぐ）
        if (_scrollToBottomCoroutine != null)
        {
            StopCoroutine(_scrollToBottomCoroutine);
        }
        _scrollToBottomCoroutine = StartCoroutine(ScrollToBottom());
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

        // コルーチン完了時に参照をクリア
        _scrollToBottomCoroutine = null;
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
    /// 「たべる」ボタンがクリックされた時の処理
    /// </summary>
    public void OnEatButtonClicked()
    {
        Debug.Log("[UIController] OnEatButtonClicked が呼ばれました");
        OnActionButtonClicked(ActionType.Eat);
    }

    /// <summary>
    /// 「ねむる」ボタンがクリックされた時の処理
    /// </summary>
    public void OnSleepButtonClicked()
    {
        OnActionButtonClicked(ActionType.Sleep);
    }

    /// <summary>
    /// 「あそぶ」ボタンがクリックされた時の処理
    /// </summary>
    public void OnPlayButtonClicked()
    {
        OnActionButtonClicked(ActionType.Play);
    }

    /// <summary>
    /// 「つういん」ボタンがクリックされた時の処理
    /// </summary>
    public void OnClinicButtonClicked()
    {
        OnActionButtonClicked(ActionType.Clinic);
    }

    /// <summary>
    /// TODO 5: 特殊能力ボタンがクリックされた時の処理
    /// </summary>
    /// <summary>
    /// 6つの特殊能力ボタンがクリックされた時の処理（特殊能力タイプを指定）
    /// </summary>
    public void OnSpecialAbilityButtonClicked(SpecialAbilityType abilityType)
    {
        Debug.Log($"[UIController] OnSpecialAbilityButtonClicked({abilityType}) が呼ばれました");
        OnActionButtonClicked(ActionType.SpecialAbility, abilityType);
    }

    /// <summary>
    /// 旧: 特殊能力ボタンがクリックされた時の処理（非推奨：6つの個別ボタンに移行）
    /// </summary>
    public void OnSpecialAbilityButtonClicked()
    {
        Debug.Log("[UIController] OnSpecialAbilityButtonClicked が呼ばれました");
        OnActionButtonClicked(ActionType.SpecialAbility);
    }


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

        // 自分のPlayerActionDataを取得（GameManagerのDictionaryから直接取得）
        PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(localPlayer);

        if (myActionData != null)
        {
            // 行動を確定（行動は既に選択時に送信済み）
            myActionData.RPC_FixActions();

            Debug.Log($"[UIController] 行動を確定しました: Player={myActionData.OwnerPlayer}, 午前={_morningAction.Value.Type}, 午後={_afternoonAction.Value.Type}");

            // LogAreaに確定メッセージを表示
            AddLog("行動を確定しました。対戦相手の選択を待っています。");

            // 確定SE再生
            AudioManager.Instance?.PlayActionConfirmSE();

            // 確定後、行動ボタンと確定・クリアボタンを無効化（ロック）
            LockActionButtons();
        }
        else
        {
            Debug.LogError($"[UIController] OwnerPlayer={localPlayer}のPlayerActionDataが見つかりません");
        }
    }

    /// <summary>
    /// 「クリア」ボタンがクリックされた時の処理
    /// 選択した行動をリセットする
    /// </summary>
    public void OnClearButtonClicked()
    {
        // 連続使用表示を全て非表示にする
        HideAllConsecutiveUseIndicators();

        // バグ4対応: 熱中症による午前ロックをチェック
        var runner = FindFirstObjectByType<NetworkRunner>();
        bool isMorningLocked = false;
        PlayerActionData myActionData = null;

        if (runner != null)
        {
            myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
            if (myActionData != null && myActionData.MorningActionLocked)
            {
                isMorningLocked = true;
                Debug.Log("[UIController] 熱中症により午前がロックされています（クリア時）");
            }
        }

        if (isMorningLocked)
        {
            // 熱中症時: 午後の選択のみクリア、午前はつういん固定のまま
            _afternoonAction = null;
            // _morningAction と _isMorningSelected はそのまま維持

            Debug.Log("[UIController] 熱中症対応: 午後の選択のみクリアしました");

            // クリアSE再生
            AudioManager.Instance?.PlayActionClearSE();

            // 午後のパネル表示のみクリア
            if (myTodayAfternoonActionText != null)
            {
                myTodayAfternoonActionText.text = "";
                myTodayAfternoonActionText.fontSize = _defaultActionTextSize;
            }

            // 午後選択中のハイライトを維持
            HighlightCurrentSelection(true);

            // PlayerActionDataの午後のみクリア
            if (myActionData != null)
            {
                myActionData.RPC_ClearAfternoonAction();
            }

            // 午後選択状態で表示を更新してから元気チェックを再実行
            UpdateAllActionEffectDisplay();
            UpdateActionButtonsBasedOnEnergy();
            Debug.Log("[UIController] 熱中症対応: 午後のみクリア完了");
        }
        else
        {
            // 通常時: すべてクリア
            _morningAction = null;
            _afternoonAction = null;
            _isMorningSelected = false;

            Debug.Log("[UIController] 選択をクリアしました");

            // クリアSE再生
            AudioManager.Instance?.PlayActionClearSE();

            // パネル表示もクリア
            if (myTodayMorningActionText != null)
            {
                myTodayMorningActionText.text = "";
                myTodayMorningActionText.fontSize = _defaultActionTextSize;
            }
            if (myTodayAfternoonActionText != null)
            {
                myTodayAfternoonActionText.text = "";
                myTodayAfternoonActionText.fontSize = _defaultActionTextSize;
            }

            // 午前選択中のハイライトに戻す
            HighlightCurrentSelection(false);

            // PlayerActionDataをクリア
            if (myActionData != null)
            {
                myActionData.RPC_ClearActions();
            }

            // 行動クリア後、表示を更新してから元気チェックを再実行
            UpdateAllActionEffectDisplay();
            UpdateActionButtonsBasedOnEnergy();
            Debug.Log("[UIController] クリアボタン押下後、元気チェックをリセットしました");
        }
    }

    // ... その他、ラウンド表示、メッセージ表示などのメソッド ...



    /// <summary>
    /// 6つの特殊能力ボタンがクリックされた時の処理（特殊能力タイプを直接指定）
    /// 9.3: べんきょうボタン修正 - 特殊能力タイプを直接渡すように変更
    /// </summary>
    private void OnActionButtonClicked(ActionType actionType, SpecialAbilityType abilityType)
    {
        Debug.Log($"[UIController] OnActionButtonClicked 開始: actionType={actionType}, abilityType={abilityType}");

        // 行動選択SE再生
        AudioManager.Instance?.PlayActionSelectSE();

        // ActionDataを生成
        ActionData selectedAction = ActionData.CreateSpecialAbility(abilityType);
        Debug.Log($"[UIController] ActionData作成成功: {abilityType}");

        // 午前が未選択なら午前に設定、選択済みなら午後に設定
        if (!_isMorningSelected)
        {
            _morningAction = selectedAction;
            _isMorningSelected = true;
            Debug.Log($"[UIController] 午前の行動を選択: {abilityType}");

                        // パネル表示を更新（自分の午前）
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null)
            {
                // PlayerActionDataに即座に反映（時間切れ時の判定用）
                PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
                if (myActionData != null)
                {
                    myActionData.RPC_SetMorningAction(selectedAction);
                    Debug.Log($"[UIController] 午前の行動をPlayerActionDataに送信: {selectedAction.Type}");
                }

                UpdateActionDisplay(runner.LocalPlayer, true, selectedAction);
            }

            HighlightCurrentSelection(false); // 午前選択中を表示

            // 午前の行動が選択されたので、午後のボタン状態を更新
            // 9.3修正: 午後の行動ボタンの効果表示を更新（連続使用デバフを反映）
            UpdateAllActionEffectDisplay();
            UpdateActionButtonsBasedOnEnergy();
            Debug.Log($"[UIController] 午前の行動選択後、午後のボタン状態を更新しました");
        }
        else
        {
            _afternoonAction = selectedAction;
            Debug.Log($"[UIController] 午後の行動を選択: {abilityType}");

                        // パネル表示を更新（自分の午後）
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null)
            {
                // PlayerActionDataに即座に反映（時間切れ時の判定用）
                PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
                if (myActionData != null)
                {
                    myActionData.RPC_SetAfternoonAction(selectedAction);
                    Debug.Log($"[UIController] 午後の行動をPlayerActionDataに送信: {selectedAction.Type}");
                }

                UpdateActionDisplay(runner.LocalPlayer, false, selectedAction);
            }

            // 午前・午後の両方が選択されたので、選択完了
            HighlightCurrentSelection(false); // ハイライト解除（未選択状態に戻す）
        }
    }

    /// <summary>
    /// 行動ボタンがクリックされた時の共通処理（3.2で追加）
    /// </summary>
    /// <param name="actionType">選択された行動の種類</param>
    private void OnActionButtonClicked(ActionType actionType)
    {
        Debug.Log($"[UIController] OnActionButtonClicked 開始: actionType={actionType}");

        // 行動選択SE再生
        AudioManager.Instance?.PlayActionSelectSE();

        // ActionTypeに対応するActionDataを生成
        ActionData selectedAction;

        if (actionType == ActionType.SpecialAbility)
        {
            // TODO 5: 特殊能力の場合は、SpecialAbilityTypeを取得してActionDataを生成
            Debug.Log("[UIController] 特殊能力ボタンが押されました。GetMySpecialAbilityType()を呼び出します");
            SpecialAbilityType? abilityType = GetMySpecialAbilityType();
            Debug.Log($"[UIController] GetMySpecialAbilityType() の結果: {(abilityType.HasValue ? abilityType.Value.ToString() : "null")}");

            if (abilityType.HasValue)
            {
                selectedAction = ActionData.CreateSpecialAbility(abilityType.Value);
                Debug.Log($"[UIController] ActionData作成成功: {abilityType.Value}");
            }
            else
            {
                Debug.LogWarning("[UIController] 特殊能力が設定されていません");
                return;
            }
        }
        else
        {
            selectedAction = actionType switch
            {
                ActionType.Eat => ActionData.CreateEat(),
                ActionType.Sleep => ActionData.CreateSleep(),
                ActionType.Play => ActionData.CreatePlay(),
                ActionType.Clinic => ActionData.CreateClinic(),
                _ => ActionData.Default()
            };
        }

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
                // PlayerActionDataに即座に反映（時間切れ時の判定用）
                PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
                if (myActionData != null)
                {
                    myActionData.RPC_SetMorningAction(selectedAction);
                    Debug.Log($"[UIController] 午前の行動をPlayerActionDataに送信: {selectedAction.Type}");
                }

                UpdateActionDisplay(runner.LocalPlayer, true, selectedAction);
                HighlightCurrentSelection(false); // 午前選択中を表示
            }

            // 午前の行動が選択されたので、午後の選択肢を午前+午後の合計で再チェック
            // 9.3修正: 午後の行動ボタンの効果表示を更新（連続使用デバフを反映）
            UpdateAllActionEffectDisplay();
            UpdateActionButtonsBasedOnEnergy();
            Debug.Log($"[UIController] 午前の行動選択後、午後のボタン状態を更新しました");
        }
        else
        {
            _afternoonAction = selectedAction;
            Debug.Log($"[UIController] 午後の行動を選択: {actionType}");

                        // パネル表示を更新（自分の午後）
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null)
            {
                // PlayerActionDataに即座に反映（時間切れ時の判定用）
                PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
                if (myActionData != null)
                {
                    myActionData.RPC_SetAfternoonAction(selectedAction);
                    Debug.Log($"[UIController] 午後の行動をPlayerActionDataに送信: {selectedAction.Type}");
                }

                UpdateActionDisplay(runner.LocalPlayer, false, selectedAction);
                HighlightCurrentSelection(true); // 午後選択中を表示
            }
        }
    }

    // === 3.3で追加: 行動表示メソッド ===

    /// <summary>
    /// 行動表示を更新
    /// </summary>
    public void UpdateActionDisplay(PlayerRef player, bool isMorning, ActionData actionData)
    {
        Debug.Log($"[UIController] UpdateActionDisplay: player={player}, isMorning={isMorning}, action={actionData.Type}");

        // 特殊能力の場合は、具体的な特殊能力名を取得
        string actionText;
        if (actionData.Type == ActionType.SpecialAbility)
        {
            // ActionDataに含まれる特殊能力名を使用
            string abilityName = actionData.SpecialAbilityName.ToString();
            Debug.Log($"[UIController] 特殊能力名（ActionDataから）: '{abilityName}'");

            if (!string.IsNullOrEmpty(abilityName) && System.Enum.TryParse<SpecialAbilityType>(abilityName, out SpecialAbilityType abilityType))
            {
                actionText = GetSpecialAbilityDisplayName(abilityType);
                Debug.Log($"[UIController] 特殊能力表示名: '{actionText}'");
            }
            else
            {
                Debug.LogWarning($"[UIController] 特殊能力名が不正: '{abilityName}'");
                actionText = "特殊能力";
            }
        }
        else
        {
            actionText = GetActionText(actionData.Type);
        }

        // 自分のプレイヤーか判定
        bool isMyPlayer = IsMyPlayer(player);

        if (isMyPlayer)
        {
            if (isMorning)
            {
                myTodayMorningActionText.text = actionText;
                SetFontSizeForSpecialAbility(myTodayMorningActionText, actionText, _defaultActionTextSize, 160f);
            }
            else
            {
                myTodayAfternoonActionText.text = actionText;
                SetFontSizeForSpecialAbility(myTodayAfternoonActionText, actionText, _defaultActionTextSize, 160f);
            }
        }
        else
        {
            if (isMorning)
            {
                oppTodayMorningActionText.text = actionText;
                SetFontSizeForSpecialAbility(oppTodayMorningActionText, actionText, _defaultActionTextSize, 160f);
            }
            else
            {
                oppTodayAfternoonActionText.text = actionText;
                SetFontSizeForSpecialAbility(oppTodayAfternoonActionText, actionText, _defaultActionTextSize, 160f);
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
                SetFontSizeForSpecialAbility(myTodayMorningActionText, customText, _defaultActionTextSize, 160f);
            }
            else
            {
                myTodayAfternoonActionText.text = customText;
                SetFontSizeForSpecialAbility(myTodayAfternoonActionText, customText, _defaultActionTextSize, 160f);
            }
        }
        else
        {
            if (isMorning)
            {
                oppTodayMorningActionText.text = customText;
                SetFontSizeForSpecialAbility(oppTodayMorningActionText, customText, _defaultActionTextSize, 160f);
            }
            else
            {
                oppTodayAfternoonActionText.text = customText;
                SetFontSizeForSpecialAbility(oppTodayAfternoonActionText, customText, _defaultActionTextSize, 160f);
            }
        }
    }

    /// <summary>
    /// 昨日の午後の行動表示を更新
    /// </summary>
    public void UpdateYesterdayAfternoonDisplay(PlayerRef player, ActionData actionData)
    {
        Debug.Log($"[UIController] UpdateYesterdayAfternoonDisplay: player={player}, action={actionData.Type}");

        // 特殊能力の場合は具体的な能力名を取得
        string actionText;
        if (actionData.Type == ActionType.SpecialAbility)
        {
            // ActionDataに含まれる特殊能力名を使用
            string abilityName = actionData.SpecialAbilityName.ToString();
            Debug.Log($"[UIController] 特殊能力名（ActionDataから）: '{abilityName}'");

            if (!string.IsNullOrEmpty(abilityName) && System.Enum.TryParse<SpecialAbilityType>(abilityName, out SpecialAbilityType abilityType))
            {
                actionText = GetSpecialAbilityDisplayName(abilityType);
                Debug.Log($"[UIController] 特殊能力表示名: '{actionText}'");
            }
            else
            {
                Debug.LogWarning($"[UIController] 特殊能力名が不正: '{abilityName}'");
                actionText = "特殊能力";
            }
        }
        else
        {
            actionText = GetActionText(actionData.Type);
        }

        bool isMyPlayer = IsMyPlayer(player);

        if (isMyPlayer)
        {
            if (myYesterdayAfternoonActionText != null)
            {
                myYesterdayAfternoonActionText.text = actionText;
                SetFontSizeForSpecialAbility(myYesterdayAfternoonActionText, actionText, _defaultActionTextSize, 160f);
            }
        }
        else
        {
            if (oppYesterdayAfternoonActionText != null)
            {
                oppYesterdayAfternoonActionText.text = actionText;
                SetFontSizeForSpecialAbility(oppYesterdayAfternoonActionText, actionText, _defaultActionTextSize, 160f);
            }
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

        // 9.3: 特殊能力ボタンのアウトラインも表示（6つ個別に、アクティブな場合のみ）
        if (gaishokuButtonOutline != null && gaishokuButton != null && gaishokuButton.gameObject.activeSelf)
        {
            gaishokuButtonOutline.effectColor = outlineColor;
            gaishokuButtonOutline.effectDistance = outlineDistance;
            gaishokuButtonOutline.enabled = true;
        }

        if (kintreButtonOutline != null && kintreButton != null && kintreButton.gameObject.activeSelf)
        {
            kintreButtonOutline.effectColor = outlineColor;
            kintreButtonOutline.effectDistance = outlineDistance;
            kintreButtonOutline.enabled = true;
        }

        if (gamusharaButtonOutline != null && gamusharaButton != null && gamusharaButton.gameObject.activeSelf)
        {
            gamusharaButtonOutline.effectColor = outlineColor;
            gamusharaButtonOutline.effectDistance = outlineDistance;
            gamusharaButtonOutline.enabled = true;
        }

        if (benkyouButtonOutline != null && benkyouButton != null && benkyouButton.gameObject.activeSelf)
        {
            benkyouButtonOutline.effectColor = outlineColor;
            benkyouButtonOutline.effectDistance = outlineDistance;
            benkyouButtonOutline.enabled = true;
        }

        if (jukusuiButtonOutline != null && jukusuiButton != null && jukusuiButton.gameObject.activeSelf)
        {
            jukusuiButtonOutline.effectColor = outlineColor;
            jukusuiButtonOutline.effectDistance = outlineDistance;
            jukusuiButtonOutline.enabled = true;
        }

        if (dokaguiButtonOutline != null && dokaguiButton != null && dokaguiButton.gameObject.activeSelf)
        {
            dokaguiButtonOutline.effectColor = outlineColor;
            dokaguiButtonOutline.effectDistance = outlineDistance;
            dokaguiButtonOutline.enabled = true;
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

        // 9.3: 特殊能力ボタンの制御（6つ個別に、アクティブなボタンのみ）
        // 有効化の場合は GameFlowManager から UpdateActionButtonsBasedOnEnergy() が呼ばれる
        // そこで UpdateSpecialAbilityButtonState() が実行される
        if (!interactable)
        {
            // 無効化する場合は、すべての特殊能力ボタンを無効化
            if (gaishokuButton != null && gaishokuButton.gameObject.activeSelf)
                gaishokuButton.interactable = false;
            if (kintreButton != null && kintreButton.gameObject.activeSelf)
                kintreButton.interactable = false;
            if (gamusharaButton != null && gamusharaButton.gameObject.activeSelf)
                gamusharaButton.interactable = false;
            if (benkyouButton != null && benkyouButton.gameObject.activeSelf)
                benkyouButton.interactable = false;
            if (jukusuiButton != null && jukusuiButton.gameObject.activeSelf)
                jukusuiButton.interactable = false;
            if (dokaguiButton != null && dokaguiButton.gameObject.activeSelf)
                dokaguiButton.interactable = false;
        }

        // 確定・クリアボタンも同時に制御
        if (fixButton != null) fixButton.interactable = interactable;
        if (clearButton != null) clearButton.interactable = interactable;
    }

    /// <summary>
    /// 現在の元気に基づいて、実行可能な行動のボタンのみを有効にする
    /// 選択フェーズ開始時に呼ばれる
    /// </summary>
    public void UpdateActionButtonsBasedOnEnergy()
    {
        // LocalPlayerのUyopyonStateを取得
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null) return;

        PlayerRef localPlayer = runner.LocalPlayer;
        UyopyonState myState = GameManager.Instance?.GetUyopyonState(localPlayer);
        if (myState == null || GameManager.Instance?.gameParams == null) return;

        // 選択フェーズ開始時の重さ合計を保存（じゅくすい・どかぐいの条件判定用）
        _selectionPhaseStartWeightTotal = 0;
        foreach (var kvp in _uyopyons)
        {
            _selectionPhaseStartWeightTotal += kvp.Value.Weight;
        }
        Debug.Log($"[UIController] 選択フェーズ開始時の重さ合計: {_selectionPhaseStartWeightTotal}");

        int currentEnergy = myState.Energy;
        var gameParams = GameManager.Instance.gameParams;

        // 午前か午後かを判定
        bool isMorning = !_isMorningSelected;
        Debug.Log($"[UIController] UpdateActionButtonsBasedOnEnergy: isMorning={isMorning}, _isMorningSelected={_isMorningSelected}");

        // 前日午後の行動を取得（連続使用ペナルティチェック用）
        ActionData? previousAction = null;
        PlayerActionData actionData = GameManager.Instance.GetPlayerActionData(localPlayer);
        if (actionData != null)
        {
            previousAction = actionData.LastAfternoonAction;
            Debug.Log($"[UIController] 前日午後の行動: {previousAction?.Type}");
        }

        // 午後の場合は午前の行動を取得（ローカルの_morningActionを優先）
        ActionData? morningAction = null;
        if (!isMorning)
        {
            // まずローカルで選択した午前の行動を使用
            if (_morningAction.HasValue)
            {
                morningAction = _morningAction;
                Debug.Log($"[UIController] 午後選択中: ローカルの午前行動を使用 - {morningAction.Value.Type}");
            }
            else
            {
                // フォールバック: ネットワーク同期された午前の行動を取得
                if (actionData != null)
                {
                    morningAction = actionData.MorningAction;
                    Debug.Log($"[UIController] 午後選択中: ネットワークの午前行動を使用 - {morningAction?.Type}");
                }
            }
        }
        else
        {
            Debug.Log($"[UIController] 午前選択中: 午前の行動は考慮しない");
        }

        // 各行動ボタンの有効/無効を設定（EnergyCheck.csを使用）
        // たべる
        if (eatButton != null)
        {
            eatButton.interactable = EnergyCheck.CanPerformAction(currentEnergy, ActionType.Eat, isMorning, morningAction, previousAction, gameParams);
        }

        // ねむる
        if (sleepButton != null)
        {
            sleepButton.interactable = EnergyCheck.CanPerformAction(currentEnergy, ActionType.Sleep, isMorning, morningAction, previousAction, gameParams);
        }

        // あそぶ
        if (playButton != null)
        {
            playButton.interactable = EnergyCheck.CanPerformAction(currentEnergy, ActionType.Play, isMorning, morningAction, previousAction, gameParams);
        }

        // つういん
        if (clinicButton != null)
        {
            clinicButton.interactable = EnergyCheck.CanPerformAction(currentEnergy, ActionType.Clinic, isMorning, morningAction, previousAction, gameParams);
        }

        // 9.3: 特殊能力ボタン（6つ個別に、進化後のみ、アクティブなボタンのみ）
        if (myState.HasEvolved)
        {
            string abilityName = myState.SpecialAbilityName.ToString();
            bool canUseAbility = EnergyCheck.CanUseSpecialAbility(currentEnergy, abilityName, isMorning, morningAction, previousAction, gameParams);

            if (gaishokuButton != null && gaishokuButton.gameObject.activeSelf)
                gaishokuButton.interactable = canUseAbility;
            if (kintreButton != null && kintreButton.gameObject.activeSelf)
                kintreButton.interactable = canUseAbility;
            if (gamusharaButton != null && gamusharaButton.gameObject.activeSelf)
                gamusharaButton.interactable = canUseAbility;
            if (benkyouButton != null && benkyouButton.gameObject.activeSelf)
                benkyouButton.interactable = canUseAbility;
            if (jukusuiButton != null && jukusuiButton.gameObject.activeSelf)
                jukusuiButton.interactable = canUseAbility;
            if (dokaguiButton != null && dokaguiButton.gameObject.activeSelf)
                dokaguiButton.interactable = canUseAbility;
        }

        // 確定・クリアボタンは常に有効
        if (fixButton != null) fixButton.interactable = true;
        if (clearButton != null) clearButton.interactable = true;

        // じゅくすい・どかぐいの条件チェック（選択フェーズ開始時の重さ合計を使用）
        UpdateSpecialAbilityButtonState();
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

        // 9.3: 特殊能力ボタンを無効化（6つ個別に）
        if (gaishokuButton != null) gaishokuButton.interactable = false;
        if (kintreButton != null) kintreButton.interactable = false;
        if (gamusharaButton != null) gamusharaButton.interactable = false;
        if (benkyouButton != null) benkyouButton.interactable = false;
        if (jukusuiButton != null) jukusuiButton.interactable = false;
        if (dokaguiButton != null) dokaguiButton.interactable = false;

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

        // 9.3: 特殊能力ボタンのアウトラインを非表示（6つ個別に）
        if (gaishokuButtonOutline != null) gaishokuButtonOutline.enabled = false;
        if (kintreButtonOutline != null) kintreButtonOutline.enabled = false;
        if (gamusharaButtonOutline != null) gamusharaButtonOutline.enabled = false;
        if (benkyouButtonOutline != null) benkyouButtonOutline.enabled = false;
        if (jukusuiButtonOutline != null) jukusuiButtonOutline.enabled = false;
        if (dokaguiButtonOutline != null) dokaguiButtonOutline.enabled = false;
    }

    /// <summary>
    /// 行動選択状態をリセット（新しい日の選択フェーズ開始時に呼ぶ）
    /// </summary>
    public void ResetActionSelection()
    {
        Debug.Log("[UIController] 行動選択状態をリセット");

        // 連続使用表示を全て非表示にする
        HideAllConsecutiveUseIndicators();

        // バグ4対応: 熱中症による午前ロックをチェック
        var runner = FindFirstObjectByType<NetworkRunner>();
        bool isMorningLocked = false;
        if (runner != null)
        {
            PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
            if (myActionData != null && myActionData.MorningActionLocked)
            {
                isMorningLocked = true;
                Debug.Log("[UIController] 熱中症により午前がロックされています");
            }
        }

        if (isMorningLocked)
        {
            // 熱中症時: 午前はつういん固定、午後から選択開始
            _morningAction = new ActionData(ActionType.Clinic, Genre.Rock);
            _afternoonAction = null;
            _isMorningSelected = true; // 午後選択中の状態にする

            // 午前に「つういん」を表示
            if (myTodayMorningActionText != null)
            {
                myTodayMorningActionText.text = "つういん";
                myTodayMorningActionText.fontSize = _defaultActionTextSize;
            }

            // 午後選択中のハイライト
            HighlightCurrentSelection(true);

            Debug.Log("[UIController] 熱中症対応: 午前=つういん固定、午後から選択開始");
        }
        else
        {
            // 通常時: すべてクリア
            _morningAction = null;
            _afternoonAction = null;
            _isMorningSelected = false;

            // 午前選択中のハイライトに戻す
            HighlightCurrentSelection(false);
        }

        // 行動選択状態をリセットした後、元気チェックを再実行
        UpdateActionButtonsBasedOnEnergy();

        Debug.Log("[UIController] 行動選択状態のリセット完了（元気チェックも再実行）");
    }

    /// <summary>
    /// TODO 4: 特殊能力ボタンの有効/無効を更新（じゅくすい・どかぐいの選択条件チェック）
    /// 選択フェーズ開始時に呼ぶ
    /// 9.3: 6つの個別ボタンに対応
    /// </summary>
    public void UpdateSpecialAbilityButtonState()
    {
        Debug.Log("[UIController] UpdateSpecialAbilityButtonState 開始");

        // ゲームフェーズをチェック（選択フェーズ以外ではボタンを無効化）
        if (GameFlowManager.Instance == null || GameFlowManager.Instance.CurrentPhase != GamePhase.Selection)
        {
            // 9.3: 6つのボタンすべてを無効化
            if (gaishokuButton != null && gaishokuButton.gameObject.activeSelf)
                gaishokuButton.interactable = false;
            if (kintreButton != null && kintreButton.gameObject.activeSelf)
                kintreButton.interactable = false;
            if (gamusharaButton != null && gamusharaButton.gameObject.activeSelf)
                gamusharaButton.interactable = false;
            if (benkyouButton != null && benkyouButton.gameObject.activeSelf)
                benkyouButton.interactable = false;
            if (jukusuiButton != null && jukusuiButton.gameObject.activeSelf)
                jukusuiButton.interactable = false;
            if (dokaguiButton != null && dokaguiButton.gameObject.activeSelf)
                dokaguiButton.interactable = false;

            Debug.Log($"[UIController] 選択フェーズ以外のため特殊能力ボタンを無効化。CurrentPhase={GameFlowManager.Instance?.CurrentPhase}");
            return;
        }

        // NetworkRunnerから自分のPlayerRefを取得
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogWarning("[UIController] NetworkRunnerが見つかりません");
            return;
        }

        PlayerRef localPlayer = runner.LocalPlayer;
        Debug.Log($"[UIController] localPlayer: {localPlayer}");

        // GameManagerから自分のUyopyonStateを取得
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UIController] GameManager.Instance が null です");
            return;
        }

        Debug.Log("[UIController] GameManager.Instance 取得成功");

        UyopyonState myState = GameManager.Instance.GetUyopyonState(localPlayer);
        Debug.Log($"[UIController] GetUyopyonState 完了: myState={(myState != null ? "存在" : "null")}");

        if (myState == null)
        {
            Debug.LogWarning("[UIController] 自分のUyopyonStateが見つかりません");
            return;
        }

        // 特殊能力名を取得（try-catchで安全に）
        string abilityName = "";
        try
        {
            abilityName = myState.SpecialAbilityName.ToString();
            Debug.Log($"[UIController] SpecialAbilityName.ToString() 成功: '{abilityName}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIController] SpecialAbilityName.ToString() で例外: {e.Message}");
            return;
        }

        Debug.Log($"[UIController] UpdateSpecialAbilityButtonState: abilityName={abilityName}");

        if (string.IsNullOrEmpty(abilityName))
        {
            // 特殊能力が設定されていない場合は何もしない
            Debug.Log("[UIController] 特殊能力名が空のため、処理をスキップ");
            return;
        }

        // SpecialAbilityTypeに変換
        if (!System.Enum.TryParse<SpecialAbilityType>(abilityName, out SpecialAbilityType abilityType))
        {
            Debug.LogWarning($"[UIController] 特殊能力名のパースに失敗: {abilityName}");
            return;
        }

        Debug.Log($"[UIController] 特殊能力タイプ: {abilityType}");

        // じゅくすい・どかぐい以外の場合は常に有効（該当するボタンのみ）
        if (abilityType != SpecialAbilityType.Jukusui && abilityType != SpecialAbilityType.Dokagui)
        {
            // 9.3: 該当する特殊能力ボタンを有効化
            EnableSpecialAbilityButton(abilityType, true);
            Debug.Log($"[UIController] {abilityType} は条件なしで有効化");
            return;
        }

        // 選択フェーズ開始時に保存された重さ合計を使用
        int totalWeight = _selectionPhaseStartWeightTotal;
        Debug.Log($"[UIController] 使用する重さ合計（選択フェーズ開始時）: {totalWeight}");

        // じゅくすい: 重さ合計が奇数のときのみ有効
        // どかぐい: 重さ合計が偶数のときのみ有効
        bool isOdd = (totalWeight % 2) == 1;
        bool shouldEnable = false;

        if (abilityType == SpecialAbilityType.Jukusui)
        {
            shouldEnable = isOdd;
            Debug.Log($"[UIController] じゅくすい: 重さ合計={totalWeight}, 奇数={isOdd}, 有効={shouldEnable}");
        }
        else if (abilityType == SpecialAbilityType.Dokagui)
        {
            shouldEnable = !isOdd;
            Debug.Log($"[UIController] どかぐい: 重さ合計={totalWeight}, 偶数={!isOdd}, 有効={shouldEnable}");
        }

        // 9.3: 該当する特殊能力ボタンを有効/無効化
        EnableSpecialAbilityButton(abilityType, shouldEnable);
        Debug.Log($"[UIController] {abilityType} ボタンのinteractableを {shouldEnable} に設定");
    }

    /// <summary>
    /// 9.3: 指定された特殊能力ボタンの有効/無効を設定
    /// </summary>
    private void EnableSpecialAbilityButton(SpecialAbilityType abilityType, bool enable)
    {
        switch (abilityType)
        {
            case SpecialAbilityType.Gaishoku:
                if (gaishokuButton != null && gaishokuButton.gameObject.activeSelf)
                    gaishokuButton.interactable = enable;
                break;
            case SpecialAbilityType.Kintre:
                if (kintreButton != null && kintreButton.gameObject.activeSelf)
                    kintreButton.interactable = enable;
                break;
            case SpecialAbilityType.Gamushara:
                if (gamusharaButton != null && gamusharaButton.gameObject.activeSelf)
                    gamusharaButton.interactable = enable;
                break;
            case SpecialAbilityType.Benkyou:
                if (benkyouButton != null && benkyouButton.gameObject.activeSelf)
                    benkyouButton.interactable = enable;
                break;
            case SpecialAbilityType.Jukusui:
                if (jukusuiButton != null && jukusuiButton.gameObject.activeSelf)
                    jukusuiButton.interactable = enable;
                break;
            case SpecialAbilityType.Dokagui:
                if (dokaguiButton != null && dokaguiButton.gameObject.activeSelf)
                    dokaguiButton.interactable = enable;
                break;
        }
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
    /// 9.3修正: 病気とケガを別々のパネルに分けて表示
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="ailments">状態異常配列 [0]=睡眠時無呼吸, [1]=糖尿病, [2]=腰痛, [3]=熱中症</param>
    public void UpdateStatusAilmentDisplay(PlayerRef player, byte[] ailments)
    {
        // 自分のプレイヤーか判定
        bool isMyPlayer = IsMyPlayer(player);

        // プレイヤーごとのパネルとテキストを取得
        GameObject sicknessPanel = isMyPlayer ? mySicknessPanel : oppSicknessPanel;
        GameObject injuryPanel = isMyPlayer ? myInjuryPanel : oppInjuryPanel;
        TextMeshProUGUI[] sicknessTexts = isMyPlayer ? mySicknessTexts : oppSicknessTexts;
        TextMeshProUGUI[] injuryTexts = isMyPlayer ? myInjuryTexts : oppInjuryTexts;

        // 病気パネルの更新（インデックス0=睡眠時無呼吸, 1=糖尿病）
        bool hasSickness = false;
        int sicknessTextIndex = 0;
        for (int i = 0; i < 2; i++) // 病気は0と1
        {
            if (i < ailments.Length && ailments[i] != 0)
            {
                hasSickness = true;
                StatusAilment ailment = IndexToStatusAilment(i);
                string ailmentText = GetStatusAilmentText(ailment);

                if (!string.IsNullOrEmpty(ailmentText) && sicknessTextIndex < sicknessTexts.Length && sicknessTexts[sicknessTextIndex] != null)
                {
                    sicknessTexts[sicknessTextIndex].text = ailmentText;
                    sicknessTextIndex++;
                }
            }
        }
        // 病気がない場合は未使用のテキストをクリア
        for (int i = sicknessTextIndex; i < sicknessTexts.Length; i++)
        {
            if (sicknessTexts[i] != null)
            {
                sicknessTexts[i].text = "";
            }
        }
        // 病気パネルの表示/非表示
        if (sicknessPanel != null)
        {
            sicknessPanel.SetActive(hasSickness);
        }

        // ケガパネルの更新（インデックス2=腰痛, 3=熱中症）
        bool hasInjury = false;
        int injuryTextIndex = 0;
        for (int i = 2; i < 4; i++) // ケガは2と3
        {
            if (i < ailments.Length && ailments[i] != 0)
            {
                hasInjury = true;
                StatusAilment ailment = IndexToStatusAilment(i);
                string ailmentText = GetStatusAilmentText(ailment);

                if (!string.IsNullOrEmpty(ailmentText) && injuryTextIndex < injuryTexts.Length && injuryTexts[injuryTextIndex] != null)
                {
                    injuryTexts[injuryTextIndex].text = ailmentText;
                    injuryTextIndex++;
                }
            }
        }
        // ケガがない場合は未使用のテキストをクリア
        for (int i = injuryTextIndex; i < injuryTexts.Length; i++)
        {
            if (injuryTexts[i] != null)
            {
                injuryTexts[i].text = "";
            }
        }
        // ケガパネルの表示/非表示
        if (injuryPanel != null)
        {
            injuryPanel.SetActive(hasInjury);
        }

        Debug.Log($"[UIController] Player {player} の状態異常を更新: 病気{sicknessTextIndex}件, ケガ{injuryTextIndex}件");
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

    // ===== 7.1: 特殊能力選択UI =====
    
    /// <summary>
    /// 特殊能力選択UIを表示する
    /// </summary>
    /// <param name="player">進化するプレイヤー</param>
    /// <param name="choices">選択肢となる特殊能力</param>
    public void ShowSpecialAbilityChoice(PlayerRef player, SpecialAbilityType[] choices, SpecialAbilityType[] disabledAbilities = null)
    {
        // ローカルプレイヤーでない場合は表示しない
        if (!IsMyPlayer(player))
        {
            Debug.Log($"[UIController] ShowSpecialAbilityChoice: プレイヤー {player.PlayerId} は自分ではないためUIを表示しません");
            return;
        }

        Debug.Log($"[UIController] ShowSpecialAbilityChoice: {choices.Length}個の選択肢を表示、無効化={disabledAbilities?.Length ?? 0}個");

        // パネルを表示
        if (specialAbilityChoicePanel != null)
        {
            specialAbilityChoicePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[UIController] specialAbilityChoicePanel が null です");
            return;
        }

        // 各ボタンに選択肢を設定
        for (int i = 0; i < specialAbilityButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                // ボタンを表示
                if (specialAbilityButtons[i] != null)
                {
                    specialAbilityButtons[i].gameObject.SetActive(true);
                    
                    // テキストを設定（無効化されていても表示）
                    if (specialAbilityButtonTexts[i] != null)
                    {
                        specialAbilityButtonTexts[i].text = GetSpecialAbilityDisplayName(choices[i]);
                    }

                    // 無効化チェック
                    bool isDisabled = disabledAbilities != null && System.Array.Exists(disabledAbilities, ability => ability == choices[i]);
                    
                    if (isDisabled)
                    {
                        // ボタンを無効化（押下不可）
                        specialAbilityButtons[i].interactable = false;
                        
                        // テキストの色を変更して無効化を示す（グレー）
                        //if (specialAbilityButtonTexts[i] != null)
                        //{
                        //    specialAbilityButtonTexts[i].color = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 0.5f);
                        //}
                        
                        Debug.Log($"[UIController] 選択肢 {choices[i]} は既に選ばれているため無効化");
                    }
                    else
                    {
                        // ボタンを有効化
                        specialAbilityButtons[i].interactable = true;
                        
                        // テキストの色を黒に設定
                        if (specialAbilityButtonTexts[i] != null)
                        {
                            specialAbilityButtonTexts[i].color = UnityEngine.Color.black;
                        }

                        // ボタンクリックイベントを設定
                        int index = i; // クロージャのためローカル変数にコピー
                        SpecialAbilityType ability = choices[i];
                        specialAbilityButtons[i].onClick.RemoveAllListeners();
                        specialAbilityButtons[i].onClick.AddListener(() => OnAbilityChoicePanelButtonClicked(ability));
                        
                        Debug.Log($"[UIController] 選択肢 {choices[i]} は有効（黒色テキスト）");
                    }
                }
            }
            else
            {
                // 選択肢が3つ未満の場合、余分なボタンを非表示
                if (specialAbilityButtons[i] != null)
                {
                    specialAbilityButtons[i].gameObject.SetActive(false);
                }
            }
        }

        // 選択完了フラグをリセット
        _isAbilitySelectionComplete = false;
        _selectedAbility = null;
    }

    /// <summary>
    /// 進化時の特殊能力選択パネルのボタンがクリックされた時の処理
    /// </summary>
    private void OnAbilityChoicePanelButtonClicked(SpecialAbilityType ability)
    {
        Debug.Log($"[UIController] 特殊能力選択パネルで {ability} が選択されました");

        _selectedAbility = ability;
        _isAbilitySelectionComplete = true;

        // パネルを非表示
        if (specialAbilityChoicePanel != null)
        {
            specialAbilityChoicePanel.SetActive(false);
        }

        // 7.2: GameFlowManagerに選択完了を通知
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.NotifyAbilitySelected(ability);
        }
        else
        {
            Debug.LogError("[UIController] GameFlowManager.Instance が null です");
        }
    }

    /// <summary>
    /// 特殊能力選択が完了したかどうかを確認
    /// </summary>
    public bool IsAbilitySelectionComplete()
    {
        return _isAbilitySelectionComplete;
    }

    /// <summary>
    /// 選択された特殊能力を取得
    /// </summary>
    public SpecialAbilityType? GetSelectedAbility()
    {
        return _selectedAbility;
    }

    /// <summary>
    /// 特殊能力選択状態をリセット（7.2: 次のプレイヤーの選択のため）
    /// </summary>
    public void ResetAbilitySelection()
    {
        _isAbilitySelectionComplete = false;
        _selectedAbility = null;
        Debug.Log("[UIController] 特殊能力選択状態をリセットしました");
    }

    /// <summary>
    /// 7.2: 待機中パネルを表示
    /// </summary>
    public void ShowWaitingPanel(string message = "対戦相手が特殊能力を選択中です。")
    {
        if (waitingForOpponentPanel != null)
        {
            waitingForOpponentPanel.SetActive(true);
            
            if (waitingMessageText != null)
            {
                waitingMessageText.text = message;
            }
            
            Debug.Log($"[UIController] 待機パネルを表示: {message}");
        }
        else
        {
            Debug.LogWarning("[UIController] waitingForOpponentPanel が null です");
        }
    }

    /// <summary>
    /// 7.2: 待機中パネルを非表示
    /// </summary>
    public void HideWaitingPanel()
    {
        if (waitingForOpponentPanel != null)
        {
            waitingForOpponentPanel.SetActive(false);
            Debug.Log("[UIController] 待機パネルを非表示");
        }
    }

    /// <summary>
    /// 特殊能力の表示名を取得
    /// </summary>
    private string GetSpecialAbilityDisplayName(SpecialAbilityType ability)
    {
        switch (ability)
        {
            case SpecialAbilityType.Gaishoku:
                return "がいしょく";
            case SpecialAbilityType.Kintre:
                return "きんとれ";
            case SpecialAbilityType.Gamushara:
                return "がむしゃら";
            case SpecialAbilityType.Benkyou:
                return "べんきょう";
            case SpecialAbilityType.Jukusui:
                return "じゅくすい";
            case SpecialAbilityType.Dokagui:
                return "どかぐい";
            default:
                return ability.ToString();
        }
    }

    /// <summary>
    /// TODO 5: 自分の特殊能力の種類を取得
    /// </summary>
    private SpecialAbilityType? GetMySpecialAbilityType()
    {
        Debug.Log("[UIController] GetMySpecialAbilityType() 開始");

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogWarning("[UIController] NetworkRunner が null です");
            return null;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UIController] GameManager.Instance が null です");
            return null;
        }

        PlayerRef localPlayer = runner.LocalPlayer;
        Debug.Log($"[UIController] LocalPlayer: {localPlayer}");

        UyopyonState myState = GameManager.Instance.GetUyopyonState(localPlayer);
        Debug.Log($"[UIController] UyopyonState: {(myState != null ? "存在" : "null")}");

        if (myState == null)
        {
            Debug.LogWarning("[UIController] UyopyonState が null です");
            return null;
        }

        string abilityName = $"{myState.SpecialAbilityName}";
        Debug.Log($"[UIController] SpecialAbilityName: '{abilityName}'");

        if (string.IsNullOrEmpty(abilityName))
        {
            Debug.LogWarning("[UIController] SpecialAbilityName が空です");
            return null;
        }

        if (System.Enum.TryParse<SpecialAbilityType>(abilityName, out SpecialAbilityType abilityType))
        {
            Debug.Log($"[UIController] パース成功: {abilityType}");
            return abilityType;
        }

        Debug.LogWarning($"[UIController] SpecialAbilityType のパースに失敗: '{abilityName}'");
        return null;
    }

    /// <summary>
    /// TODO 5: 自分の特殊能力の表示名を取得
    /// </summary>
    private string GetMySpecialAbilityDisplayName()
    {
        SpecialAbilityType? abilityType = GetMySpecialAbilityType();

        if (abilityType.HasValue)
        {
            return GetSpecialAbilityDisplayName(abilityType.Value);
        }

        return "特殊能力";
    }

    /// <summary>
    /// 自分の特殊能力の名前（enum文字列）を取得
    /// </summary>
    private string GetMySpecialAbilityName()
    {
        SpecialAbilityType? abilityType = GetMySpecialAbilityType();
        return abilityType.HasValue ? abilityType.Value.ToString() : "";
    }

    /// <summary>
    /// ActionTypeからActionDataを生成するヘルパーメソッド
    /// </summary>
    private ActionData CreateActionDataFromType(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Eat => ActionData.CreateEat(),
            ActionType.Sleep => ActionData.CreateSleep(),
            ActionType.Play => ActionData.CreatePlay(),
            ActionType.Clinic => ActionData.CreateClinic(),
            ActionType.None => ActionData.Empty(),
            _ => ActionData.Default()
        };
    }

    /// <summary>
    /// 指定されたプレイヤーの特殊能力の表示名を取得
    /// </summary>
    private string GetSpecialAbilityDisplayNameForPlayer(PlayerRef player)
    {
        Debug.Log($"[UIController] GetSpecialAbilityDisplayNameForPlayer: player={player}");
        
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UIController] GameManager.Instance is null");
            return "特殊能力";
        }

        UyopyonState state = GameManager.Instance.GetUyopyonState(player);
        if (state == null)
        {
            Debug.LogWarning($"[UIController] UyopyonState is null for player {player}");
            return "特殊能力";
        }

        string abilityName = $"{state.SpecialAbilityName}";
        Debug.Log($"[UIController] Player {player} の SpecialAbilityName: '{abilityName}'");

        if (string.IsNullOrEmpty(abilityName))
        {
            Debug.LogWarning($"[UIController] SpecialAbilityName is empty for player {player}");
            return "特殊能力";
        }

        if (System.Enum.TryParse<SpecialAbilityType>(abilityName, out SpecialAbilityType abilityType))
        {
            string displayName = GetSpecialAbilityDisplayName(abilityType);
            Debug.Log($"[UIController] Player {player} の特殊能力表示名: '{displayName}'");
            return displayName;
        }

        Debug.LogWarning($"[UIController] Failed to parse SpecialAbilityType: '{abilityName}'");
        return "特殊能力";
    }

    /// <summary>
    /// 5文字の特殊能力名（長い名前）かどうかをチェック
    /// </summary>
    private bool IsLongSpecialAbilityName(string text)
    {
        return text == "がいしょく" || text == "がむしゃら" || text == "べんきょう" || text == "じゅくすい";
    }

    /// <summary>
    /// 特殊能力名の長さに応じてフォントサイズを設定
    /// </summary>
    private void SetFontSizeForSpecialAbility(TextMeshProUGUI textComponent, string text, float normalSize, float longNameSize)
    {
        if (textComponent == null) return;

        if (IsLongSpecialAbilityName(text))
        {
            textComponent.fontSize = longNameSize;
        }
        else
        {
            textComponent.fontSize = normalSize;
        }
    }

    /// <summary>
    /// 指定されたPlayerRefがホスト（SharedModeMasterClient）かどうかを判定
    /// </summary>
    /// <param name="player">判定するPlayerRef</param>
    /// <returns>ホストの場合true、それ以外false</returns>
    public bool IsHostPlayer(PlayerRef player)
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning)
        {
            return false;
        }

        // Shared Modeでは最初のプレイヤー（PlayerId が最小のプレイヤー）がホスト
        // ActivePlayersから最小のPlayerIdを持つプレイヤーを取得
        var activePlayers = runner.ActivePlayers.ToList();
        if (activePlayers.Count == 0)
        {
            return false;
        }

        var hostPlayer = activePlayers.OrderBy(p => p.PlayerId).First();
        return player == hostPlayer;
    }

    /// <summary>
    /// TextMeshProUGUIコンポーネントの文字色を設定
    /// </summary>
    /// <param name="textComponent">対象のTextMeshProUGUIコンポーネント</param>
    /// <param name="isHost">ホストプレイヤーかどうか</param>
    private void SetPlayerNameTextColor(TextMeshProUGUI textComponent, bool isHost)
    {
        if (textComponent != null)
        {
            textComponent.color = isHost ? hostPlayerNameColor : clientPlayerNameColor;
        }
    }

    /// <summary>
    /// プレイヤー名を色付きでフォーマットする（ログ表示用）
    /// TextMeshProのリッチテキストタグを使用
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="player">PlayerRef</param>
    /// <returns>色付きプレイヤー名（リッチテキスト形式）</returns>
    public string FormatPlayerNameForLog(string playerName, PlayerRef player)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            return playerName;
        }

        bool isHost = IsHostPlayer(player);
        Color nameColor = isHost ? hostPlayerNameColor : clientPlayerNameColor;
        string hexColor = ColorUtility.ToHtmlStringRGB(nameColor);

        return $"<color=#{hexColor}>{playerName}</color>";
    }

    // === 8.2: ゲーム終了アニメーション ===

    /// <summary>
    /// 勝利アニメーションを再生（スタブ）
    /// </summary>
    public void PlayVictoryAnimation(PlayerRef player)
    {
        Debug.Log($"[UIController] PlayVictoryAnimation: Player {player} の勝利アニメーションを再生");

        // TODO: 実際のアニメーションを再生する処理を実装
        // 例: Animatorコンポーネントを使用してアニメーションクリップを再生

        // プレイヤーのUyopyonStateを取得
        if (GameManager.Instance != null && GameManager.Instance.uyopyonStateDict.TryGetValue(player, out UyopyonState state))
        {
            // TODO: state.GetComponent<Animator>()?.SetTrigger("Victory");
            Debug.Log($"[UIController] Player {player} のうーぴょんで勝利アニメーションをトリガー");
        }
        else
        {
            Debug.LogWarning($"[UIController] Player {player} のUyopyonStateが見つかりません");
        }

        // 勝利SE再生
        AudioManager.Instance?.PlayVictorySE();
    }

    /// <summary>
    /// 敗北アニメーションを再生（スタブ）
    /// </summary>
    public void PlayDefeatAnimation(PlayerRef player)
    {
        Debug.Log($"[UIController] PlayDefeatAnimation: Player {player} の敗北アニメーションを再生");

        // TODO: 実際のアニメーションを再生する処理を実装
        // 例: Animatorコンポーネントを使用してアニメーションクリップを再生

        // プレイヤーのUyopyonStateを取得
        if (GameManager.Instance != null && GameManager.Instance.uyopyonStateDict.TryGetValue(player, out UyopyonState state))
        {
            // TODO: state.GetComponent<Animator>()?.SetTrigger("Defeat");
            Debug.Log($"[UIController] Player {player} のうーぴょんで敗北アニメーションをトリガー");
        }
        else
        {
            Debug.LogWarning($"[UIController] Player {player} のUyopyonStateが見つかりません");
        }

        // 敗北SE再生
        AudioManager.Instance?.PlayDefeatSE();
    }

    // === 8.3: リザルト画面 ===

    /// <summary>
    /// リザルト画面を表示
    /// </summary>
    public void ShowResultPanel(PlayerRef winner, int day, bool isMorning)
    {
        Debug.Log($"[UIController] ShowResultPanel: winner={winner}, day={day}, isMorning={isMorning}");

        if (resultPanel == null)
        {
            Debug.LogError("[UIController] resultPanel が null です");
            return;
        }

        // 他のパネルを全て非表示にする
        if (waitingForOpponentPanel != null && waitingForOpponentPanel.activeSelf)
        {
            waitingForOpponentPanel.SetActive(false);
        }

        if (specialAbilityChoicePanel != null && specialAbilityChoicePanel.activeSelf)
        {
            specialAbilityChoicePanel.SetActive(false);
        }

        if (blackoutPanel != null && blackoutPanel.activeSelf)
        {
            blackoutPanel.SetActive(false);
        }

        // ResultPanel内のボタン以外のすべてのGraphicのraycastをオフにする
        var allGraphics = resultPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var graphic in allGraphics)
        {
            if (graphic.gameObject != resultReturnToTitleButton.gameObject)
            {
                graphic.raycastTarget = false;
            }
        }

        // LogAreaのすべてのGraphicもオフにする
        if (logScrollRect != null)
        {
            var logGraphics = logScrollRect.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in logGraphics)
            {
                graphic.raycastTarget = false;
            }
        }

        // Canvas全体のResultPanel以外のすべてのGraphicをオフにする
        var mainCanvas = resultPanel.GetComponentInParent<Canvas>();
        if (mainCanvas != null)
        {
            var allCanvasGraphics = mainCanvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in allCanvasGraphics)
            {
                if (!graphic.transform.IsChildOf(resultPanel.transform))
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        // CanvasGroupがraycastをブロックしないようにする
        var canvasGroups = resultPanel.GetComponentsInParent<CanvasGroup>(true);
        foreach (var cg in canvasGroups)
        {
            if (!cg.blocksRaycasts)
            {
                cg.blocksRaycasts = true;
            }
        }

        // ボタンをヒエラルキーの最前面に移動
        if (resultReturnToTitleButton != null)
        {
            resultReturnToTitleButton.transform.SetAsLastSibling();
        }

        // ボタンのすべてのGraphicのraycastTargetを有効にする
        if (resultReturnToTitleButton != null)
        {
            var buttonGraphics = resultReturnToTitleButton.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in buttonGraphics)
            {
                graphic.raycastTarget = true;
            }
        }

        // ResultPanelのImageがボタンをブロックしないようにする
        var resultPanelImage = resultPanel.GetComponent<UnityEngine.UI.Image>();
        if (resultPanelImage != null)
        {
            resultPanelImage.raycastTarget = false;
        }

        // NetworkRunnerから自分のPlayerRefを取得
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[UIController] NetworkRunnerが見つかりません");
            return;
        }

        PlayerRef localPlayer = runner.LocalPlayer;

        // 勝者・敗者の情報を取得
        var allPlayers = GameManager.Instance.uyopyonStateDict.Keys.ToList();
        if (allPlayers.Count != 2)
        {
            Debug.LogError($"[UIController] プレイヤーが2人ではありません: {allPlayers.Count}人");
            return;
        }

        PlayerRef loser = allPlayers.FirstOrDefault(p => p != winner);

        // 勝者名を表示
        string winnerName = GetPlayerName(winner);
        bool winnerIsHost = IsHostPlayer(winner);
        if (resultWinnerText != null)
        {
            resultWinnerText.text = $"{winnerName} の勝ち！";
            SetPlayerNameTextColor(resultWinnerText, winnerIsHost);
        }

        // 終了日数・午前/午後を表示
        if (resultDayText != null)
        {
            string timeOfDay = isMorning ? "午前" : "午後";
            resultDayText.text = $"{day}日目 {timeOfDay}";
        }

        // 自分と相手のステータスを取得
        PlayerRef myPlayer = localPlayer;
        PlayerRef oppPlayer = (myPlayer == allPlayers[0]) ? allPlayers[1] : allPlayers[0];

        UyopyonState myState = GameManager.Instance.GetUyopyonState(myPlayer);
        UyopyonState oppState = GameManager.Instance.GetUyopyonState(oppPlayer);

        if (myState == null || oppState == null)
        {
            Debug.LogError("[UIController] UyopyonStateが取得できません");
            return;
        }

        // 自分のステータスを表示
        if (resultMyNameText != null)
        {
            bool myIsHost = IsHostPlayer(myPlayer);
            resultMyNameText.text = GetPlayerName(myPlayer);
            SetPlayerNameTextColor(resultMyNameText, myIsHost);
        }
        if (resultMyWeightText != null)
        {
            resultMyWeightText.text = $"重さ: {myState.Weight}kg";
        }
        if (resultMyEnergyText != null)
        {
            resultMyEnergyText.text = $"元気: {myState.Energy}";
        }
        if (resultMyAbilityText != null)
        {
            string abilityName = myState.HasEvolved ? GetSpecialAbilityDisplayNameForPlayer(myPlayer) : "未進化";
            resultMyAbilityText.text = $"特殊能力: {abilityName}";
        }
        if (resultMyJankenWinText != null)
        {
            resultMyJankenWinText.text = $"じゃんけん勝利数: {myState.JankenWinCount}回";
        }

        // 相手のステータスを表示
        if (resultOppNameText != null)
        {
            bool oppIsHost = IsHostPlayer(oppPlayer);
            resultOppNameText.text = GetPlayerName(oppPlayer);
            SetPlayerNameTextColor(resultOppNameText, oppIsHost);
        }
        if (resultOppWeightText != null)
        {
            resultOppWeightText.text = $"重さ: {oppState.Weight}kg";
        }
        if (resultOppEnergyText != null)
        {
            resultOppEnergyText.text = $"元気: {oppState.Energy}";
        }
        if (resultOppAbilityText != null)
        {
            string abilityName = oppState.HasEvolved ? GetSpecialAbilityDisplayNameForPlayer(oppPlayer) : "未進化";
            resultOppAbilityText.text = $"特殊能力: {abilityName}";
        }
        if (resultOppJankenWinText != null)
        {
            resultOppJankenWinText.text = $"じゃんけん勝利数: {oppState.JankenWinCount}回";
        }

        // リザルトパネルを表示
        resultPanel.SetActive(true);
    }

    /// <summary>
    /// タイトルに戻るボタンがクリックされた時の処理
    /// Unity EditorのInspectorでButtonのOnClick()に設定してください
    /// </summary>
    public void OnReturnToTitleButtonClicked()
    {
        // ボタンの二重クリック防止
        if (resultReturnToTitleButton != null)
        {
            resultReturnToTitleButton.interactable = false;
        }

        Debug.Log("[UIController] OnReturnToTitleButtonClicked: ボタンがクリックされました");

        // ボタンを押したプレイヤーだけがタイトルに戻る（RPCを使わない）
        StartReturnToTitleProcess();
    }

    /// <summary>
    /// タイトルに戻る処理を開始（ローカルプレイヤーのみ）
    /// </summary>
    public void StartReturnToTitleProcess()
    {
        Debug.Log("[UIController] StartReturnToTitleProcess: タイトル遷移処理を開始します");
        StartCoroutine(ReturnToTitleCoroutine());
    }

    /// <summary>
    /// タイトルシーンに戻るコルーチン
    /// GameFlowManagerのゲーム終了フラグを設定し、NetworkRunnerをシャットダウンしてから即座にシーン遷移を行う
    /// </summary>
    private IEnumerator ReturnToTitleCoroutine()
    {
        Debug.Log("[UIController] ReturnToTitleCoroutine: タイトルへの遷移を開始します");

        // GameFlowManagerのゲーム終了フラグを設定（非同期処理を止める）
        var gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        if (gameFlowManager != null && gameFlowManager.Object != null && gameFlowManager.Object.IsValid)
        {
            // ホストの場合のみフラグを設定可能
            if (gameFlowManager.Object.HasStateAuthority)
            {
                gameFlowManager.IsGameEnded = true;
                Debug.Log("[UIController] ReturnToTitleCoroutine: IsGameEndedをtrueに設定しました");
            }
        }

        // NetworkRunnerをシャットダウン（完了を待たない）
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            Debug.Log("[UIController] ReturnToTitleCoroutine: NetworkRunnerのシャットダウンを開始します");
            try
            {
                runner.Shutdown();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIController] ReturnToTitleCoroutine: Shutdown中に例外発生: {e.Message}");
            }
        }

        // 少し待機（シャットダウン開始を確実にする）
        yield return new WaitForSeconds(0.1f);

        // TitleSceneに遷移
        Debug.Log("[UIController] ReturnToTitleCoroutine: TitleSceneに遷移します");
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }

    /// <summary>
    /// プレイヤー名を取得するヘルパーメソッド
    /// </summary>
    private string GetPlayerName(PlayerRef player)
    {
        // GameManagerのDictionaryから直接取得（FindObjectsByType削減）
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetPlayerName(player);
        }
        return $"Player {player}";
    }

    /// <summary>
    /// GameObjectの階層パスを取得するヘルパーメソッド
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    // === 9.2: 設定パネル ===
    // 設定パネルはSettingController（シングルトン）で管理されるため、
    // UIControllerでは設定ボタンの制御は不要

    // === 9.3: 行動選択時の効果予測表示メソッド ===

    /// <summary>
    /// 行動ボタンにホバーした時にツールチップを表示
    /// </summary>
    /// <summary>
    /// 行動ボタンにホバーした時にツールチップ（詳細情報）を表示
    /// </summary>
    public void ShowActionTooltip(ActionType action)
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || runner.LocalPlayer == PlayerRef.None) return;

        UyopyonState myState = GameManager.Instance?.GetUyopyonState(runner.LocalPlayer);
        PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
        GameParameters gameParams = GameFlowManager.Instance?.gameParams;

        if (myState == null || myActionData == null || gameParams == null) return;

        // 詳細テキストを生成（ActionDetailDisplayを使用）
        string detailText = ActionDetailDisplay.GenerateDetailText(action, null, myState, myActionData, gameParams);

        // 対応するツールチップを表示
        switch (action)
        {
            case ActionType.Eat:
                if (eatTooltipPanel != null && eatTooltipText != null)
                {
                    eatTooltipText.text = detailText;
                    eatTooltipPanel.SetActive(true);
                }
                break;
            case ActionType.Sleep:
                if (sleepTooltipPanel != null && sleepTooltipText != null)
                {
                    sleepTooltipText.text = detailText;
                    sleepTooltipPanel.SetActive(true);
                }
                break;
            case ActionType.Play:
                if (playTooltipPanel != null && playTooltipText != null)
                {
                    playTooltipText.text = detailText;
                    playTooltipPanel.SetActive(true);
                }
                break;
            case ActionType.Clinic:
                if (clinicTooltipPanel != null && clinicTooltipText != null)
                {
                    clinicTooltipText.text = detailText;
                    clinicTooltipPanel.SetActive(true);
                }
                break;
        }
    }

    /// <summary>
    /// 特殊能力ボタンにホバーした時にツールチップ（詳細情報）を表示
    /// </summary>
    public void ShowSpecialAbilityTooltip()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || runner.LocalPlayer == PlayerRef.None) return;

        UyopyonState myState = GameManager.Instance?.GetUyopyonState(runner.LocalPlayer);
        PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
        GameParameters gameParams = GameFlowManager.Instance?.gameParams;

        if (myState == null || myActionData == null || gameParams == null) return;
        if (!myState.HasEvolved || string.IsNullOrEmpty(myState.SpecialAbilityName.ToString())) return;

        string abilityName = myState.SpecialAbilityName.ToString();
        string detailText = ActionDetailDisplay.GenerateDetailText(ActionType.SpecialAbility, abilityName, myState, myActionData, gameParams);

        if (specialAbilityTooltipPanel != null && specialAbilityTooltipText != null)
        {
            specialAbilityTooltipText.text = detailText;
            specialAbilityTooltipPanel.SetActive(true);
        }
    }

    /// <summary>
    /// すべての行動ボタンの常時表示テキストを更新
    /// 選択フェーズ開始時やステータス変更時に呼び出す
    /// </summary>
    public void UpdateAllActionEffectDisplay()
    {
        Debug.Log("[UIController] UpdateAllActionEffectDisplay() 呼び出し");

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || runner.LocalPlayer == PlayerRef.None)
        {
            Debug.LogWarning("[UIController] NetworkRunner or LocalPlayer not found");
            return;
        }

        UyopyonState myState = GameManager.Instance?.GetUyopyonState(runner.LocalPlayer);
        PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
        GameParameters gameParams = GameFlowManager.Instance?.gameParams;

        if (myState == null || myActionData == null || gameParams == null)
        {
            Debug.LogWarning($"[UIController] データ取得失敗: State={myState != null}, ActionData={myActionData != null}, Params={gameParams != null}");
            return;
        }

        Debug.Log($"[UIController] データ取得成功: Weight={myState.Weight}, BuffWeight={myState.PlayBuffWeight}, BuffEnergy={myState.PlayBuffEnergy}");

        // 9.3修正: 午前か午後かを判定（ローカルの選択状態を使用）
        bool isMorning = !_isMorningSelected;
        Debug.Log($"[UIController] UpdateAllActionEffectDisplay: isMorning={isMorning}, _isMorningSelected={_isMorningSelected}");

        // たべる：元気増減量、重さ増減量、病気にかかる確率
        UpdateActionEffectDisplay(ActionType.Eat,
            eatEnergyValueText, eatWeightValueText,
            myState, myActionData, gameParams, isMorning, _morningAction,
            sicknessProbabilityText: eatSicknessProbabilityText);

        // ねむる：元気増減量のみ
        UpdateActionEffectDisplay(ActionType.Sleep,
            sleepEnergyValueText, null,
            myState, myActionData, gameParams, isMorning, _morningAction);

        // あそぶ：元気増減量、元気バフ量、重さバフ量、ケガにかかる確率
        UpdateActionEffectDisplay(ActionType.Play,
            playEnergyValueText, null,
            myState, myActionData, gameParams, isMorning, _morningAction,
            energyBuffText: playEnergyBuffText,
            weightBuffText: playWeightBuffText,
            injuryProbabilityText: playInjuryProbabilityText);

        // つういん：元気増減量のみ
        UpdateActionEffectDisplay(ActionType.Clinic,
            clinicEnergyValueText, null,
            myState, myActionData, gameParams, isMorning, _morningAction);

        // 特殊能力の表示を更新（進化後のみ）
        if (myState.HasEvolved && !string.IsNullOrEmpty(myState.SpecialAbilityName.ToString()))
        {
            UpdateSpecialAbilityButtons(myState.SpecialAbilityName.ToString(),
                myState, myActionData, gameParams, isMorning, _morningAction);
                }

        // 連続使用デバフ表示を更新
        UpdateConsecutiveUseIndicators();
    }

    /// <summary>
    /// 個別の行動ボタンの常時表示を更新（数値テキストのみ）
    /// 9.3修正: ローカルの午前行動を渡せるようにlocalMorningActionパラメータを追加
    /// </summary>
    private void UpdateActionEffectDisplay(
        ActionType action,
        TextMeshProUGUI energyValueText,
        TextMeshProUGUI weightValueText,
        UyopyonState state,
        PlayerActionData actionData,
        GameParameters gameParams,
        bool isMorning,
        ActionData? localMorningAction = null,
        TextMeshProUGUI sicknessProbabilityText = null,
        TextMeshProUGUI injuryProbabilityText = null,
        TextMeshProUGUI energyBuffText = null,
        TextMeshProUGUI weightBuffText = null)
    {
        int energyChange = ActionCalculator.CalculateEnergyChange(action, null, state, actionData, gameParams, isMorning, localMorningAction);
        int weightChange = ActionCalculator.CalculateWeightChange(action, null, state, gameParams);

        // 元気値テキストの更新
        if (energyValueText != null)
        {
            if (energyChange != 0)
            {
                energyValueText.gameObject.SetActive(true);
                SetTextIfChanged(energyValueText, FormatChange(energyChange));
            }
            else
            {
                // 元気変化が0の場合は非表示
                energyValueText.gameObject.SetActive(false);
            }
        }

        // 重さ値テキストの更新
        if (weightValueText != null)
        {
            if (weightChange != 0)
            {
                weightValueText.gameObject.SetActive(true);
                SetTextIfChanged(weightValueText, FormatChange(weightChange));
            }
            else
            {
                // 重さ変化が0の場合は非表示
                weightValueText.gameObject.SetActive(false);
            }
        }

        // 病気発症確率の更新（たべる用）
        if (sicknessProbabilityText != null)
        {
            var (sicknessProbability, _) = ActionCalculator.CalculateAilmentProbability(state, gameParams);
            sicknessProbabilityText.gameObject.SetActive(true);
            SetTextIfChanged(sicknessProbabilityText, ActionCalculator.FormatProbability(sicknessProbability));
        }

        // ケガ発症確率の更新（あそぶ用）
        if (injuryProbabilityText != null)
        {
            var (_, injuryProbability) = ActionCalculator.CalculateAilmentProbability(state, gameParams);
            injuryProbabilityText.gameObject.SetActive(true);
            SetTextIfChanged(injuryProbabilityText, ActionCalculator.FormatProbability(injuryProbability));
        }

        // バフ量の更新（あそぶ用）
        if (energyBuffText != null || weightBuffText != null)
        {
            var (energyBuff, weightBuff) = ActionCalculator.GetBuffIncrement(gameParams);

            if (energyBuffText != null)
            {
                energyBuffText.gameObject.SetActive(true);
                SetTextIfChanged(energyBuffText, FormatChange(energyBuff));
            }

            if (weightBuffText != null)
            {
                weightBuffText.gameObject.SetActive(true);
                SetTextIfChanged(weightBuffText, FormatChange(weightBuff));
            }
        }
    }

    /// <summary>
    /// 特殊能力ボタンの常時表示テキストを更新
    /// </summary>
    /// <summary>
    /// 特殊能力ボタンの常時表示を更新（アイコン+数値分離表示）
    /// </summary>
    /// <summary>
    /// 特殊能力ボタンの常時表示を更新（数値テキストのみ）
    /// </summary>
    /// <summary>
    /// 特殊能力ボタンの表示を更新（6つのボタンのうち1つをSetActiveにする）
    /// 9.3修正: ローカルの午前行動を渡せるようにlocalMorningActionパラメータを追加
    /// </summary>
    private void UpdateSpecialAbilityButtons(
        string abilityName,
        UyopyonState state,
        PlayerActionData actionData,
        GameParameters gameParams,
        bool isMorning,
        ActionData? localMorningAction = null)
    {
        // すべてのボタンを非表示
        if (gaishokuButton != null) gaishokuButton.gameObject.SetActive(false);
        if (kintreButton != null) kintreButton.gameObject.SetActive(false);
        if (gamusharaButton != null) gamusharaButton.gameObject.SetActive(false);
        if (benkyouButton != null) benkyouButton.gameObject.SetActive(false);
        if (jukusuiButton != null) jukusuiButton.gameObject.SetActive(false);
        if (dokaguiButton != null) dokaguiButton.gameObject.SetActive(false);

        // 選択された特殊能力に応じてボタンを表示・更新
        switch (abilityName)
        {
            case "Gaishoku":
                UpdateGaishokuButton(state, actionData, gameParams, isMorning, localMorningAction);
                break;
            case "Kintre":
                UpdateKintreButton(state, actionData, gameParams, isMorning, localMorningAction);
                break;
            case "Gamushara":
                UpdateGamusharaButton(state, actionData, gameParams, isMorning, localMorningAction);
                break;
            case "Benkyou":
                UpdateBenkyouButton(state, actionData, gameParams, isMorning, localMorningAction);
                break;
            case "Jukusui":
                UpdateJukusuiButton(state, actionData, gameParams, isMorning, localMorningAction);
                break;
            case "Dokagui":
                UpdateDokaguiButton(state, actionData, gameParams, isMorning, localMorningAction);
                break;
        }
    }

    /// <summary>
    /// がいしょくボタンの表示を更新
    /// </summary>
    private void UpdateGaishokuButton(UyopyonState state, PlayerActionData actionData, GameParameters gameParams, bool isMorning, ActionData? localMorningAction = null)
    {
        if (gaishokuButton == null) return;
        gaishokuButton.gameObject.SetActive(true);

        // 元気増減量
        int energyChange = ActionCalculator.CalculateEnergyChange(ActionType.SpecialAbility, "Gaishoku", state, actionData, gameParams, isMorning, localMorningAction);
        if (gaishokuEnergyValueText != null)
            gaishokuEnergyValueText.text = FormatChange(energyChange);

        // 重さ増減量
        int weightChange = ActionCalculator.CalculateWeightChange(ActionType.SpecialAbility, "Gaishoku", state, gameParams);
        if (gaishokuWeightValueText != null)
            gaishokuWeightValueText.text = FormatChange(weightChange);

        // 病気確率
        float sicknessProbability = ActionCalculator.CalculateSpecialAbilitySicknessProbability(state, gameParams);
        if (gaishokuSicknessProbabilityText != null)
            gaishokuSicknessProbabilityText.text = ActionCalculator.FormatProbability(sicknessProbability);
    }

    /// <summary>
    /// きんとれボタンの表示を更新
    /// </summary>
    private void UpdateKintreButton(UyopyonState state, PlayerActionData actionData, GameParameters gameParams, bool isMorning, ActionData? localMorningAction = null)
    {
        if (kintreButton == null) return;
        kintreButton.gameObject.SetActive(true);

        // 元気増減量
        int energyChange = ActionCalculator.CalculateEnergyChange(ActionType.SpecialAbility, "Kintre", state, actionData, gameParams, isMorning, localMorningAction);
        if (kintreEnergyValueText != null)
            kintreEnergyValueText.text = FormatChange(energyChange);

        // 重さ増減量
        int weightChange = ActionCalculator.CalculateKintreWeightChange(state.Weight);
        if (kintreWeightValueText != null)
            kintreWeightValueText.text = FormatChange(weightChange);

        // 元気バフ・重さバフの増減量
        var (energyBuffChange, weightBuffChange) = ActionCalculator.CalculateKintreBuffChange(state, gameParams);
        if (kintreEnergyBuffText != null)
            kintreEnergyBuffText.text = FormatChange(energyBuffChange);
        if (kintreWeightBuffText != null)
            kintreWeightBuffText.text = FormatChange(weightBuffChange);

        // ケガ確率
        var (_, injuryProbability) = ActionCalculator.CalculateAilmentProbability(state, gameParams);
        if (kintreInjuryProbabilityText != null)
            kintreInjuryProbabilityText.text = ActionCalculator.FormatProbability(injuryProbability);
    }

    /// <summary>
    /// がむしゃらボタンの表示を更新
    /// </summary>
    private void UpdateGamusharaButton(UyopyonState state, PlayerActionData actionData, GameParameters gameParams, bool isMorning, ActionData? localMorningAction = null)
    {
        if (gamusharaButton == null) return;
        gamusharaButton.gameObject.SetActive(true);

    }

    /// <summary>
    /// べんきょうボタンの表示を更新
    /// </summary>
    private void UpdateBenkyouButton(UyopyonState state, PlayerActionData actionData, GameParameters gameParams, bool isMorning, ActionData? localMorningAction = null)
    {
        if (benkyouButton == null) return;
        benkyouButton.gameObject.SetActive(true);

        // 元気増減量
        int energyChange = ActionCalculator.CalculateEnergyChange(ActionType.SpecialAbility, "Benkyou", state, actionData, gameParams, isMorning, localMorningAction);
        if (benkyouEnergyValueText != null)
            benkyouEnergyValueText.text = FormatChange(energyChange);

        // 重さバフ増加量（次回実行時）
        int nextStudyCombo = state.StudyCombo + 1;
        int buffIncrement = ActionCalculator.CalculateBenkyouBuffIncrement(nextStudyCombo);
        if (benkyouWeightBuffText != null)
            benkyouWeightBuffText.text = FormatChange(buffIncrement);

        // 連続使用数表示
        if (benkyouStudyComboText != null)
            benkyouStudyComboText.text = $"{state.StudyCombo}";
    }

    /// <summary>
    /// じゅくすいボタンの表示を更新
    /// </summary>
    private void UpdateJukusuiButton(UyopyonState state, PlayerActionData actionData, GameParameters gameParams, bool isMorning, ActionData? localMorningAction = null)
    {
        if (jukusuiButton == null) return;
        jukusuiButton.gameObject.SetActive(true);

        // 元気増減量のみ
        int energyChange = ActionCalculator.CalculateEnergyChange(ActionType.SpecialAbility, "Jukusui", state, actionData, gameParams, isMorning, localMorningAction);
        if (jukusuiEnergyValueText != null)
            jukusuiEnergyValueText.text = FormatChange(energyChange);
    }

    /// <summary>
    /// どかぐいボタンの表示を更新
    /// </summary>
    private void UpdateDokaguiButton(UyopyonState state, PlayerActionData actionData, GameParameters gameParams, bool isMorning, ActionData? localMorningAction = null)
    {
        if (dokaguiButton == null) return;
        dokaguiButton.gameObject.SetActive(true);

        // 元気増減量
        int energyChange = ActionCalculator.CalculateEnergyChange(ActionType.SpecialAbility, "Dokagui", state, actionData, gameParams, isMorning, localMorningAction);
        if (dokaguiEnergyValueText != null)
            dokaguiEnergyValueText.text = FormatChange(energyChange);

        // 重さ増減量
        int weightChange = ActionCalculator.CalculateWeightChange(ActionType.SpecialAbility, "Dokagui", state, gameParams);
        if (dokaguiWeightValueText != null)
            dokaguiWeightValueText.text = FormatChange(weightChange);

        // 病気確率
        float sicknessProbability = ActionCalculator.CalculateSpecialAbilitySicknessProbability(state, gameParams);
        if (dokaguiSicknessProbabilityText != null)
            dokaguiSicknessProbabilityText.text = ActionCalculator.FormatProbability(sicknessProbability);
    }

    /// <summary>
    /// 旧バージョン：個別の特殊能力の常時表示を更新（非推奨：6つのパネルに移行）
    /// </summary>
    private void UpdateSpecialAbilityEffectDisplay(
        string abilityName,
        TextMeshProUGUI energyValueText,
        TextMeshProUGUI weightValueText,
        UyopyonState state,
        PlayerActionData actionData,
        GameParameters gameParams,
        bool isMorning)
    {
        int energyChange = ActionCalculator.CalculateEnergyChange(ActionType.SpecialAbility, abilityName, 
                                                                   state, actionData, gameParams, isMorning);
        int weightChange = ActionCalculator.CalculateWeightChange(ActionType.SpecialAbility, abilityName, state, gameParams);

        // 元気値テキストの更新
        if (energyValueText != null)
        {
            if (energyChange != 0)
            {
                energyValueText.gameObject.SetActive(true);
                energyValueText.text = FormatChange(energyChange);
            }
            else
            {
                energyValueText.gameObject.SetActive(false);
            }
        }

        // 重さ値テキストの更新
        if (weightValueText != null)
        {
            if (weightChange != 0)
            {
                weightValueText.gameObject.SetActive(true);
                weightValueText.text = FormatChange(weightChange);
            }
            else
            {
                weightValueText.gameObject.SetActive(false);
            }
        }
    }

    // === 連続使用デバフ表示関連メソッド ===

    /// <summary>
    /// 指定された行動が連続使用デバフの対象かどうかを判定する
    /// 連続使用表示: 1つ前に選択した行動と同じ行動に表示される
    /// - 午前選択中: 前日午後と同じ行動に表示
    /// - 午後選択中: 今日午前と同じ行動に表示
    /// </summary>
    /// <param name="actionType">判定する行動タイプ</param>
    /// <param name="abilityName">特殊能力の場合の特殊能力名</param>
    /// <returns>連続使用表示の対象であればtrue</returns>
    private bool IsConsecutiveUse(ActionType actionType, string abilityName = null)
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null) return false;

        PlayerActionData myActionData = GameManager.Instance?.GetPlayerActionData(runner.LocalPlayer);
        if (myActionData == null) return false;

        ActionData? previousAction = null;

        // 午前選択中か午後選択中かで、比較対象を変える
        if (!_isMorningSelected)
        {
            // 午前選択中: 前日午後の行動と比較
            previousAction = myActionData.LastAfternoonAction;
        }
        else
        {
            // 午後選択中: 今日午前の行動と比較（ローカルの_morningActionを使用）
            previousAction = _morningAction;
        }

        // 比較対象の行動がない場合は表示しない
        if (!previousAction.HasValue) return false;

        // 判定する行動タイプと前回の行動タイプが一致するかチェック
        if (actionType != previousAction.Value.Type)
        {
            return false;
        }

        // 特殊能力の場合は特殊能力名も同じかどうかをチェック
        if (actionType == ActionType.SpecialAbility)
        {
            if (string.IsNullOrEmpty(abilityName)) return false;
            if (previousAction.Value.SpecialAbilityName.ToString() != abilityName)
            {
                return false;
            }
        }

        // 前回と同じ行動なので連続使用表示の対象
        return true;
    }

    /// <summary>
    /// 全ての行動ボタンの連続使用表示を更新する
    /// </summary>
    private void UpdateConsecutiveUseIndicators()
    {
        // 基本行動
        UpdateConsecutiveIndicator(ActionType.Eat, null, eatConsecutiveIndicator);
        UpdateConsecutiveIndicator(ActionType.Sleep, null, sleepConsecutiveIndicator);
        UpdateConsecutiveIndicator(ActionType.Play, null, playConsecutiveIndicator);
        UpdateConsecutiveIndicator(ActionType.Clinic, null, clinicConsecutiveIndicator);

        // 特殊能力（進化後のみ、共通のIndicatorを使用）
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            UyopyonState myState = GameManager.Instance?.GetUyopyonState(runner.LocalPlayer);
            if (myState != null && myState.HasEvolved && !string.IsNullOrEmpty(myState.SpecialAbilityName.ToString()))
            {
                string abilityName = myState.SpecialAbilityName.ToString();
                // どの特殊能力でも共通のIndicatorを使用
                UpdateConsecutiveIndicator(ActionType.SpecialAbility, abilityName, specialAbilityConsecutiveIndicator);
            }
        }
    }

    /// <summary>
    /// 個別の行動ボタンの連続使用表示を更新する
    /// </summary>
    private void UpdateConsecutiveIndicator(ActionType actionType, string abilityName, GameObject consecutiveIndicator)
    {
        if (consecutiveIndicator == null) return;

        bool isConsecutive = IsConsecutiveUse(actionType, abilityName);
        consecutiveIndicator.SetActive(isConsecutive);
    }

    /// <summary>
    /// 全ての連続使用表示を非表示にする
    /// </summary>
    private void HideAllConsecutiveUseIndicators()
    {
        // 基本行動
        if (eatConsecutiveIndicator != null) eatConsecutiveIndicator.SetActive(false);
        if (sleepConsecutiveIndicator != null) sleepConsecutiveIndicator.SetActive(false);
        if (playConsecutiveIndicator != null) playConsecutiveIndicator.SetActive(false);
        if (clinicConsecutiveIndicator != null) clinicConsecutiveIndicator.SetActive(false);

        // 特殊能力（共通Indicator）
        if (specialAbilityConsecutiveIndicator != null) specialAbilityConsecutiveIndicator.SetActive(false);
    }

    /// <summary>
    /// 数値の変化をフォーマットする（+10、-15など）
    /// </summary>
    private string FormatChange(int value)
    {
        if (value > 0)
        {
            return $"+{value}";
        }
        else if (value < 0)
        {
            return value.ToString();
        }
        else
        {
            return "±0";
        }
    }


    /// <summary>
    /// TextMeshProのテキストを更新（変更がある場合のみ）
    /// メッシュ再生成を最小化するための最適化
    /// </summary>
    private void SetTextIfChanged(TextMeshProUGUI textComponent, string newText)
    {
        if (textComponent != null && textComponent.text != newText)
        {
            textComponent.text = newText;
        }
    }

    /// <summary>
    /// ツールチップを非表示にする
    /// </summary>
    public void HideActionTooltip()
    {
        if (eatTooltipPanel != null) eatTooltipPanel.SetActive(false);
        if (sleepTooltipPanel != null) sleepTooltipPanel.SetActive(false);
        if (playTooltipPanel != null) playTooltipPanel.SetActive(false);
        if (clinicTooltipPanel != null) clinicTooltipPanel.SetActive(false);
        if (specialAbilityTooltipPanel != null) specialAbilityTooltipPanel.SetActive(false);
    }

    // === タイマー表示関連 ===

    [Header("Timer UI")]
    [SerializeField] private TimerDisplay timerDisplay;

    /// <summary>
    /// タイマー表示を更新
    /// GameFlowManagerから呼び出される（RPCまたはRender）
    /// </summary>
    public void UpdateTimerDisplay(int remainingTime, int maxTime)
    {
        if (timerDisplay != null)
        {
            timerDisplay.UpdateTimer(remainingTime, maxTime);
        }
    }

    /// <summary>
    /// タイマーパネルを表示
    /// </summary>
    public void ShowTimer()
    {
        if (timerDisplay != null)
        {
            timerDisplay.Show();
        }
    }

    /// <summary>
    /// タイマーパネルを非表示
    /// </summary>
    public void HideTimer()
    {
        if (timerDisplay != null)
        {
            timerDisplay.Hide();
        }
    }


}
