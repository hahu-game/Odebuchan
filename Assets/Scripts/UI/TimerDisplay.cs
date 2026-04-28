using UnityEngine;
using TMPro;

/// <summary>
/// 選択フェーズのタイマーを表示するUIコンポーネント（シンプルテキスト版）
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.92f, 0.23f); // 黄
    [SerializeField] private Color dangerColor = new Color(0.96f, 0.26f, 0.21f); // 赤

    [Header("Animation Settings")]
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float animationSpeed = 0.5f;

    private int _lastDisplayedTime = -1;
    private bool _warningPlayed = false;
    private bool _isPulsing = false;
    private float _animationTime = 0f;

    private void Awake()
    {
        if (timerText == null)
        {
            Debug.LogError("[TimerDisplay] timerText が null です。Inspectorで設定してください。");
        }
    }

    /// <summary>
    /// タイマー表示を更新
    /// </summary>
    /// <param name="remainingTime">残り時間（秒）</param>
    /// <param name="maxTime">最大時間（秒）- 未使用だが互換性のため保持</param>
    public void UpdateTimer(int remainingTime, int maxTime = 60)
    {
        if (remainingTime < 0) remainingTime = 0;

        if (timerText == null)
        {
            Debug.LogError("[TimerDisplay] UpdateTimer: timerText が null です");
            return;
        }

        // テキスト更新
        if (remainingTime > 0)
        {
            timerText.text = $"残り {remainingTime} 秒";
        }
        else
        {
            timerText.text = "時間切れ！";
        }

        // 色とアニメーション更新（1秒に1回のみ）
        if (_lastDisplayedTime != remainingTime)
        {
            _lastDisplayedTime = remainingTime;
            UpdateVisuals(remainingTime);
        }
    }

    /// <summary>
    /// 色とアニメーションを更新
    /// </summary>
    private void UpdateVisuals(int remainingTime)
    {
        // アニメーション停止
        StopAnimations();

        // 色変更
        if (remainingTime > 30)
        {
            // 通常（白）
            timerText.color = normalColor;
        }
        else if (remainingTime > 10)
        {
            // 警告（黄）
            timerText.color = warningColor;

            // パルスアニメーション開始
            if (enablePulseAnimation)
            {
                StartPulseAnimation();
            }
        }
        else if (remainingTime > 0)
        {
            // 危険（赤）
            timerText.color = dangerColor;

            // パルスアニメーション開始
            if (enablePulseAnimation)
            {
                StartPulseAnimation();
            }

            // 警告音再生（1回のみ）
            if (!_warningPlayed)
            {
                AudioManager.Instance?.PlayTimerWarningSE();
                _warningPlayed = true;
            }
        }
        else
        {
            // 時間切れ（灰色）
            timerText.color = Color.gray;

            // タイムアップ音再生
            AudioManager.Instance?.PlayTimeUpSE();
        }
    }

    /// <summary>
    /// パルスアニメーション（拡大縮小）
    /// </summary>
    private void StartPulseAnimation()
    {
        _isPulsing = true;
        _animationTime = 0f;
    }

    /// <summary>
    /// アニメーション停止
    /// </summary>
    private void StopAnimations()
    {
        _isPulsing = false;
        _animationTime = 0f;

        if (timerText != null)
        {
            timerText.transform.localScale = Vector3.one;
            timerText.alpha = 1f;
        }
    }

    /// <summary>
    /// タイマーパネルを表示/非表示
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        _warningPlayed = false;
        _lastDisplayedTime = -1;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        StopAnimations();
    }

    private void Update()
    {
        // アニメーション更新
        if (_isPulsing)
        {
            _animationTime += Time.deltaTime;

            // パルスアニメーション（拡大縮小）
            float scale = 1f + Mathf.Sin(_animationTime / animationSpeed * Mathf.PI) * (pulseScale - 1f);
            timerText.transform.localScale = Vector3.one * scale;
        }
    }

    private void OnDestroy()
    {
        StopAnimations();
    }
}
