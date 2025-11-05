using Fusion;
using UnityEngine;

/// <summary>
/// ネットワーク上で参加プレイヤーを表現するオブジェクト。
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    /// <summary>
    /// このNetworkPlayerの所有者となるPlayerRef
    /// NetworkRunnerHandlerでスポーン後に設定される
    /// </summary>
    [Networked]
    public PlayerRef OwnerPlayerRef { get; set; }

    private PlayerRef MyPlayerRef;
    private string _lastPlayerName; // 前回の名前を保持
    private bool _hasSpawnedUyopyon = false; // Uyopyonをスポーン済みかどうか
    private bool _hasSetPlayerName = false; // PlayerNameを設定済みかどうか

    public override void Spawned()
    {
        // OwnerPlayerRefが設定されている場合はそれを使用、未設定の場合はInputAuthorityを使用
        MyPlayerRef = OwnerPlayerRef != PlayerRef.None ? OwnerPlayerRef : Object.InputAuthority;

        // 初期値を設定
        _lastPlayerName = PlayerName.ToString();

        Debug.Log($"[NetworkPlayer] Spawned: Player={MyPlayerRef}");
    }

    /// <summary>
    /// クライアント側からホストにPlayerNameの設定を要求するRPC
    /// InputAuthorityが設定されていない場合でも送信できるよう、RpcSources.Allを使用
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string playerName, RpcInfo info = default)
    {
        // セキュリティチェック: 送信元が自分のNetworkPlayerの場合のみ許可
        if (info.Source == OwnerPlayerRef)
        {
            PlayerName = playerName;
            Debug.Log($"[NetworkPlayer] RPC_SetPlayerName: Player={MyPlayerRef}, PlayerName='{playerName}'を設定しました");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 不正なRPC: Player={info.Source}が Player={OwnerPlayerRef}のPlayerNameを変更しようとしました");
        }
    }

    /// <summary>
    /// 全クライアントで実行されるレンダリング処理。データの変更をチェックする。
    /// </summary>
    public override void Render()
    {
        // 【追加】初回のみ、PlayerNameを設定する
        if (!_hasSetPlayerName)
        {
            bool isLocalPlayer = (Runner.LocalPlayer == OwnerPlayerRef);

            if (isLocalPlayer)
            {
                string localName = PlayerPrefs.GetString(TitleScreenManager.GetPlayerNameKey(), "guest");

                // StateAuthorityがある場合は直接設定、ない場合はRPCでホストに設定してもらう
                if (Object.HasStateAuthority)
                {
                    PlayerName = localName;
                }
                else
                {
                    RPC_SetPlayerName(localName);
                }
            }

            _hasSetPlayerName = true;
        }

        string currentName = PlayerName.ToString();

        // ホスト側でまだUyopyonをスポーンしていない場合、PlayerNameが設定されたらスポーンする
        if (Runner.IsSharedModeMasterClient && !_hasSpawnedUyopyon)
        {
            if (!string.IsNullOrEmpty(currentName))
            {
                Debug.Log($"[NetworkPlayer] Spawning Uyopyon for Player={MyPlayerRef}, Name='{currentName}'");
                GameManager.Instance.SpawnUyopyon(MyPlayerRef, currentName);
                _hasSpawnedUyopyon = true;
            }
        }

        // 名前が変更された場合
        if (_lastPlayerName != currentName)
        {
            UpdatePlayerNameUI(currentName);
            _lastPlayerName = currentName; // 値を更新
        }
    }

    /// <summary>
    /// UIControllerに通知して名前を更新する(Render()から呼ばれる)
    /// </summary>
    private void UpdatePlayerNameUI(string newName)
    {
        if (Runner != null && UIController.Instance != null)
        {
            Debug.Log($"[NetworkPlayer] UpdatePlayerNameUI: OwnerPlayerRef={OwnerPlayerRef}, LocalPlayer={Runner.LocalPlayer}, InputAuthority={Object.InputAuthority}, Name='{newName}'");

            // OwnerPlayerRefで判定する（InputAuthorityは同期されない場合がある）
            if (Runner.LocalPlayer == OwnerPlayerRef)
            {
                Debug.Log($"[NetworkPlayer] 自分の名前を更新: {newName}");
                UIController.Instance.UpdateMyName(newName);
            }
            else
            {
                Debug.Log($"[NetworkPlayer] 相手の名前を更新: {newName}");
                UIController.Instance.UpdateOpponentName(newName);
            }
        }
    }
}
