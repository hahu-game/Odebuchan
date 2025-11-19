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

    /// <summary>
    /// Uyopyonをスポーン済みかどうか（全クライアントで同期される）
    /// </summary>
    [Networked]
    public NetworkBool HasSpawnedUyopyon { get; set; }

    private PlayerRef MyPlayerRef;
    private string _lastPlayerName; // 前回の名前を保持
    private bool _hasSetPlayerName = false; // PlayerNameを設定済みかどうか

    public override void Spawned()
    {
        // OwnerPlayerRefが設定されている場合はそれを使用、未設定の場合はInputAuthorityを使用
        MyPlayerRef = OwnerPlayerRef != PlayerRef.None ? OwnerPlayerRef : Object.InputAuthority;
        DebugLogger.Log($"[NetworkPlayer.Spawned] GameObject='{gameObject.name}', OwnerPlayerRef={OwnerPlayerRef}, InputAuthority={Object.InputAuthority}, MyPlayerRef={MyPlayerRef}");

        // 初期値を設定
        _lastPlayerName = PlayerName.ToString();

        // スポーン時に既にPlayerNameが設定されている場合（ネットワーク同期で受信した場合）、UIを更新
        if (!string.IsNullOrEmpty(_lastPlayerName))
        {
            DebugLogger.Log($"[NetworkPlayer.Spawned] 初期PlayerNameでUI更新: '{_lastPlayerName}' (OwnerPlayerRef={OwnerPlayerRef})");
            UpdatePlayerNameUI(_lastPlayerName);
        }

        // GameManagerに登録（FindObjectsByType削減のため）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNetworkPlayer(this);
        }

        DebugLogger.Log($"[NetworkPlayer] Spawned: Player={MyPlayerRef}");
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
            DebugLogger.Log($"[NetworkPlayer] RPC_SetPlayerName: Player={MyPlayerRef}, PlayerName='{playerName}'を設定しました");
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
            DebugLogger.Log($"[NetworkPlayer.Render] OwnerPlayerRef={OwnerPlayerRef}, LocalPlayer={Runner.LocalPlayer}, isLocalPlayer={isLocalPlayer}");

            if (isLocalPlayer)
            {
                string playerPrefsKey = TitleScreenManager.GetPlayerNameKey();
                string localName = PlayerPrefs.GetString(playerPrefsKey, "guest");
                DebugLogger.Log($"[NetworkPlayer.Render] PlayerPrefsから読み込んだ名前: key='{playerPrefsKey}', name='{localName}'");

                // StateAuthorityがある場合は直接設定、ない場合はRPCでホストに設定してもらう
                if (Object.HasStateAuthority)
                {
                    PlayerName = localName;
                    DebugLogger.Log($"[NetworkPlayer.Render] StateAuthorityがあるので直接設定: PlayerName='{localName}'");
                }
                else
                {
                    DebugLogger.Log($"[NetworkPlayer.Render] StateAuthorityがないのでRPCで設定: '{localName}'");
                    RPC_SetPlayerName(localName);
                }
            }
            else
            {
                DebugLogger.Log($"[NetworkPlayer.Render] 相手のNetworkPlayerなので、PlayerName設定はスキップ（ネットワーク同期待ち）");
            }

            _hasSetPlayerName = true;
        }

        string currentName = PlayerName.ToString();

        // ホスト側でまだUyopyonをスポーンしていない場合、PlayerNameが設定されたらスポーンする
        // HasSpawnedUyopyonはネットワーク同期されるので、全クライアントで重複スポーンを防げる
        if (Runner.IsSharedModeMasterClient && !HasSpawnedUyopyon)
        {
            if (!string.IsNullOrEmpty(currentName))
            {
                DebugLogger.Log($"[NetworkPlayer.Render] '{gameObject.name}' Spawning Uyopyon: OwnerPlayerRef={OwnerPlayerRef}, MyPlayerRef={MyPlayerRef}, Name='{currentName}'");
                GameManager.Instance.SpawnUyopyon(MyPlayerRef, currentName);
                HasSpawnedUyopyon = true;
                DebugLogger.Log($"[NetworkPlayer.Render] HasSpawnedUyopyonをtrueに設定（ネットワーク同期されます）");
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
    /// Despawn時にGameManagerから登録解除
    /// </summary>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterNetworkPlayer(OwnerPlayerRef);
        }
    }

    /// <summary>
    /// UIControllerに通知して名前を更新する(Render()から呼ばれる)
    /// </summary>
    private void UpdatePlayerNameUI(string newName)
    {
        if (Runner != null && UIController.Instance != null)
        {
            DebugLogger.Log($"[NetworkPlayer] UpdatePlayerNameUI: OwnerPlayerRef={OwnerPlayerRef}, LocalPlayer={Runner.LocalPlayer}, InputAuthority={Object.InputAuthority}, Name='{newName}'");

            // OwnerPlayerRefで判定する（InputAuthorityは同期されない場合がある）
            if (Runner.LocalPlayer == OwnerPlayerRef)
            {
                DebugLogger.Log($"[NetworkPlayer] 自分の名前を更新: {newName}");
                UIController.Instance.UpdateMyName(newName);
            }
            else
            {
                DebugLogger.Log($"[NetworkPlayer] 相手の名前を更新: {newName}");
                UIController.Instance.UpdateOpponentName(newName);
            }
        }
    }
}
