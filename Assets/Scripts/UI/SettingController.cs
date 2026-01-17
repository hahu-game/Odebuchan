using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;

/// <summary>
/// 設定パネルの制御
/// 音量調整と投了機能を含む
/// シーン間で永続化される（DontDestroyOnLoad）
/// </summary>
public class SettingController : MonoBehaviour
{
    public static SettingController Instance { get; private set; }

    [Header("UI References")]
    public Button settingButton;        // 設定ボタン（歯車アイコン）
    public GameObject settingPanel;
    public Slider bgmVolumeSlider;
    public Slider seVolumeSlider;
    public Button surrenderButton;
    public Button closeButton;

    [Header("Confirm Dialog")]
    public GameObject confirmPanel;
    public Button yesButton;
    public Button noButton;

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SE_VOLUME_KEY = "SEVolume";

    void Awake()
    {
        Debug.Log($"[SettingController] Awake() called. gameObject.name={gameObject.name}, activeInHierarchy={gameObject.activeInHierarchy}");

        // シングルトンパターン + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // シーンロードイベントを登録
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 設定パネルを非表示にする（初期状態）
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        // TitleSceneでは降参ボタンを非表示
        SetSurrenderButtonVisible(false);

        // ボタンのイベント設定（Awakeで行うことで、GameObjectがinactiveでも確実に実行される）
        RegisterButtonEvents();
    }

    void OnDestroy()
    {
        // イベントの登録解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// シーンがロードされたときに呼ばれる
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SettingController] OnSceneLoaded: scene={scene.name}");

        // すべてのパネルを非表示
        ForceHideAllPanels();

        // UI参照を再取得
        ReassignUIReferences();

        // TitleSceneでは降参ボタンを非表示
        if (scene.name == "TitleScene")
        {
            SetSurrenderButtonVisible(false);
        }
    }

    /// <summary>
    /// シーン遷移後にUI要素の参照を再取得する
    /// </summary>
    private void ReassignUIReferences()
    {
        Debug.Log("[SettingController] ReassignUIReferences: UI参照を再取得開始");

        // 全てのGameObjectを取得
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Prefabやアセットを除外
            if (obj.hideFlags == HideFlags.NotEditable || obj.hideFlags == HideFlags.HideAndDontSave)
                continue;

            // シーン内のオブジェクトのみ対象
            if (!obj.scene.IsValid() || !obj.scene.isLoaded)
                continue;

            switch (obj.name)
            {
                case "SettingButton":
                    settingButton = obj.GetComponent<Button>();
                    break;
                case "SettingPanel":
                    settingPanel = obj;
                    break;
                case "BGMVolumeSlider":
                    bgmVolumeSlider = obj.GetComponent<Slider>();
                    break;
                case "SEVolumeSlider":
                    seVolumeSlider = obj.GetComponent<Slider>();
                    break;
                case "SurrenderButton":
                    surrenderButton = obj.GetComponent<Button>();
                    break;
                case "CloseButton":
                    closeButton = obj.GetComponent<Button>();
                    break;
                case "ConfirmPanel":
                    confirmPanel = obj;
                    break;
                case "YesButton":
                    yesButton = obj.GetComponent<Button>();
                    break;
                case "NoButton":
                    noButton = obj.GetComponent<Button>();
                    break;
            }
        }

        Debug.Log($"[SettingController] settingButton: {(settingButton != null ? "OK" : "NULL")}");
        Debug.Log($"[SettingController] settingPanel: {(settingPanel != null ? "OK" : "NULL")}");

        // ボタンイベントを再登録
        RegisterButtonEvents();

        Debug.Log("[SettingController] ReassignUIReferences: UI参照の再取得完了");
    }

    void Start()
    {
        Debug.Log($"[SettingController] Start() called.");

        // 保存された音量設定を読み込み
        LoadVolumeSettings();

        // スライダーのイベント設定
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveAllListeners(); // 重複登録を防ぐ
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (seVolumeSlider != null)
        {
            seVolumeSlider.onValueChanged.RemoveAllListeners(); // 重複登録を防ぐ
            seVolumeSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }

        // 確認パネルのボタンイベント設定
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners(); // 重複登録を防ぐ
            yesButton.onClick.AddListener(OnConfirmYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners(); // 重複登録を防ぐ
            noButton.onClick.AddListener(OnConfirmNoClicked);
        }
    }

    /// <summary>
    /// ボタンのイベントを登録
    /// </summary>
    private void RegisterButtonEvents()
    {
        Debug.Log($"[SettingController] RegisterButtonEvents() called.");
        Debug.Log($"[SettingController] settingButton null check: {(settingButton == null ? "NULL" : "OK")}");

        if (settingButton != null)
        {
            settingButton.onClick.RemoveAllListeners(); // 重複登録を防ぐ
            settingButton.onClick.AddListener(ShowSettingPanel);
            Debug.Log($"[SettingController] settingButton.onClick.AddListener completed.");
            Debug.Log($"[SettingController] Button name: {settingButton.gameObject.name}");
            Debug.Log($"[SettingController] Button activeInHierarchy: {settingButton.gameObject.activeInHierarchy}");
            Debug.Log($"[SettingController] Button interactable: {settingButton.interactable}");
            Debug.Log($"[SettingController] Button enabled: {settingButton.enabled}");

            // 親オブジェクトも確認
            Transform parent = settingButton.transform.parent;
            if (parent != null)
            {
                Debug.Log($"[SettingController] Button parent: {parent.name}, activeInHierarchy: {parent.gameObject.activeInHierarchy}");
            }
        }
        else
        {
            Debug.LogError("[SettingController] settingButton が null です。Inspectorで設定してください。");
        }

        if (surrenderButton != null)
        {
            surrenderButton.onClick.RemoveAllListeners(); // 重複登録を防ぐ
            surrenderButton.onClick.AddListener(OnSurrenderButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // 重複登録を防ぐ
            closeButton.onClick.AddListener(CloseSettingPanel);
        }
    }

    /// <summary>
    /// 設定パネルを表示
    /// </summary>
    public void ShowSettingPanel()
    {
        Debug.Log("[SettingController] ShowSettingPanel() called!");

        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            Debug.Log("[SettingController] 設定パネルを表示しました");
        }
        else
        {
            Debug.LogError("[SettingController] settingPanel が null です");
        }

        // 確認パネルは常に非表示にする
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 設定パネルを閉じる
    /// </summary>
    public void CloseSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
            Debug.Log("[SettingController] 設定パネルを閉じました");
        }

        // 確認パネルも閉じる
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 保存された音量設定を読み込み
    /// </summary>
    private void LoadVolumeSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.3f);  // デフォルト30%
        float seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.7f);    // デフォルト70%

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = bgmVolume;
        }

        if (seVolumeSlider != null)
        {
            seVolumeSlider.value = seVolume;
        }

        // AudioManagerがあれば対応音量を適用
        AudioManager.Instance?.SetBGMVolume(bgmVolume);
        AudioManager.Instance?.SetSEVolume(seVolume);

        Debug.Log($"[SettingController] 音量設定を読み込み: BGM={bgmVolume}, SE={seVolume}");
    }

    /// <summary>
    /// BGM音量変更時の処理
    /// </summary>
    private void OnBGMVolumeChanged(float volume)
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        // AudioManagerがあれば対応音量を適用
        AudioManager.Instance?.SetBGMVolume(volume);

        Debug.Log($"[SettingController] BGM音量を変更: {volume}");
    }

    /// <summary>
    /// SE音量変更時の処理
    /// </summary>
    private void OnSEVolumeChanged(float volume)
    {
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        // AudioManagerがあれば対応音量を適用
        AudioManager.Instance?.SetSEVolume(volume);

        Debug.Log($"[SettingController] SE音量を変更: {volume}");
    }

    /// <summary>
    /// 投了ボタンクリック時の処理
    /// </summary>
    private void OnSurrenderButtonClicked()
    {
        Debug.Log("[SettingController] 投了ボタンがクリックされました");

        // 確認パネルを表示
        ShowConfirmPanel();
    }

    /// <summary>
    /// 確認パネルを表示
    /// </summary>
    private void ShowConfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            Debug.Log("[SettingController] 確認パネルを表示しました");
        }
    }

    /// <summary>
    /// 確認パネルを閉じる
    /// </summary>
    private void CloseConfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
            Debug.Log("[SettingController] 確認パネルを閉じました");
        }
    }

    /// <summary>
    /// すべてのパネルを強制的に非表示にする（TitleSceneに戻ったときなどに使用）
    /// </summary>
    public void ForceHideAllPanels()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
            Debug.Log("[SettingController] 設定パネルを強制非表示");
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
            Debug.Log("[SettingController] 確認パネルを強制非表示");
        }
    }

    /// <summary>
    /// 確認パネル「はい」ボタンクリック時の処理
    /// </summary>
    private void OnConfirmYesClicked()
    {
        Debug.Log("[SettingController] 「はい」ボタンがクリックされました");

        // 確認パネルを閉じる
        CloseConfirmPanel();

        // 投了処理を実行
        ExecuteSurrender();
    }

    /// <summary>
    /// 確認パネル「いいえ」ボタンクリック時の処理
    /// </summary>
    private void OnConfirmNoClicked()
    {
        Debug.Log("[SettingController] 「いいえ」ボタンがクリックされました");

        // 確認パネルを閉じる、設定パネルに戻る
        CloseConfirmPanel();
    }

    /// <summary>
    /// 投了処理を実行
    /// </summary>
    private void ExecuteSurrender()
    {
        // GameFlowManagerに投了を通知
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RPC_Surrender();
            Debug.Log("[SettingController] 投了リクエストを送信しました");

            // 確認パネルと設定パネルを閉じる
            CloseConfirmPanel();
            CloseSettingPanel();
        }
        else
        {
            Debug.LogError("[SettingController] GameFlowManager.Instanceが見つかりません");
        }
    }

    /// <summary>
    /// 降参ボタンの表示/非表示を設定
    /// </summary>
    public void SetSurrenderButtonVisible(bool visible)
    {
        if (surrenderButton != null)
        {
            surrenderButton.gameObject.SetActive(visible);
            Debug.Log($"[SettingController] 降参ボタンの表示を設定: {visible}");
        }
    }
}
