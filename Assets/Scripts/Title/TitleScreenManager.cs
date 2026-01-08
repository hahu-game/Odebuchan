using Fusion;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面のUIを管理し、ネットワーク接続を開始する。
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    public NetworkRunnerHandler runnerHandlerPrefab;

    // InspectorでTMP_InputFieldをアサイン
    public TMP_InputField playerNameInputField;
    public TMP_InputField sessionNameInputField;

    // === 新しく追加されたUI参照 ===
    // InspectorでGameObject/Componentをアサイン
    public GameObject matchingOverlayPanel; // マッチング中のパネル全体
    public Button cancelButton;             // キャンセルボタン
    public Button randomMatchButton;        // ランダムマッチボタン
    public Button friendMatchButton;        // フレンドマッチボタン
    public TextMeshProUGUI errorMessageText; // エラーメッセージ表示用テキスト（フレンドマッチボタンの上）
    public TextMeshProUGUI playerNameErrorMessageText; // プレイヤー名エラーメッセージ表示用テキスト（PlayerNameInputFieldの下）

    // PlayerPrefsのキーを取得（ParrelSync対応）
    public static string GetPlayerNameKey()
    {
        // ParrelSyncのクローンかどうかを判定
        // ParrelSyncはプロジェクトパスに"_clone_"を含む
        string projectPath = UnityEngine.Application.dataPath;
        if (projectPath.Contains("_clone_"))
        {
            // クローン番号を抽出してキーに追加
            int cloneIndex = projectPath.IndexOf("_clone_");
            int endIndex = projectPath.IndexOf("\\", cloneIndex);
            if (endIndex == -1) endIndex = projectPath.Length;
            string cloneSuffix = projectPath.Substring(cloneIndex, endIndex - cloneIndex);
            return "UyopyonPlayerName" + cloneSuffix;
        }
        return "UyopyonPlayerName";
    }

    public const string PLAYER_NAME_KEY = "UyopyonPlayerName"; // 互換性のため残すが、GetPlayerNameKey()を使用すること

    // シングルトン（NetworkRunnerHandlerからUI制御を呼び出すため）
    public static TitleScreenManager Instance { get; private set; }

    private NetworkRunnerHandler _activeRunnerHandlerInstance;

    // マッチング処理中フラグ（二重実行防止）
    private bool _isMatchingInProgress = false;

    private void Awake()
    {
        // シングルトンチェック：既に別のインスタンスが存在する場合は即座にreturn
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TitleScreenManager Awake] 既存のInstanceが存在するため、このGameObjectを破棄します");
            Destroy(gameObject);
            return; // IMPORTANT: 即座にreturnして、以降の処理を実行しない
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // シーンリロード時もこのインスタンスを維持
        Debug.Log("[TitleScreenManager Awake] このインスタンスをシングルトンとして設定し、DontDestroyOnLoadを適用しました");

        // シーンがロードされたときのイベントを登録
        SceneManager.sceneLoaded += OnSceneLoaded;

        //プレイヤーネームのデフォルトリセット。
        //デバッグ完了後、この行は削除またはコメントアウトしてください。
        //PlayerPrefs.DeleteKey(GetPlayerNameKey());

        // TextMeshProリッチテキストタグを無効化（セキュリティ対策）
        if (playerNameInputField != null)
        {
            playerNameInputField.richText = false;
            Debug.Log("[Awake] playerNameInputField のリッチテキストを無効化しました");
        }
        if (sessionNameInputField != null)
        {
            sessionNameInputField.richText = false;
            Debug.Log("[Awake] sessionNameInputField のリッチテキストを無効化しました");
        }

        // プレイヤー名のロード（デフォルトは空欄）
        string key = GetPlayerNameKey();
        string loadedName = PlayerPrefs.GetString(key, "");
        Debug.Log($"[Awake] PlayerPrefsから名前をロード: key='{key}', value='{loadedName}'");
        playerNameInputField.text = loadedName;

        Debug.Log($"[Awake] playerNameInputField null check: {(playerNameInputField == null ? "NULL" : "OK")}");
        if (playerNameInputField != null)
        {
            Debug.Log($"[Awake] playerNameInputField.gameObject.name: {playerNameInputField.gameObject.name}");
            Debug.Log($"[Awake] playerNameInputField.interactable BEFORE SetMatchingUIActive: {playerNameInputField.interactable}");
        }

        // 初期状態ではマッチングUIを非表示にしておく
        // ただし、マッチング処理中の場合（シーンリロード時など）は実行しない
        if (!_isMatchingInProgress)
        {
            SetMatchingUIActive(false);
            Debug.Log("[Awake] SetMatchingUIActive(false) を実行しました（マッチング処理中ではないため）");
        }
        else
        {
            Debug.LogWarning("[Awake] マッチング処理中のため、SetMatchingUIActive(false) をスキップしました");
        }

        if (playerNameInputField != null)
        {
            Debug.Log($"[Awake] playerNameInputField.interactable AFTER SetMatchingUIActive: {playerNameInputField.interactable}");
        }

        // エラーメッセージを非表示にし、Raycast Targetをオフにする
        if (errorMessageText != null)
        {
            errorMessageText.raycastTarget = false; // クリックをブロックしないようにする
        }
        HideErrorMessage();

        // プレイヤー名エラーメッセージを非表示にし、Raycast Targetをオフにする
        if (playerNameErrorMessageText != null)
        {
            playerNameErrorMessageText.raycastTarget = false; // クリックをブロックしないようにする
        }
        HidePlayerNameError();
    }

    private void OnDestroy()
    {
        // イベントの登録解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// シーンがロードされたときに呼ばれる
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TitleScreenManager] OnSceneLoaded: scene={scene.name}");

        // TitleSceneに戻ってきたときにUI状態を初期化
        if (scene.name == "TitleScene")
        {
            Debug.Log("[TitleScreenManager] TitleSceneに戻ってきました。UI状態を初期化します");
            InitializeUIState();
        }
    }

    private void Start()
    {
        // タイトルBGM再生（AudioManagerが見つかるまで待機）
        StartCoroutine(PlayTitleBGMCoroutine());
    }

    /// <summary>
    /// タイトル画面のUI状態を初期化する
    /// ゲーム起動時とGameSceneから戻ってきた時の両方で使用
    /// </summary>
    private void InitializeUIState()
    {
        Debug.Log("[TitleScreenManager] UI状態を初期化します");

        // 1. ボタンの状態をリセット
        if (randomMatchButton != null)
        {
            randomMatchButton.interactable = true;
            Debug.Log("[TitleScreenManager] randomMatchButton.interactable = true");
        }

        if (friendMatchButton != null)
        {
            friendMatchButton.interactable = true;
            Debug.Log("[TitleScreenManager] friendMatchButton.interactable = true");
        }

        if (cancelButton != null)
        {
            cancelButton.interactable = true;
        }

        // 2. 入力フィールドの状態をリセット
        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = true;
        }

        if (sessionNameInputField != null)
        {
            sessionNameInputField.interactable = true;
        }

        // 3. マッチングUIを非表示にする
        if (matchingOverlayPanel != null)
        {
            matchingOverlayPanel.SetActive(false);
            Debug.Log("[TitleScreenManager] matchingOverlayPanel を非表示にしました");
        }

        // 4. エラーメッセージを非表示にする
        HideErrorMessage();
        HidePlayerNameError();

        // 5. 降参ボタンを非表示にする（GameSceneでのみ表示）
        if (SettingController.Instance != null)
        {
            SettingController.Instance.SetSurrenderButtonVisible(false);
            Debug.Log("[TitleScreenManager] 降参ボタンを非表示にしました");
        }

        // 6. BGMをタイトルBGMに切り替える（リザルトBGMなどが流れている場合）
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlayTitleBGM();
            Debug.Log("[TitleScreenManager] タイトルBGMを再生しました");
        }

        Debug.Log("[TitleScreenManager] UI状態の初期化が完了しました");
    }

    private System.Collections.IEnumerator PlayTitleBGMCoroutine()
    {
        // AudioManagerが初期化されるまで待機（最大1秒）
        float timeout = 1f;
        float elapsed = 0f;

        while (AudioManager.Instance == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (AudioManager.Instance != null)
        {
            // 既存のBGM（リザルトBGMなど）を停止してからタイトルBGMを再生
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlayTitleBGM();
            Debug.Log("[TitleScreenManager] タイトルBGMを再生開始");
        }
        else
        {
            Debug.LogWarning("[TitleScreenManager] AudioManager.Instance が null のため、タイトルBGMを再生できません。TitleSceneにAudioManagerを配置してください。");
        }
    }

    /// <summary>
    /// マッチングUIの表示を切り替え、他のボタンとインプットフィールドを操作不可にする
    /// </summary>
    private void SetMatchingUIActive(bool isActive)
    {
        Debug.Log($"[SetMatchingUIActive] isActive={isActive}");
        Debug.Log($"[SetMatchingUIActive] 呼び出し元スタックトレース:\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");

        // GameSceneから呼ばれた場合（TitleSceneのUI要素が存在しない場合）は早期return
        if (playerNameInputField == null || matchingOverlayPanel == null)
        {
            Debug.LogWarning("[SetMatchingUIActive] TitleSceneのUI要素が存在しないため、処理をスキップします（GameSceneから呼ばれた可能性）");
            return;
        }

        // 1. オーバーレイパネルの表示/非表示を切り替え
        if (matchingOverlayPanel != null)
        {
            // CanvasGroupがあればそれも制御
            var canvasGroup = matchingOverlayPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isActive ? 1f : 0f;
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive; // これが重要：Raycastをブロックしないようにする
                Debug.Log($"[SetMatchingUIActive] CanvasGroup: alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
            }

            matchingOverlayPanel.SetActive(isActive);
            Debug.Log($"[SetMatchingUIActive] matchingOverlayPanel.SetActive({isActive}) 実行完了。現在のactiveSelf={matchingOverlayPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[SetMatchingUIActive] matchingOverlayPanel is NULL!");
        }

        // 2. 他のボタンとインプットフィールドの操作を制御
        if (randomMatchButton != null)
        {
            randomMatchButton.interactable = !isActive;
            Debug.Log($"[SetMatchingUIActive] randomMatchButton.interactable = {!isActive}");
        }
        if (friendMatchButton != null)
        {
            friendMatchButton.interactable = !isActive;
            Debug.Log($"[SetMatchingUIActive] friendMatchButton.interactable = {!isActive}");
        }

        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = !isActive;
            Debug.Log($"[SetMatchingUIActive] playerNameInputField.interactable = {!isActive}");
        }
        else
        {
            Debug.LogError($"[SetMatchingUIActive] playerNameInputField is NULL!");
        }

        if (sessionNameInputField != null)
        {
            sessionNameInputField.interactable = !isActive;
            Debug.Log($"[SetMatchingUIActive] sessionNameInputField.interactable = {!isActive}");
        }
    }

    /// <summary>
    /// NetworkRunnerHandlerから呼ばれ、UIを通常状態に戻す
    /// </summary>
    public void HideMatchingUI()
    {
        Debug.Log("[HideMatchingUI] 呼び出されました");

        // マッチング処理中フラグを解除
        _isMatchingInProgress = false;
        Debug.Log("[HideMatchingUI] _isMatchingInProgress = false に設定");

        // 接続成功または失敗、切断時に呼ばれる
        SetMatchingUIActive(false);

        // 念のため、matchingOverlayPanelのCanvasGroupとImageを強制的に無効化
        if (matchingOverlayPanel != null)
        {
            var canvasGroup = matchingOverlayPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                Debug.Log("[HideMatchingUI] CanvasGroupのblocksRaycastsをfalseに設定しました");
            }

            // ImageコンポーネントのRaycast Targetも無効化
            var image = matchingOverlayPanel.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.raycastTarget = false;
                Debug.Log("[HideMatchingUI] ImageのraycastTargetをfalseに設定しました");
            }
        }

        // 念のため、すべてのUI要素を強制的に活性化
        if (randomMatchButton != null)
        {
            randomMatchButton.interactable = true;
            Debug.Log($"[HideMatchingUI] randomMatchButton.interactable を強制的にtrueに設定しました");
        }

        if (friendMatchButton != null)
        {
            friendMatchButton.interactable = true;
            Debug.Log($"[HideMatchingUI] friendMatchButton.interactable を強制的にtrueに設定しました");
        }

        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = true;
            Debug.Log($"[HideMatchingUI] playerNameInputField.interactable を強制的にtrueに設定しました");
        }

        if (sessionNameInputField != null)
        {
            sessionNameInputField.interactable = true;
            Debug.Log($"[HideMatchingUI] sessionNameInputField.interactable を強制的にtrueに設定しました");
        }

        Debug.Log("[HideMatchingUI] 処理完了");

        // 親CanvasGroupがボタンをブロックしていないかチェック
        if (randomMatchButton != null)
        {
            var parentCanvasGroups = randomMatchButton.GetComponentsInParent<CanvasGroup>();
            Debug.Log($"[HideMatchingUI] randomMatchButtonの親CanvasGroup数: {parentCanvasGroups.Length}");
            for (int i = 0; i < parentCanvasGroups.Length; i++)
            {
                var cg = parentCanvasGroups[i];
                Debug.Log($"[HideMatchingUI] 親CanvasGroup[{i}] (GameObject: {cg.gameObject.name}): alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}");

                // interactable=falseの親CanvasGroupがあれば警告
                if (!cg.interactable)
                {
                    Debug.LogError($"[HideMatchingUI] 親CanvasGroup '{cg.gameObject.name}' のinteractableがfalseです！これがUIをブロックしている可能性があります。");
                }
            }
        }
    }


    // ------------------------------------------------------------------
    // ボタンクリック処理
    // ------------------------------------------------------------------

    public async void OnRandomMatchClicked()
    {
        Debug.Log($"[OnRandomMatchClicked] ===== ボタンクリック =====");
        Debug.Log($"[OnRandomMatchClicked] InputField.text: '{playerNameInputField.text}'");

        // 二重実行防止チェック
        if (_isMatchingInProgress)
        {
            Debug.LogWarning("[OnRandomMatchClicked] 既にマッチング処理中です。処理を中断します。");
            return;
        }

        // プレイヤー名のバリデーション
        if (!ValidatePlayerName())
        {
            Debug.LogWarning("[OnRandomMatchClicked] バリデーション失敗。処理を中断します。");
            return; // バリデーションエラーの場合、処理を中断
        }

        Debug.Log("[OnRandomMatchClicked] CheckAndRestoreRunnerHandler() を呼び出します。");
        if (!CheckAndRestoreRunnerHandler())
        {
            Debug.LogError("[OnRandomMatchClicked] CheckAndRestoreRunnerHandler() が false を返しました。処理を中断します。");
            return;
        }

        // マッチング処理開始：フラグを設定し、ボタンを即座に無効化
        _isMatchingInProgress = true;
        Debug.Log("[OnRandomMatchClicked] _isMatchingInProgress = true に設定");

        // ボタンを即座に無効化（ダブルクリック防止）
        if (randomMatchButton != null) randomMatchButton.interactable = false;
        if (friendMatchButton != null) friendMatchButton.interactable = false;

        try
        {
            Debug.Log("[OnRandomMatchClicked] SavePlayerName() を呼び出します。");
            SavePlayerName();

            Debug.Log("[OnRandomMatchClicked] SetMatchingUIActive(true) を呼び出します。");
            SetMatchingUIActive(true);

            Debug.Log("[OnRandomMatchClicked] StartGame() を呼び出します。sessionName=null");
            // ランダムマッチング：sessionName に null を渡す
            await _activeRunnerHandlerInstance.StartGame(Fusion.GameMode.Shared, null);

            Debug.Log("[OnRandomMatchClicked] StartGame() が完了しました。");
        }
        finally
        {
            // 処理が完了したらフラグを解除（キャンセルや失敗時も含む）
            // ただし、マッチング成功時はシーン遷移するのでフラグは不要
            // マッチング失敗やキャンセル時のみ解除が必要
            Debug.Log("[OnRandomMatchClicked] finally: マッチング処理終了");
        }
    }

    public async void OnFriendMatchClicked()
    {
        Debug.Log($"[OnFriendMatchClicked] ボタンクリック時のInputField.text: '{playerNameInputField.text}'");

        // 二重実行防止チェック
        if (_isMatchingInProgress)
        {
            Debug.LogWarning("[OnFriendMatchClicked] 既にマッチング処理中です。処理を中断します。");
            return;
        }

        // NOTE: SE再生はButtonSoundPlayer.PlayFriendMatchButtonSE()に任せる（Inspector設定）
        // ここでSEを再生すると二重再生になるため削除

        // プレイヤー名のバリデーション
        if (!ValidatePlayerName())
        {
            return; // バリデーションエラーの場合、処理を中断
        }

        // 合言葉の入力状態をチェック
        string rawSessionName = sessionNameInputField.text;
        string sanitizedSessionName = SanitizeSessionName(rawSessionName);

        // サニタイズした値をInputFieldに反映
        if (rawSessionName != sanitizedSessionName)
        {
            sessionNameInputField.text = sanitizedSessionName;
            Debug.Log($"[OnFriendMatchClicked] セッション名をサニタイズして反映しました");
        }

        if (string.IsNullOrWhiteSpace(sanitizedSessionName))
        {
            Debug.LogWarning("フレンドマッチには合言葉（セッション名）の入力が必要です。");
            // エラーメッセージを3秒間表示
            ShowErrorMessage("合言葉を入力してください。", 3f);
            return; // ここで処理を終了し、画面が固まらないようにする
        }

        // NetworkRunnerHandlerの有効性をチェックし、再取得する
        if (!CheckAndRestoreRunnerHandler()) return;

        // マッチング処理開始：フラグを設定し、ボタンを即座に無効化
        _isMatchingInProgress = true;
        Debug.Log("[OnFriendMatchClicked] _isMatchingInProgress = true に設定");

        // ボタンを即座に無効化（ダブルクリック防止）
        if (randomMatchButton != null) randomMatchButton.interactable = false;
        if (friendMatchButton != null) friendMatchButton.interactable = false;

        try
        {
            SavePlayerName();
            SetMatchingUIActive(true); // マッチングUIを表示し、他を操作不可にする

            // マッチングが成功/失敗するまでこのメソッドはブロックされる（待機する）
            await _activeRunnerHandlerInstance.StartGame(Fusion.GameMode.Shared, sanitizedSessionName);
        }
        finally
        {
            // 処理が完了したらフラグを解除（キャンセルや失敗時も含む）
            Debug.Log("[OnFriendMatchClicked] finally: マッチング処理終了");
        }
    }

    public void OnCancelMatchClicked()
    {
        if (_activeRunnerHandlerInstance != null)
        {
            _activeRunnerHandlerInstance.ShutdownRunner();
        }

        // インスタンスはShutdownRunner/OnShutdownで破壊されるので、
        // 参照をクリア
        _activeRunnerHandlerInstance = null;

        HideMatchingUI();
    }

    // ------------------------------------------------------------------
    // ヘルパーメソッド
    // ------------------------------------------------------------------

    /// <summary>
    /// 現在アクティブなNetworkRunnerHandlerインスタンスが有効かチェックし、
    /// 無効であればプレハブから新規生成する。
    /// </summary>
    /// <returns>アクティブなインスタンスが有効であればtrue</returns>
    private bool CheckAndRestoreRunnerHandler()
    {
        // 1. アクティブなインスタンスが既に存在し、有効かチェック
        // Unityオブジェクトのnullチェック（破壊されていないかチェック）
        if (_activeRunnerHandlerInstance != null)
        {
            return true;
        }

        // 2. プレハブがアサインされているかチェック
        if (runnerHandlerPrefab == null)
        {
            Debug.LogError("FATAL ERROR: NetworkRunnerHandler PrefabがTitleScreenManagerにアサインされていません。");
            SetMatchingUIActive(false);
            return false;
        }

        // 3. インスタンスが破壊されていたため、プレハブから新規生成
        Debug.Log("NetworkRunnerHandlerが破壊されたため、プレハブから新規生成します。");

        // Instantiateでプレハブから新しいインスタンスを生成
        _activeRunnerHandlerInstance = Instantiate(runnerHandlerPrefab);
        _activeRunnerHandlerInstance.gameObject.name = "NetworkRunner_Instance";

        // 生成に成功
        if (_activeRunnerHandlerInstance != null)
        {
            return true;
        }

        // 最終的なエラー
        Debug.LogError("NetworkRunnerHandlerの生成に失敗しました。2回目のマッチングを開始できません。");
        SetMatchingUIActive(false);
        return false;
    }

    private void SavePlayerName()
    {
        string playerName = playerNameInputField.text;
        Debug.Log($"[SavePlayerName] InputFieldから取得した名前: '{playerName}' ({playerName.Length}文字)");

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "野良うーぴょん";
            Debug.Log($"[SavePlayerName] 空欄だったためデフォルト名を設定: '{playerName}'");
        }

        string key = GetPlayerNameKey();
        Debug.Log($"[SavePlayerName] PlayerPrefsに保存: key='{key}', value='{playerName}'");
        PlayerPrefs.SetString(key, playerName);
        PlayerPrefs.Save();

        // 保存後、実際に保存された値を確認
        string savedValue = PlayerPrefs.GetString(key, "NOT_FOUND");
        Debug.Log($"[SavePlayerName] 保存確認: key='{key}'から読み込んだ値='{savedValue}'");
    }

    // ------------------------------------------------------------------
    // エラーメッセージ表示
    // ------------------------------------------------------------------

    /// <summary>
    /// エラーメッセージを指定秒数だけ表示する
    /// </summary>
    private void ShowErrorMessage(string message, float duration)
    {
        if (errorMessageText == null)
        {
            Debug.LogWarning("errorMessageText が設定されていません。Inspectorで設定してください。");
            return;
        }

        // コルーチンでメッセージ表示処理を開始（WebGL互換性のため）
        StartCoroutine(ShowErrorMessageCoroutine(message, duration));
    }

    /// <summary>
    /// エラーメッセージ表示のコルーチン（WebGL互換性のため）
    /// </summary>
    private System.Collections.IEnumerator ShowErrorMessageCoroutine(string message, float duration)
    {
        // Raycast Targetをオフにして、クリックをブロックしないようにする
        errorMessageText.raycastTarget = false;

        // メッセージを設定して表示
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);

        // 指定秒数待機（WebGL環境でも動作する）
        yield return new WaitForSeconds(duration);

        // メッセージを非表示にする
        HideErrorMessage();
    }

    /// <summary>
    /// エラーメッセージを非表示にする
    /// </summary>
    private void HideErrorMessage()
    {
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // 文字列サニタイズ
    // ------------------------------------------------------------------

    /// <summary>
    /// 文字列をサニタイズして安全な形式に変換する
    /// - TextMeshProリッチテキストタグをエスケープ
    /// - 制御文字を除去
    /// - 前後の空白をトリム
    /// - 連続する空白を単一の空白に変換
    /// </summary>
    private string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // 1. 前後の空白を削除
        string sanitized = input.Trim();

        // 2. TextMeshProのリッチテキストタグで使用される特殊文字をエスケープ
        // '<' と '>' をエスケープしてタグを無効化
        sanitized = sanitized.Replace("<", "＜"); // 全角に置換
        sanitized = sanitized.Replace(">", "＞"); // 全角に置換

        // 3. 制御文字（改行、タブなど）を除去
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "");

        // 4. 連続する空白を単一の空白に変換
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ");

        // 5. 再度トリム（空白処理後に端に空白が残る可能性があるため）
        sanitized = sanitized.Trim();

        return sanitized;
    }

    /// <summary>
    /// プレイヤー名をサニタイズする
    /// </summary>
    private string SanitizePlayerName(string playerName)
    {
        string sanitized = SanitizeString(playerName);
        Debug.Log($"[SanitizePlayerName] 元の値: '{playerName}' → サニタイズ後: '{sanitized}'");
        return sanitized;
    }

    /// <summary>
    /// セッション名（合言葉）をサニタイズする
    /// </summary>
    private string SanitizeSessionName(string sessionName)
    {
        string sanitized = SanitizeString(sessionName);
        Debug.Log($"[SanitizeSessionName] 元の値: '{sessionName}' → サニタイズ後: '{sanitized}'");
        return sanitized;
    }

    // ------------------------------------------------------------------
    // プレイヤー名バリデーション
    // ------------------------------------------------------------------

    /// <summary>
    /// プレイヤー名のバリデーションを行う
    /// </summary>
    /// <returns>バリデーション成功でtrue、失敗でfalse</returns>
    private bool ValidatePlayerName()
    {
        // 入力値をサニタイズ
        string rawPlayerName = playerNameInputField.text;
        string sanitizedPlayerName = SanitizePlayerName(rawPlayerName);

        // サニタイズした値をInputFieldに反映
        if (rawPlayerName != sanitizedPlayerName)
        {
            playerNameInputField.text = sanitizedPlayerName;
            Debug.Log($"[ValidatePlayerName] プレイヤー名をサニタイズして反映しました");
        }

        Debug.Log($"[ValidatePlayerName] プレイヤー名チェック: '{sanitizedPlayerName}' ({sanitizedPlayerName.Length}文字)");

        // プレイヤー名が8文字を超えている場合
        if (!string.IsNullOrEmpty(sanitizedPlayerName) && sanitizedPlayerName.Length > 8)
        {
            Debug.LogWarning($"プレイヤー名が8文字を超えています: {sanitizedPlayerName.Length}文字");
            Debug.Log($"[ValidatePlayerName] ShowPlayerNameError()を呼び出します");
            // エラーメッセージを3秒間表示
            ShowPlayerNameError("プレイヤー名は８文字以下で入力してください。", 3f);
            return false;
        }

        Debug.Log($"[ValidatePlayerName] バリデーション成功");
        return true;
    }

    /// <summary>
    /// プレイヤー名エラーメッセージを指定秒数だけ表示する
    /// </summary>
    private void ShowPlayerNameError(string message, float duration)
    {
        Debug.Log($"[ShowPlayerNameError] 開始: message='{message}', duration={duration}");
        Debug.Log($"[ShowPlayerNameError] playerNameErrorMessageText null check: {(playerNameErrorMessageText == null ? "NULL" : "OK")}");

        if (playerNameErrorMessageText == null)
        {
            Debug.LogError("playerNameErrorMessageText が設定されていません。Inspectorで設定してください。");
            return;
        }

        // コルーチンでメッセージ表示処理を開始（WebGL互換性のため）
        StartCoroutine(ShowPlayerNameErrorCoroutine(message, duration));
    }

    /// <summary>
    /// プレイヤー名エラーメッセージ表示のコルーチン（WebGL互換性のため）
    /// </summary>
    private System.Collections.IEnumerator ShowPlayerNameErrorCoroutine(string message, float duration)
    {
        Debug.Log($"[ShowPlayerNameError] GameObject名: {playerNameErrorMessageText.gameObject.name}");
        Debug.Log($"[ShowPlayerNameError] GameObject active before: {playerNameErrorMessageText.gameObject.activeSelf}");

        // Raycast Targetをオフにして、クリックをブロックしないようにする
        playerNameErrorMessageText.raycastTarget = false;

        // メッセージを設定して表示
        playerNameErrorMessageText.text = message;
        playerNameErrorMessageText.gameObject.SetActive(true);

        Debug.Log($"[ShowPlayerNameError] GameObject active after: {playerNameErrorMessageText.gameObject.activeSelf}");
        Debug.Log($"[ShowPlayerNameError] Text設定完了: '{playerNameErrorMessageText.text}'");

        // 指定秒数待機（WebGL環境でも動作する）
        yield return new WaitForSeconds(duration);

        // メッセージを非表示にする
        Debug.Log($"[ShowPlayerNameError] {duration}秒経過、非表示にします");
        HidePlayerNameError();
    }

    /// <summary>
    /// プレイヤー名エラーメッセージを非表示にする
    /// </summary>
    private void HidePlayerNameError()
    {
        Debug.Log($"[HidePlayerNameError] 呼び出されました");
        if (playerNameErrorMessageText != null)
        {
            playerNameErrorMessageText.gameObject.SetActive(false);
            Debug.Log($"[HidePlayerNameError] GameObjectを非表示にしました");
        }
        else
        {
            Debug.LogWarning($"[HidePlayerNameError] playerNameErrorMessageText is null");
        }
    }
}
