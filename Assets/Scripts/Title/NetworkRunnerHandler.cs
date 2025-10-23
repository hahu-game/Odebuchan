using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // ActivePlayers.Count()を使うために必要

/// <summary>
/// Photon Fusionの接続開始、切断、イベントコールバックを処理する。
/// INetworkRunnerCallbacksのメソッドは明示的なインターフェイス実装として定義し、Unity警告を回避する。
/// </summary>
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private bool _started = false;
    public NetworkObject playerPrefab;

    // 【テスト用】Playボタンで自動起動するロジック
    private void Start()
    {
        if (Application.isEditor && !_started)
        {
            Debug.Log("【テストモード】ホストとして自動起動します。");
            // StartGameがasyncなので、タスクとして実行
            _ = StartGame(Fusion.GameMode.Shared,"RANDOM_POOL_UYOPYON");
        }
    }
    // 【テスト完了後】本番に戻す際は上記の Start() メソッドを削除すること

    /// <summary>
    /// ネットワーク接続を開始する。（async Task に変更）
    /// </summary>
    public async Task StartGame(GameMode mode, string sessionName)
    {
        if (_started) return;
        _started = true;

        _runner = GetComponent<NetworkRunner>() ?? gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = sceneManager,
            PlayerCount = 2,
        };

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            // 【重要修正】接続成功しても即座にシーン遷移しない
            // ホストはマッチング完了（OnPlayerJoined）までタイトル画面に留まる
        }
        else
        {
            Debug.LogError($"Fusion接続失敗: {result.ShutdownReason}");
            _started = false;
            // 失敗時、TitleScreenManagerに通知してUIを戻す
            TitleScreenManager.Instance?.HideMatchingUI();
        }
    }

    /// <summary>
    /// 接続をシャットダウンし、TitleScreenManagerから呼ばれる
    /// </summary>
    public void ShutdownRunner()
    {
        if (_runner != null && _started)
        {
            _runner.Shutdown();
            _started = false;
        }
        // UIの非表示はTitleScreenManager側で行う
    }

    // ========== INetworkRunnerCallbacks の Fusion 2.0.7 完全な実装 ==========

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // 1. NetworkPlayerのスポーン（ホスト/サーバーでのみ実行）
        if (runner.IsSharedModeMasterClient && playerPrefab != null)
        {
            runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        }

        // 2. 【重要修正】2人目のプレイヤー参加を検知し、シーン遷移を開始する
        if (runner.IsSharedModeMasterClient && runner.ActivePlayers.Count() == 2)
        {
            Debug.Log("マッチング完了！2人目が参加しました。ゲームシーンへ遷移します。");

            // タイトル画面のUIを非表示にする
            TitleScreenManager.Instance?.HideMatchingUI();

            // WebGL接続が安定するまで1秒間待機する (非同期実行のためブロックしない)
            _ = DelayedSceneLoad(runner);
        }
    }

    // 【新規追加メソッド】
    private async Task DelayedSceneLoad(NetworkRunner runner)
    {
        // WebGLクライアントとの接続が完全に安定するまで、1000ミリ秒 (1秒) 待機
        await Task.Delay(1000);

        if (runner != null)
        {
            const string GAME_SCENE_NAME = "GameScene";
            // シーンロードを非同期で開始
            await runner.LoadScene(GAME_SCENE_NAME);
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

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.IsServer)
        {
            if (runner.ActivePlayers.Count() == 2)
            {
                Debug.Log("P2 (クライアント) がゲームシーンへのロードを完了し、ゲームに参加しました！");
            }
            else if (runner.ActivePlayers.Count() == 1)
            {
                Debug.Log("ホスト自身のゲームシーンロードが完了しました。");
            }
        }
    }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}