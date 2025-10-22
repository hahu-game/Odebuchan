using UnityEngine;
using TMPro;

/// <summary>
/// タイトル画面のUI操作を管理し、ネットワーク接続を開始する。
/// ユーザーのボタン操作をトリガーとしてNetworkRunnerHandlerを呼び出す。
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    // === Inspectorで設定するフィールド ===
    public NetworkRunnerHandler runnerHandler; // NetworkRunnerHandlerへの参照
    public TMP_InputField playerNameInputField; // プレイヤー名入力欄
    public TMP_InputField sessionNameInputField; // フレンドマッチ用セッション名入力欄

    // プレイヤー名をローカル保存するためのキー
    public const string PLAYER_NAME_KEY = "UyopyonPlayerName";

    private void Awake()
    {
        // 前回入力した名前があれば表示しておく
        playerNameInputField.text = PlayerPrefs.GetString(PLAYER_NAME_KEY, "ゲストうーぴょん");
    }

    /// <summary>
    /// ランダムマッチボタンが押されたときに呼ばれる。
    /// </summary>
    public async void OnRandomMatchClicked() // async void に変更
    {
        SavePlayerName();
        // StartGameの戻り値がTaskになったため、await可能になる
        await runnerHandler.StartGame(Fusion.GameMode.Shared, "RANDOM_POOL_UYOPYON");
    }

    public async void OnFriendMatchClicked() // async void に変更
    {
        SavePlayerName();

        string sessionName = sessionNameInputField.text;

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            Debug.LogError("フレンドマッチには合言葉（セッション名）の入力が必要です。");
            return;
        }

        // StartGameの戻り値がTaskになったため、await可能になる
        await runnerHandler.StartGame(Fusion.GameMode.Shared, sessionName);
    }

    /// <summary>
    /// 入力されたプレイヤー名をローカルに保存する。
    /// </summary>
    private void SavePlayerName()
    {
        string playerName = playerNameInputField.text;
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "無名うーぴょん";
        }
        PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
        PlayerPrefs.Save();
    }
}