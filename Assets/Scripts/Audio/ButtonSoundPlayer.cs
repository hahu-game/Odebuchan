using UnityEngine;

/// <summary>
/// ボタンクリック時のSE再生を管理するクラス
/// UnityのInspectorでButtonのOnClick()イベントから呼び出すために使用
/// </summary>
public class ButtonSoundPlayer : MonoBehaviour
{
    [Header("Title Scene References")]
    [Tooltip("タイトル画面でフレンドマッチボタンの条件分岐SEに使用")]
    public TitleScreenManager titleScreenManager;

    /// <summary>
    /// 一般的なボタンクリック時のSEを再生
    /// (9_ButtonClick)
    ///
    /// 使用例：
    /// - たべる、ねむる、あそぶ、つういんボタン
    /// - 特殊能力ボタン（がいしょく、きんとれ、がむしゃら、べんきょう、じゅくすい、どかぐい）
    /// - 確定ボタン
    /// - タイトル画面のボタン
    /// - 設定画面のボタン
    /// </summary>
    public void PlayButtonClickSE()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }
        else
        {
            Debug.LogWarning("[ButtonSoundPlayer] AudioManager.Instance が null です");
        }
    }

    /// <summary>
    /// クリアボタンクリック時のSEを再生
    /// (10_ClearClick)
    ///
    /// 使用例：
    /// - 行動クリアボタン
    /// </summary>
    public void PlayClearButtonSE()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayActionClearSE();
        }
        else
        {
            Debug.LogWarning("[ButtonSoundPlayer] AudioManager.Instance が null です");
        }
    }

    /// <summary>
    /// フレンドマッチボタン専用：合言葉の入力状態に応じてSEを切り替える
    /// - 合言葉が入力されている場合: 9_ButtonClick
    /// - 合言葉が入力されていない場合: 10_ClearClick
    ///
    /// 使用例：
    /// - タイトル画面のフレンドマッチボタン
    /// </summary>
    public void PlayFriendMatchButtonSE()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[ButtonSoundPlayer] AudioManager.Instance が null です");
            return;
        }

        // TitleScreenManagerの参照をチェック
        if (titleScreenManager == null)
        {
            Debug.LogWarning("[ButtonSoundPlayer] titleScreenManager が null です。9_ButtonClick を再生します。");
            AudioManager.Instance.PlayButtonClickSE();
            return;
        }

        // sessionNameInputFieldの参照をチェック
        if (titleScreenManager.sessionNameInputField == null)
        {
            Debug.LogWarning("[ButtonSoundPlayer] sessionNameInputField が null です。9_ButtonClick を再生します。");
            AudioManager.Instance.PlayButtonClickSE();
            return;
        }

        // 合言葉の入力状態をチェック
        string sessionName = titleScreenManager.sessionNameInputField.text;

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            // 合言葉が入力されていない場合: 10_ClearClick
            Debug.Log("[ButtonSoundPlayer] 合言葉が未入力のため、10_ClearClick を再生します。");
            AudioManager.Instance.PlayActionClearSE();
        }
        else
        {
            // 合言葉が入力されている場合: 9_ButtonClick
            Debug.Log("[ButtonSoundPlayer] 合言葉が入力されているため、9_ButtonClick を再生します。");
            AudioManager.Instance.PlayButtonClickSE();
        }
    }
}
