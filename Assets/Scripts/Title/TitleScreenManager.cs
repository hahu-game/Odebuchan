using Fusion;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        //プレイヤーネームのデフォルトリセット。
        //デバッグ完了後、この行は削除またはコメントアウトしてください。
        //PlayerPrefs.DeleteKey(GetPlayerNameKey());

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
        SetMatchingUIActive(false);

        if (playerNameInputField != null)
        {
            Debug.Log($"[Awake] playerNameInputField.interactable AFTER SetMatchingUIActive(false): {playerNameInputField.interactable}");
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

    private void Start()
    {
        // タイトルBGM再生（AudioManagerが見つかるまで待機）
        StartCoroutine(PlayTitleBGMCoroutine());
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

        // 1. オーバーレイパネルの表示/非表示を切り替え
        if (matchingOverlayPanel != null)
        {
            matchingOverlayPanel.SetActive(isActive);
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
        // 接続成功または失敗、切断時に呼ばれる
        SetMatchingUIActive(false);
    }

    // ------------------------------------------------------------------
    // ボタンクリック処理
    // ------------------------------------------------------------------

    public async void OnRandomMatchClicked()
    {
        Debug.Log($"[OnRandomMatchClicked] ===== ボタンクリック =====");
        Debug.Log($"[OnRandomMatchClicked] InputField.text: '{playerNameInputField.text}'");

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

        Debug.Log("[OnRandomMatchClicked] SavePlayerName() を呼び出します。");
        SavePlayerName();

        Debug.Log("[OnRandomMatchClicked] SetMatchingUIActive(true) を呼び出します。");
        SetMatchingUIActive(true);

        Debug.Log("[OnRandomMatchClicked] StartGame() を呼び出します。sessionName=null");
        // ランダムマッチング：sessionName に null を渡す
        await _activeRunnerHandlerInstance.StartGame(Fusion.GameMode.Shared, null);

        Debug.Log("[OnRandomMatchClicked] StartGame() が完了しました。");
    }

    public async void OnFriendMatchClicked()
    {
        Debug.Log($"[OnFriendMatchClicked] ボタンクリック時のInputField.text: '{playerNameInputField.text}'");

        // プレイヤー名のバリデーション
        if (!ValidatePlayerName())
        {
            return; // バリデーションエラーの場合、処理を中断
        }

        // 【修正箇所】: 合言葉の入力チェックを先に行う
        string sessionName = sessionNameInputField.text;

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            Debug.LogWarning("フレンドマッチには合言葉（セッション名）の入力が必要です。");
            // エラーメッセージを3秒間表示
            ShowErrorMessage("合言葉を入力してください。", 3f);
            return; // ここで処理を終了し、画面が固まらないようにする
        }

        // NetworkRunnerHandlerの有効性をチェックし、再取得する
        if (!CheckAndRestoreRunnerHandler()) return;

        SavePlayerName();
        SetMatchingUIActive(true); // マッチングUIを表示し、他を操作不可にする

        // マッチングが成功/失敗するまでこのメソッドはブロックされる（待機する）
        await _activeRunnerHandlerInstance.StartGame(Fusion.GameMode.Shared, sessionName);
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
    // プレイヤー名バリデーション
    // ------------------------------------------------------------------

    /// <summary>
    /// プレイヤー名のバリデーションを行う
    /// </summary>
    /// <returns>バリデーション成功でtrue、失敗でfalse</returns>
    private bool ValidatePlayerName()
    {
        string playerName = playerNameInputField.text;
        Debug.Log($"[ValidatePlayerName] プレイヤー名チェック: '{playerName}' ({playerName.Length}文字)");

        // プレイヤー名が8文字を超えている場合
        if (!string.IsNullOrEmpty(playerName) && playerName.Length > 8)
        {
            Debug.LogWarning($"プレイヤー名が8文字を超えています: {playerName.Length}文字");
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
