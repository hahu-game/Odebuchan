using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Photon Fusionの接続開始、切断、イベントコールバックを処理する。
/// INetworkRunnerCallbacksを実装し、Fusionのイベントを受け取る。
/// </summary>
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private bool _started = false;

    // NOTE: 2.0.7ではNetworkSceneManagerDefaultから取得できない場合があるため、
    // NetworkPlayerプレハブをインスペクターで直接アサインすることを推奨します。
    public NetworkObject playerPrefab;

    /// <summary>
    /// ネットワーク接続を開始する。TitleScreenManagerから呼び出される。
    /// </summary>
    public async Task StartGame(GameMode mode, string sessionName)
    {
        if (_started) return;
        _started = true;

        _runner = GetComponent<NetworkRunner>() ?? gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // NetworkSceneManagerDefaultがアタッチされていることを確認
        var sceneManager = GetComponent<NetworkSceneManagerDefault>();

        // StartGameArgsの設定
        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = sceneManager,
            PlayerCount = 2,
            // 2.0.7ではSceneプロパティにはSceneRef（またはnull）を設定する必要があり、
            // intのビルドインデックスを渡す必要がないため、ここではコメントアウトします。
            // シーンロードは接続成功後に明示的に行います。
            // Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), 
        };

        // 接続処理を開始
        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            // 接続成功。ホスト（P1）はゲームシーンへの遷移を処理する
            if (_runner.IsSharedModeMasterClient)
            {
                // NOTE: シーン名を直接渡します。UnityのBuild Settingsに登録されたシーン名に置き換えてください。
                const string GAME_SCENE_NAME = "GameScene";
                // LoadSceneはSceneRefまたはシーン名を引数に取ります
                await _runner.LoadScene(GAME_SCENE_NAME);
            }
        }
        else
        {
            Debug.LogError($"Fusion接続失敗: {result.ShutdownReason}");
            _started = false;
        }
    }

    // ========== INetworkRunnerCallbacks の Fusion 2.0.7 完全な実装 ==========

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"プレイヤーが参加しました: PlayerRef={player}");

        // ホスト（P1）は、参加したプレイヤーに対応するNetworkPlayerオブジェクトをスポーンする
        if (runner.IsSharedModeMasterClient && playerPrefab != null)
        {
            runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"プレイヤーが退出しました: PlayerRef={player}");
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        // PlayerInputControllerで処理するため、ここでは何もしない
    }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"シャットダウン: {shutdownReason}");
        _started = false;
    }

    // 警告が出ていたメソッドの修正
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("サーバーに接続しました");
    }

    // 警告が出ていたメソッドの修正
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"サーバーから切断されました: {reason}");
    }

    // 以下のメソッドもすべて明示的な実装に変更します
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"接続失敗: {reason}");
    }

    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}