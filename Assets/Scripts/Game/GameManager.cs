using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// Force recompile

/// <summary>
/// ゲーム全体の進行を管理する（ホストのみがロジックを実行）。
/// 2つのUyopyonStateオブジェクトをPlayerRefで管理する。
/// </summary>
public class GameManager : MonoBehaviour
{
    // シングルトンパターン（シーン内に1つ）
    public static GameManager Instance { get; private set; }

    public NetworkObject uyopyonPrefab; // UnityエディタでUyopyonStateプリハブをアサイン
    public NetworkObject playerActionDataPrefab; // UnityエディタでPlayerActionDataプリハブをアサイン

    // TODO: フェーズ2で正式実装予定 - GameParametersへの参照
    public GameParameters gameParams;

    // NetworkRunnerへの参照
    private NetworkRunner _runner;

    // === プレイヤーごとの生成座標 ===
    [Header("Uyopyon生成座標設定")]
    [Tooltip("プレイヤー1（自分）の生成座標 - 各クライアントでローカル表示に使用")]
    public Vector3 player1SpawnPosition = new Vector3(-3, 1, 0);

    [Tooltip("プレイヤー2（相手）の生成座標 - 各クライアントでローカル表示に使用")]
    public Vector3 player2SpawnPosition = new Vector3(4, 1, 0);

    // プレイヤーIDと対応するUyopyonStateの参照を保持
    private Dictionary<PlayerRef, UyopyonState> _playerStates = new Dictionary<PlayerRef, UyopyonState>();

    // プレイヤーIDと対応するPlayerActionDataの参照を保持
    private Dictionary<PlayerRef, PlayerActionData> _playerActionData = new Dictionary<PlayerRef, PlayerActionData>();

    // 投了フラグを管理（各プレイヤーごと）
    private Dictionary<PlayerRef, bool> _surrenderFlags = new Dictionary<PlayerRef, bool>();

    // NetworkPlayerの参照を保持するDictionary
    private Dictionary<PlayerRef, NetworkPlayer> _networkPlayers = new Dictionary<PlayerRef, NetworkPlayer>();
    public IReadOnlyDictionary<PlayerRef, NetworkPlayer> networkPlayerDict => _networkPlayers;

    /// <summary>
    /// 全プレイヤーのUyopyonStateを取得するプロパティ
    /// </summary>
    public Dictionary<PlayerRef, UyopyonState> uyopyonStateDict => _playerStates;

    /// <summary>
    /// 全プレイヤーのPlayerActionDataを取得するプロパティ
    /// </summary>
    public Dictionary<PlayerRef, PlayerActionData> playerActionDataDict => _playerActionData;

    private int CurrentRound = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // シーンを跨ぐ場合は、DontDestroyOnLoad(gameObject); を追加
        }

        // スポーン位置の確認
        Debug.Log($"[GameManager] player1SpawnPosition: {player1SpawnPosition}");
        Debug.Log($"[GameManager] player2SpawnPosition: {player2SpawnPosition}");
    }

    private void Start()
    {
        // NetworkRunnerを取得
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("[GameManager] NetworkRunnerが見つかりません");
        }
        else
        {
            Debug.Log("[GameManager] NetworkRunnerを取得しました");
        }
    }

    /// <summary>
    /// NetworkPlayer.Spawned()から呼ばれる。ホスト側でのみ実行。
    /// 各プレイヤーに対応するうーぴょんオブジェクトを生成する。
    /// </summary>
    //[Server] // ホストでのみ実行することを保証
    public void SpawnUyopyon(PlayerRef player, string playerName)
    {
        Debug.Log($"[GameManager.SpawnUyopyon] 呼び出されました: player={player}, playerName='{playerName}'");

        // Runnerのnullチェック（nullの場合は取得を試みる）
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[GameManager.SpawnUyopyon] Runnerがnullです。NetworkRunnerが見つかりません。");
                return;
            }
        }

        // uyopyonPrefabのnullチェック
        if (uyopyonPrefab == null)
        {
            Debug.LogError("[GameManager.SpawnUyopyon] uyopyonPrefabがnullです。Inspectorで設定してください。");
            return;
        }

        if (_playerStates.ContainsKey(player))
        {
            Debug.LogWarning($"[GameManager.SpawnUyopyon] Player={player}のUyopyonは既に存在します。スキップします。");
            return;
        }

        // 初期スポーン位置は原点（各クライアントでRender()が適切な位置に調整する）
        Vector3 spawnPosition = Vector3.zero;

        // プレイヤーのInput Authorityを指定して、うーぴょんオブジェクトを生成
        NetworkObject newUyopyon = _runner.Spawn(
            uyopyonPrefab,
            position: spawnPosition,
            rotation: Quaternion.identity,
            inputAuthority: player, // 入力権限をこのプレイヤーに渡す
            // OnBeforeSpawned コールバックでOwnerPlayerを事前に設定
            onBeforeSpawned: (runner, obj) =>
            {
                if (obj.TryGetBehaviour<UyopyonState>(out var s))
                {
                    s.OwnerPlayer = player;
                    Debug.Log($"[GameManager.SpawnUyopyon] onBeforeSpawned: OwnerPlayerを設定 player={player}");
                }
            }
        );

        if (newUyopyon.TryGetBehaviour<UyopyonState>(out var state))
        {
            // 辞書への追加は UyopyonState.Spawned() で自動的に行われる
            Debug.Log($"[GameManager] Uyopyon spawned for Player {player} ({playerName})");
        }

        // PlayerActionDataもスポーン
        SpawnPlayerActionData(player, playerName);
    }

    /// <summary>
    /// 各プレイヤーに対応するPlayerActionDataオブジェクトを生成する。
    /// ホスト側でのみ実行。
    /// </summary>
    public void SpawnPlayerActionData(PlayerRef player, string playerName)
    {
        // Runnerのnullチェック（nullの場合は取得を試みる）
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[GameManager.SpawnPlayerActionData] Runnerがnullです。NetworkRunnerが見つかりません。");
                return;
            }
        }

        // playerActionDataPrefabのnullチェック
        if (playerActionDataPrefab == null)
        {
            Debug.LogError("[GameManager.SpawnPlayerActionData] playerActionDataPrefabがnullです。Inspectorで設定してください。");
            return;
        }

        if (_playerActionData.ContainsKey(player)) return;

        NetworkObject newActionData = _runner.Spawn(
            playerActionDataPrefab,
            position: Vector3.zero,
            rotation: Quaternion.identity,
            inputAuthority: player,
            // OnBeforeSpawned コールバックでOwnerPlayerを事前に設定
            onBeforeSpawned: (runner, obj) =>
            {
                if (obj.TryGetBehaviour<PlayerActionData>(out var a))
                {
                    a.OwnerPlayer = player;
                }
            }
        );

        Debug.Log($"[GameManager] PlayerActionData spawned for Player {player} ({playerName})");
    }


    /// <summary>
    /// 指定されたプレイヤーのUyopyonStateを取得
    /// </summary>
    public UyopyonState GetUyopyonState(PlayerRef player)
    {
        Debug.Log($"[GameManager] GetUyopyonState: player={player}, _playerStates.Count={_playerStates.Count}");

        if (_playerStates.ContainsKey(player))
        {
            Debug.Log($"[GameManager] Player {player} の UyopyonState が見つかりました");
            return _playerStates[player];
        }

        Debug.LogWarning($"[GameManager] Player {player} の UyopyonState が見つかりません。登録されているプレイヤー:");
        foreach (var kvp in _playerStates)
        {
            Debug.LogWarning($"  - {kvp.Key}: {kvp.Value.name}");
        }

        return null;
    }

    /// <summary>
    /// 指定されたプレイヤーのPlayerActionDataを取得
    /// </summary>
    public PlayerActionData GetPlayerActionData(PlayerRef player)
    {
        if (_playerActionData.ContainsKey(player))
        {
            return _playerActionData[player];
        }
        return null;
    }

    /// <summary>
    /// UyopyonStateを辞書に登録（全クライアントで実行）
    /// UyopyonState.Spawned() から呼び出される
    /// </summary>
    public void RegisterUyopyonState(UyopyonState state)
    {
        if (state == null)
        {
            Debug.LogWarning("[GameManager] RegisterUyopyonState: state が null です");
            return;
        }

        PlayerRef player = state.OwnerPlayer;

        // 既に登録されている場合はスキップ
        if (_playerStates.ContainsKey(player))
        {
            Debug.LogWarning($"[GameManager] Player {player} の UyopyonState は既に登録されています");
            return;
        }

        _playerStates.Add(player, state);
        Debug.Log($"[GameManager] Player {player} の UyopyonState を登録しました。Total: {_playerStates.Count}");
    }

    /// <summary>
    /// PlayerActionDataを辞書に登録（全クライアントで実行）
    /// PlayerActionData.Spawned() から呼び出される
    /// </summary>
    public void RegisterPlayerActionData(PlayerActionData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[GameManager] RegisterPlayerActionData: data が null です");
            return;
        }

        PlayerRef player = data.OwnerPlayer;

        // 既に登録されている場合はスキップ
        if (_playerActionData.ContainsKey(player))
        {
            Debug.LogWarning($"[GameManager] Player {player} の PlayerActionData は既に登録されています");
            return;
        }

        _playerActionData.Add(player, data);
        Debug.Log($"[GameManager] Player {player} の PlayerActionData を登録しました。Total: {_playerActionData.Count}");
    }


    /// <summary>
    /// NetworkPlayerをDictionaryに登録する
    /// </summary>
    public void RegisterNetworkPlayer(NetworkPlayer networkPlayer)
    {
        if (networkPlayer == null)
        {
            Debug.LogWarning("[GameManager] NetworkPlayerがnullです");
            return;
        }

        // OwnerPlayerRefを使用（Object.InputAuthorityはクライアント側で[Player:None]になる可能性があるため）
        PlayerRef owner = networkPlayer.OwnerPlayerRef;
        if (_networkPlayers.ContainsKey(owner))
        {
            Debug.LogWarning($"[GameManager] NetworkPlayer already registered for player {owner}");
            return;
        }

        _networkPlayers[owner] = networkPlayer;
        Debug.Log($"[GameManager] NetworkPlayer registered for player {owner}, total: {_networkPlayers.Count}");
    }

    /// <summary>
    /// PlayerRefからNetworkPlayerを取得する
    /// </summary>
    public NetworkPlayer GetNetworkPlayer(PlayerRef player)
    {
        if (_networkPlayers.TryGetValue(player, out NetworkPlayer networkPlayer))
        {
            return networkPlayer;
        }
        return null;
    }

    /// <summary>
    /// PlayerRefからプレイヤー名を取得する
    /// </summary>
    public string GetPlayerName(PlayerRef player)
    {
        // 方法1: Dictionaryから取得
        NetworkPlayer networkPlayer = GetNetworkPlayer(player);
        if (networkPlayer != null)
        {
            string name = networkPlayer.PlayerName.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        // 方法2: Runner.GetPlayerObjectから取得（フォールバック）
        if (_runner != null)
        {
            var playerObj = _runner.GetPlayerObject(player);
            if (playerObj != null && playerObj.TryGetBehaviour<NetworkPlayer>(out var np))
            {
                string name = np.PlayerName.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    // Dictionaryにも登録
                    if (!_networkPlayers.ContainsKey(player))
                    {
                        _networkPlayers[player] = np;
                        Debug.Log($"[GameManager] GetPlayerName: フォールバックでNetworkPlayerを登録 player={player}");
                    }
                    return name;
                }
            }
        }

        // フォールバック: PlayerIDを表示
        Debug.LogWarning($"[GameManager] Player {player} の名前が取得できませんでした");
        return $"Player {player.PlayerId}";
    }

    // === 9.2: 投了機能 ===

    /// <summary>
    /// 投了フラグを設定（GameFlowManagerから呼ばれる）
    /// </summary>
    public void SetSurrenderFlag(PlayerRef player)
    {
        _surrenderFlags[player] = true;
        Debug.Log($"[GameManager] Player {player} の投了フラグを設定しました");
    }

    /// <summary>
    /// 指定プレイヤーが投了したかチェック
    /// </summary>
    public bool HasSurrendered(PlayerRef player)
    {
        return _surrenderFlags.ContainsKey(player) && _surrenderFlags[player];
    }

    /// <summary>
    /// 新しいゲーム開始時に投了フラグをリセット
    /// </summary>
    public void ResetSurrenderFlags()
    {
        _surrenderFlags.Clear();
        Debug.Log("[GameManager] 投了フラグをリセットしました");
    }


    /// <summary>
    /// 全てのDictionaryをクリアする（メモリリーク防止）
    /// </summary>
    public void ClearAllDictionaries()
    {
        _playerStates.Clear();
        _playerActionData.Clear();
        _networkPlayers.Clear();
        _surrenderFlags.Clear();
        Debug.Log("[GameManager] 全てのDictionaryをクリアしました");
    }

    /// <summary>
    /// UyopyonStateをDictionaryから削除する
    /// </summary>
    public void UnregisterUyopyonState(PlayerRef player)
    {
        if (_playerStates.ContainsKey(player))
        {
            _playerStates.Remove(player);
            Debug.Log($"[GameManager] UyopyonState unregistered for player {player}");
        }
    }

    /// <summary>
    /// PlayerActionDataをDictionaryから削除する
    /// </summary>
    public void UnregisterPlayerActionData(PlayerRef player)
    {
        if (_playerActionData.ContainsKey(player))
        {
            _playerActionData.Remove(player);
            Debug.Log($"[GameManager] PlayerActionData unregistered for player {player}");
        }
    }

    /// <summary>
    /// NetworkPlayerをDictionaryから削除する
    /// </summary>
    public void UnregisterNetworkPlayer(PlayerRef player)
    {
        if (_networkPlayers.ContainsKey(player))
        {
            _networkPlayers.Remove(player);
            Debug.Log($"[GameManager] NetworkPlayer unregistered for player {player}");
        }
    }

    private void OnDestroy()
    {
        // シングルトンがこのインスタンスの場合のみクリア
        if (Instance == this)
        {
            ClearAllDictionaries();
            Instance = null;
        }
    }
}
