using System.Collections;
using UnityEngine;

/// <summary>
/// ActionPanel にアタッチして、行動のジャンケンの手（グー・チョキ・パー）画像を切り替えるコンポーネント
/// </summary>
public class ActionPanelJankenDisplay : MonoBehaviour
{
    [SerializeField] private GameObject rockImage;
    [SerializeField] private GameObject scissorsImage;
    [SerializeField] private GameObject paperImage;

    private const float WinAnimDuration = 1f;
    private const float WinAnimMaxScale = 1.5f;

    private Coroutine _winAnimCoroutine;

    private void Awake()
    {
        SetGenre(Genre.None);
    }

    /// <summary>
    /// Genre に応じた画像を活性化し、他を非活性にする
    /// </summary>
    public void SetGenre(Genre genre)
    {
        if (rockImage != null)     rockImage.SetActive(genre == Genre.Rock);
        if (scissorsImage != null) scissorsImage.SetActive(genre == Genre.Scissors);
        if (paperImage != null)    paperImage.SetActive(genre == Genre.Paper);
    }

    /// <summary>
    /// 勝利アニメーション: sin 関数で 1.5 倍まで拡大し元に戻る（2秒）
    /// </summary>
    public void PlayWinAnimation()
    {
        if (_winAnimCoroutine != null)
            StopCoroutine(_winAnimCoroutine);
        _winAnimCoroutine = StartCoroutine(WinAnimCoroutine());
    }

    private IEnumerator WinAnimCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < WinAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / WinAnimDuration;
            float scale = 1f + (WinAnimMaxScale - 1f) * Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
        _winAnimCoroutine = null;
    }
}
