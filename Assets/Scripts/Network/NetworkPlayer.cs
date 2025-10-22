using Fusion;
using UnityEngine;

/// <summary>
/// ネットワーク上で参加プレイヤーを表現するオブジェクト。
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    private PlayerRef MyPlayerRef;
    private string _lastPlayerName; // 前回の名前を保持

    public override void Spawned()
    {
        MyPlayerRef = Object.InputAuthority;

        if (Object.HasInputAuthority)
        {
            string localName = PlayerPrefs.GetString(TitleScreenManager.PLAYER_NAME_KEY, "ゲストうーぴょん");
            PlayerName = localName;
        }

        if (Runner.IsServer)
        {
            GameManager.Instance.SpawnUyopyon(MyPlayerRef, PlayerName.ToString());
        }

        // 初期値を設定
        _lastPlayerName = PlayerName.ToString();
    }

    /// <summary>
    /// 全クライアントで実行されるレンダリング処理。データの変更をチェックする。
    /// </summary>
    public override void Render()
    {
        string currentName = PlayerName.ToString();

        // 名前が変更された場合
        if (_lastPlayerName != currentName)
        {
            UpdatePlayerNameUI(currentName);
            _lastPlayerName = currentName; // 値を更新
        }
    }

    /// <summary>
    /// UIControllerに通知して名前を更新する（Render()から呼ばれる）
    /// </summary>
    private void UpdatePlayerNameUI(string newName)
    {
        if (Runner != null && UIController.Instance != null)
        {
            if (Runner.LocalPlayer == Object.InputAuthority)
            {
                UIController.Instance.UpdateMyName(newName);
            }
            else
            {
                UIController.Instance.UpdateOpponentName(newName);
            }
        }
    }
}