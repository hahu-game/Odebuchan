using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// 設定パネルの制御
/// 音量調整と投了機能を含む
/// </summary>
public class SettingController : MonoBehaviour
{
    [Header("UI References")]
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

    void Start()
    {
        // 保存された音量設定を読み込み
        LoadVolumeSettings();

        // ボタンのイベント設定
        if (surrenderButton != null)
        {
            surrenderButton.onClick.AddListener(OnSurrenderButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettingPanel);
        }

        // スライダーのイベント設定
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (seVolumeSlider != null)
        {
            seVolumeSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }

        // 確認パネルのボタンイベント設定
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnConfirmYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnConfirmNoClicked);
        }
    }

    /// <summary>
    /// 設定パネルを表示
    /// </summary>
    public void ShowSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            Debug.Log("[SettingController] 設定パネルを表示しました");
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
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = bgmVolume;
        }

        if (seVolumeSlider != null)
        {
            seVolumeSlider.value = seVolume;
        }

        // AudioManagerがあれば対応音量を適用
        // TODO: AudioManager実装後にコメント解除
        // AudioManager.Instance?.SetBGMVolume(bgmVolume);
        // AudioManager.Instance?.SetSEVolume(seVolume);

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
        // TODO: AudioManager実装後にコメント解除
        // AudioManager.Instance?.SetBGMVolume(volume);

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
        // TODO: AudioManager実装後にコメント解除
        // AudioManager.Instance?.SetSEVolume(volume);

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
}
