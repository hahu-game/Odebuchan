using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // ActivePlayers.Count()を使うために必要

/// <summary>
/// Photon Fusionの接続開始、切断、イベントコールバックを管理する。
/// INetworkRunnerCallbacksのメソッドは明示的なインターフェース実装として定義し、Unity経由で呼ばれる。
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
            // StartGameはasyncなので、タスクとして実行
            _ = StartGame(Fusion.GameMode.Shared,"RANDOM_POOL_UYOPYON");
        }
    }
    // 【テスト終了後】本番に戻す際は上記 Start() メソッドを削除すること

    /// <summary>
    /// ネットワーク接続を開始する。(async Task に変更)
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
            // 【重要メモ】接続が成功しても即座にシーン遷移しない
            // ホストはマッチング完了(OnPlayerJoined)までタイトル画面に留まる
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
        Debug.Log($"プレイヤーが参加しました: PlayerRef={player}");

        // 【重要】2人目のプレイヤーが参加したら、シーン遷移を開始する
        if (runner.IsSharedModeMasterClient && runner.ActivePlayers.Count() == 2)
        {
            Debug.Log("マッチング完了！2人目が参加しました。ゲームシーンへ遷移します。");

            // タイトル画面のUIを非表示にする
            TitleScreenManager.Instance?.HideMatchingUI();

            // WebGL接続を安定するまで1秒間待機する (非同期実行のためブロックしない)
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

    // 以前は無かったメソッドの追加
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("サーバーに接続しました");
    }

    // 以前は無かったメソッドの追加
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"サーバーから切断されました: {reason}");
    }

    // 以下のメソッドはすべて明示的な実装に変更します
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
        Debug.Log($"[NetworkRunnerHandler] OnSceneLoadDone呼び出し。IsSharedModeMasterClient={runner.IsSharedModeMasterClient}");

        if (runner.IsSharedModeMasterClient)
        {
            Debug.Log($"OnSceneLoadDone: シーンロード完了。アクティブプレイヤー数={runner.ActivePlayers.Count()}");

            // GameSceneに遷移した後、全プレイヤーのNetworkPlayerをスポーン
            string currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[NetworkRunnerHandler] 現在のシーン: {currentSceneName}");

            if (currentSceneName == "GameScene")
            {
                Debug.Log($"[NetworkRunnerHandler] GameSceneでNetworkPlayerをスポーンします。");
                foreach (var player in runner.ActivePlayers)
                {
                    if (playerPrefab != null)
                    {
                        var networkPlayerObj = runner.Spawn(
                            playerPrefab,
                            Vector3.zero,
                            Quaternion.identity,
                            player,
                            // OnBeforeSpawned コールバックでOwnerPlayerRefを事前に設定
                            (runner, obj) =>
                            {
                                if (obj.TryGetBehaviour<NetworkPlayer>(out var np))
                                {
                                    np.OwnerPlayerRef = player;
                                    Debug.Log($"[NetworkRunnerHandler] OnBeforeSpawned: OwnerPlayerRef={player} を設定");
                                }
                            }
                        );

                        Debug.Log($"[NetworkRunnerHandler] NetworkPlayerをスポーンしました: PlayerRef={player}");
                    }
                    else
                    {
                        Debug.LogError("[NetworkRunnerHandler] playerPrefabがnullです！");
                    }
                }
            }
            else
            {
                Debug.Log($"[NetworkRunnerHandler] GameScene以外のシーンのため、スキップします。");
            }
        }
        else
        {
            Debug.Log($"[NetworkRunnerHandler] クライアントのため、スポーン処理をスキップします。");
        }
    }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
