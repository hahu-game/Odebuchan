using Fusion;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面のUI操作を管理し、ネットワーク接続を開始する。
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    public NetworkRunnerHandler runnerHandlerPrefab;

    // Inspectorで TMP_InputField をアサイン
    public TMP_InputField playerNameInputField;
    public TMP_InputField sessionNameInputField;

    // === 新しく追加されたUI参照 ===
    // Inspectorで各GameObject/Componentをアサイン
    public GameObject matchingOverlayPanel; // マッチング中のパネル全体
    public Button cancelButton;             // キャンセルボタン
    public Button randomMatchButton;        // ランダムマッチボタン
    public Button friendMatchButton;        // フレンドマッチボタン

    public const string PLAYER_NAME_KEY = "UyopyonPlayerName";

    // シングルトン（NetworkRunnerHandlerからUI制御を呼び出すため）
    public static TitleScreenManager Instance { get; private set; }

    private NetworkRunnerHandler _activeRunnerHandlerInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // プレイヤー名のロード
        playerNameInputField.text = PlayerPrefs.GetString(PLAYER_NAME_KEY, "guest");

        // 初期状態ではマッチングUIを非表示にしておく
        SetMatchingUIActive(false);
    }

    /// <summary>
    /// マッチングUIの表示を切り替え、他のボタンとインプットフィールドを操作不可にする
    /// </summary>
    private void SetMatchingUIActive(bool isActive)
    {
        // 1. オーバーレイパネルの表示/非表示を切り替え
        if (matchingOverlayPanel != null)
        {
            matchingOverlayPanel.SetActive(isActive);
        }

        // 2. 他のボタンとインプットフィールドの操作を制御
        if (randomMatchButton != null)
        {
            randomMatchButton.interactable = !isActive;
        }
        if (friendMatchButton != null)
        {
            friendMatchButton.interactable = !isActive;
        }

        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = !isActive;
        }
        if (sessionNameInputField != null)
        {
            sessionNameInputField.interactable = !isActive;
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
        if (!CheckAndRestoreRunnerHandler()) return;

        SavePlayerName();
        SetMatchingUIActive(true);

        // _activeRunnerHandlerInstance を使用
        await _activeRunnerHandlerInstance.StartGame(Fusion.GameMode.Shared, "RANDOM_POOL_UYOPYON");
    }

    public async void OnFriendMatchClicked()
    {
        // 【修正箇所】: NetworkRunnerHandlerの有効性をチェックし、再取得する
        if (!CheckAndRestoreRunnerHandler()) return;

        SavePlayerName();
        string sessionName = sessionNameInputField.text;

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            Debug.LogError("フレンドマッチには合言葉（セッション名）の入力が必要です。");
            return;
        }

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

        // インスタンスは ShutdownRunner/OnShutdown で破壊されるので、
        // 参照をクリア
        _activeRunnerHandlerInstance = null;

        HideMatchingUI();
    }

    // ------------------------------------------------------------------
    // ヘルパーメソッド
    // ------------------------------------------------------------------

    /// <summary>
    /// 現在アクティブな NetworkRunnerHandler インスタンスが有効かチェックし、
    /// 無効であればプレハブから新規生成する。
    /// </summary>
    /// <returns>アクティブなインスタンスが有効であれば true</returns>
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
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "guest dayo";
        }
        PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
        PlayerPrefs.Save();
    }
}