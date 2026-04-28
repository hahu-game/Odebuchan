using UnityEngine;

/// <summary>
/// ボタンにアタッチしてクリック音を再生するコンポーネント
/// AudioManager.Instanceを直接参照するため、シーン間の参照問題が発生しない
/// </summary>
public class UIButtonSound : MonoBehaviour
{
    /// <summary>
    /// 一般的なボタンクリック音を再生
    /// UnityのButton.OnClick()から呼び出す
    /// </summary>
    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }
        else
        {
            Debug.LogWarning("[UIButtonSound] AudioManager.Instance が null です");
        }
    }

    /// <summary>
    /// クリアボタンのクリック音を再生
    /// UnityのButton.OnClick()から呼び出す
    /// </summary>
    public void PlayClearSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayActionClearSE();
        }
        else
        {
            Debug.LogWarning("[UIButtonSound] AudioManager.Instance が null です");
        }
    }
}
