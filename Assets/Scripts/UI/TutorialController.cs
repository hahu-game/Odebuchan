using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 遊び方パネルの制御
/// シーン間で永続化される（DontDestroyOnLoad）
/// TutorialCanvas ごと DontDestroyOnLoad するため、ReassignUIReferences 不要
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [Header("UI References")]
    public Button tutorialButton;   // 遊び方ボタン

    [Header("Panel1")]
    public GameObject tutorialPanel1;
    public Button closeButton1;     // Panel1 の × ボタン
    public Button nextButton;       // Panel1 の → ボタン

    [Header("Panel2")]
    public GameObject tutorialPanel2;
    public Button closeButton2;     // Panel2 の × ボタン
    public Button prevButton;       // Panel2 の ← ボタン

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初期状態：パネルを非表示
        HideAllPanels();

        // ボタンイベントを登録
        RegisterButtonEvents();
    }

    private void RegisterButtonEvents()
    {
        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(ShowPanel1);
        }

        if (closeButton1 != null)
        {
            closeButton1.onClick.RemoveAllListeners();
            closeButton1.onClick.AddListener(HideAllPanels);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ShowPanel2);
        }

        if (closeButton2 != null)
        {
            closeButton2.onClick.RemoveAllListeners();
            closeButton2.onClick.AddListener(HideAllPanels);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(ShowPanel1);
        }
    }

    /// <summary>
    /// 遊び方パネル1を表示する
    /// </summary>
    public void ShowPanel1()
    {
        if (tutorialPanel1 != null) tutorialPanel1.SetActive(true);
        if (tutorialPanel2 != null) tutorialPanel2.SetActive(false);
    }

    /// <summary>
    /// 遊び方パネル2を表示する
    /// </summary>
    public void ShowPanel2()
    {
        if (tutorialPanel1 != null) tutorialPanel1.SetActive(false);
        if (tutorialPanel2 != null) tutorialPanel2.SetActive(true);
    }

    /// <summary>
    /// すべての遊び方パネルを非表示にする
    /// </summary>
    public void HideAllPanels()
    {
        if (tutorialPanel1 != null) tutorialPanel1.SetActive(false);
        if (tutorialPanel2 != null) tutorialPanel2.SetActive(false);
    }
}
