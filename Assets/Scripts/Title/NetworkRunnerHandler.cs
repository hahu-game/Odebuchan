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

    private void Awake()
    {
        // 既存の NetworkRunnerHandler を探して破棄する
        var existingHandlers = FindObjectsOfType<NetworkRunnerHandler>();
        foreach (var handler in existingHandlers)
        {
            if (handler != this)
            {
                Debug.LogWarning($"[NetworkRunnerHandler] 既存のNetworkRunnerHandlerを発見したため破棄します: {handler.gameObject.name}");

                // 既存の Runner をシャットダウン
                if (handler._runner != null && handler._runner.IsRunning)
                {
                    Debug.Log("[NetworkRunnerHandler] 既存のRunnerをシャットダウンします。");
                    handler._runner.Shutdown();
                }

                Destroy(handler.gameObject);
            }
        }

        // シーン遷移時にこのGameObjectが破壊されないようにする
        DontDestroyOnLoad(gameObject);
        Debug.Log("[NetworkRunnerHandler] DontDestroyOnLoadを設定しました。");
    }

    // 【テスト用】Playボタンで自動起動するロジック
    // 【重要】現在は無効化しています。ランダムマッチングのテストは手動でボタンを押してください。
    // private void Start()
    // {
    //     if (Application.isEditor && !_started)
    //     {
    //         // ParallelSyncのクローンかどうかを判定
    //         // クローンの場合はプロジェクトパスに "_clone_" が含まれる
    //         string projectPath = Application.dataPath;
    //         bool isClone = projectPath.Contains("_clone_");
    //
    //         if (isClone)
    //         {
    //             Debug.Log("【テストモード】ParallelSyncクローン側のため、自動起動をスキップします。手動でランダムマッチボタンを押してください。");
    //         }
    //         else
    //         {
    //             Debug.Log("【テストモード】メインエディタでランダムマッチを自動起動します。");
    //             // StartGameはasyncなので、タスクとして実行
    //             // null を渡すことでランダムマッチングモードになる
    //             _ = StartGame(Fusion.GameMode.Shared, null);
    //         }
    //     }
    // }
    // 【テスト終了後】本番に戻す際は上記 Start() メソッドを削除すること

    /// <summary>
    /// ネットワーク接続を開始する。(async Task に変更)
    /// sessionName が null または空文字の場合はランダムマッチング、それ以外はフレンドマッチング
    /// </summary>
    public async Task StartGame(GameMode mode, string sessionName)
    {
        Debug.Log($"[StartGame] ===== 開始 =====");
        Debug.Log($"[StartGame] GameObject: {gameObject.name}");
        Debug.Log($"[StartGame] sessionName: '{sessionName ?? "null"}'");
        Debug.Log($"[StartGame] IsRandomMatch: {string.IsNullOrEmpty(sessionName)}");

        // シーン内の全ての NetworkRunner を検索
        var allRunners = FindObjectsOfType<NetworkRunner>();
        Debug.Log($"[StartGame] シーン内に {allRunners.Length} 個のNetworkRunnerが見つかりました。");

        // 実行中のRunnerをシャットダウン
        foreach (var runner in allRunners)
        {
            if (runner != null && runner.IsRunning)
            {
                Debug.Log($"[StartGame] NetworkRunnerをシャットダウンします: {runner.gameObject.name}");
                await runner.Shutdown();
            }
        }

        // このGameObjectのNetworkRunnerコンポーネントを取得または追加
        _runner = gameObject.GetComponent<NetworkRunner>();
        if (_runner == null)
        {
            Debug.Log("[StartGame] NetworkRunnerコンポーネントを新規作成します。");
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        else
        {
            Debug.Log("[StartGame] 既存のNetworkRunnerコンポーネントを再利用します。");
        }

        _runner.ProvideInput = true;
        _started = false;  // フラグをリセット
        _started = true;

        Debug.Log($"[StartGame] NetworkRunnerの準備完了。GameObject: {gameObject.name}");

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();

        // NetworkSceneManagerDefaultの存在確認
        if (sceneManager == null)
        {
            Debug.LogError("[StartGame] CRITICAL ERROR: NetworkSceneManagerDefaultが見つかりません！シーン遷移ができません。");
            Debug.LogError("[StartGame] このGameObjectにNetworkSceneManagerDefaultコンポーネントを追加してください。");
            _started = false;
            TitleScreenManager.Instance?.HideMatchingUI();
            return;
        }
        else
        {
            Debug.Log("[StartGame] NetworkSceneManagerDefaultを取得しました。");
        }

        // ランダムマッチかフレンドマッチかを判定
        bool isRandomMatch = string.IsNullOrEmpty(sessionName);

        var startGameArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = isRandomMatch ? null : sessionName,  // ランダムマッチの場合はnull
            SceneManager = sceneManager,
            PlayerCount = 2,
        };

        Debug.Log($"[StartGame] StartGameArgs作成完了: SessionName={startGameArgs.SessionName ?? "null"}, GameMode={startGameArgs.GameMode}");

        // ログ出力
        if (isRandomMatch)
        {
            Debug.Log("[StartGame] ランダムマッチモードで接続を開始します。SessionName=null（自動マッチング）");
        }
        else
        {
            Debug.Log($"[StartGame] フレンドマッチモードで接続を開始します。SessionName={sessionName}");
        }

        Debug.Log($"[StartGame] GameMode={mode}, IsRandomMatch={isRandomMatch}");

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            // 【重要メモ】接続が成功しても即座にシーン遷移しない
            // ホストはマッチング完了(OnPlayerJoined)までタイトル画面に留まる

            // マッチング開始SE再生
            AudioManager.Instance?.PlayMatchingStartSE();
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
        Debug.Log($"=== プレイヤーが参加しました: PlayerRef={player} ===");
        Debug.Log($"現在のプレイヤー数: {runner.ActivePlayers.Count()}人");
        Debug.Log($"IsSharedModeMasterClient: {runner.IsSharedModeMasterClient}");
        Debug.Log($"SessionInfo.Name: {runner.SessionInfo.Name}");

        // 【重要】2人目のプレイヤーが参加したら、シーン遷移を開始する
        if (runner.IsSharedModeMasterClient && runner.ActivePlayers.Count() == 2)
        {
            Debug.Log("=== マッチング完了！2人目が参加しました。ゲームシーンへ遷移します。 ===");

            // マッチング成功SE再生
            AudioManager.Instance?.PlayMatchingSuccessSE();

            // タイトル画面のUIを非表示にする
            TitleScreenManager.Instance?.HideMatchingUI();

            // WebGL接続を安定するまで1秒間待機する (非同期実行のためブロックしない)
            _ = DelayedSceneLoad(runner);
        }
        else
        {
            Debug.Log($"まだ1人しかいないため、2人目のプレイヤーを待機中...");
        }
    }

    // 【新規追加メソッド】
    private async Task DelayedSceneLoad(NetworkRunner runner)
    {
        try
        {
            Debug.Log("[DelayedSceneLoad] シーン遷移を開始します。");
            // WebGL環境でTask.Delayが動作しないため、待機を削除し即座にシーンロードを実行
            Debug.Log("[DelayedSceneLoad] シーンロードを実行します。");

            if (runner == null)
            {
                Debug.LogError("[DelayedSceneLoad] エラー: runnerがnullです！");
                return;
            }

            Debug.Log($"[DelayedSceneLoad] runner.IsRunning: {runner.IsRunning}");
            Debug.Log($"[DelayedSceneLoad] runner.IsSharedModeMasterClient: {runner.IsSharedModeMasterClient}");

            const string GAME_SCENE_NAME = "GameScene";
            Debug.Log($"[DelayedSceneLoad] runner.LoadScene(\"{GAME_SCENE_NAME}\")を呼び出します...");
            Debug.Log($"[DelayedSceneLoad] 現在のアクティブシーン: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            // シーンロードを開始
            // 注意: Fusion 2.0のLoadSceneはvoidを返す可能性があります
            runner.LoadScene(GAME_SCENE_NAME);

            Debug.Log($"[DelayedSceneLoad] LoadScene呼び出し完了。シーン遷移はコールバックで処理されます。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DelayedSceneLoad] シーンロード中に例外が発生しました: {ex.GetType().Name}");
            Debug.LogError($"[DelayedSceneLoad] メッセージ: {ex.Message}");
            Debug.LogError($"[DelayedSceneLoad] スタックトレース: {ex.StackTrace}");
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

        // シャットダウン時にタイトル画面のUIを復元
        // （キャンセルボタンやネットワークエラーでシャットダウンした場合）
        if (TitleScreenManager.Instance != null)
        {
            Debug.Log("[OnShutdown] タイトル画面のUIを復元します");
            TitleScreenManager.Instance.HideMatchingUI();
        }
    }

    // 以前は無かったメソッドの追加
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("=== サーバーに接続しました ===");
        Debug.Log($"Runner GameObject: {runner.gameObject.name}");
        Debug.Log($"このHandlerのRunner: {(_runner == runner ? "一致" : "不一致！")}");
        Debug.Log($"SessionInfo.Name: {runner.SessionInfo.Name}");
        Debug.Log($"SessionInfo.PlayerCount: {runner.SessionInfo.PlayerCount}");
        Debug.Log($"SessionInfo.MaxPlayers: {runner.SessionInfo.MaxPlayers}");
        Debug.Log($"SessionInfo.IsOpen: {runner.SessionInfo.IsOpen}");
        Debug.Log($"SessionInfo.IsVisible: {runner.SessionInfo.IsVisible}");
        Debug.Log($"Runner.IsServer: {runner.IsServer}");
        Debug.Log($"Runner.IsClient: {runner.IsClient}");
        Debug.Log($"Runner.IsSharedModeMasterClient: {runner.IsSharedModeMasterClient}");
    }

    // 以前は無かったメソッドの追加
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"サーバーから切断されました: {reason}");

        // GameFlowManagerが存在し、ゲーム終了フラグが立っている場合は何もしない
        // （UIController等から意図的にシャットダウンされた場合）
        var gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        if (gameFlowManager != null && gameFlowManager.IsGameEnded)
        {
            Debug.Log("[NetworkRunnerHandler] ゲーム終了フラグが立っているため、タイトル遷移をスキップします");
            return;
        }

        // GameScene中の切断の場合、タイトル画面に戻る
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentSceneName == "GameScene")
        {
            Debug.Log("[NetworkRunnerHandler] GameScene中の切断を検出。タイトル画面に戻ります");
            // 0.5秒待機してからタイトルに戻る
            StartCoroutine(ReturnToTitleAfterDisconnect());
        }
    }

    /// <summary>
    /// 切断後にタイトル画面に戻るコルーチン
    /// </summary>
    private System.Collections.IEnumerator ReturnToTitleAfterDisconnect()
    {
        yield return new WaitForSeconds(0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
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

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"=== セッションリスト更新: {sessionList.Count}個のセッション ===");
        foreach (var session in sessionList)
        {
            Debug.Log($"  - Session: {session.Name}, Players: {session.PlayerCount}/{session.MaxPlayers}, IsOpen: {session.IsOpen}, IsVisible: {session.IsVisible}");
        }
    }

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
                        var p = player; // ← うーぴょんオブジェクトが重なって表示されるのを防ぐための変数退避。

                        var networkPlayerObj = runner.Spawn(
                            playerPrefab,
                            Vector3.zero,
                            Quaternion.identity,
                            p,
                            // OnBeforeSpawned コールバックでOwnerPlayerRefを事前に設定
                            (runner, obj) =>
                            {
                                if (obj.TryGetBehaviour<NetworkPlayer>(out var np))
                                {
                                    np.OwnerPlayerRef = p;
                                    Debug.Log($"[NetworkRunnerHandler] OnBeforeSpawned: OwnerPlayerRef={p} を設定");
                                }
                            }
                        );

                        // Runner.GetPlayerObject()で取得できるようにSetPlayerObjectを呼ぶ
                        runner.SetPlayerObject(p, networkPlayerObj);

                        Debug.Log($"[NetworkRunnerHandler] NetworkPlayerをスポーンしました: PlayerRef={p}");
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
