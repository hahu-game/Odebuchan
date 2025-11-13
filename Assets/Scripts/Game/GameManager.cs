using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// Force recompile

/// <summary>
/// ゲーム全体の進行を管理する（ホストのみがロジックを実行）。
/// 2つのUyopyonStateオブジェクトをPlayerRefで管理する。
/// </summary>
public class GameManager : NetworkBehaviour
{
    // シングルトンパターン（シーン内に1つ）
    public static GameManager Instance { get; private set; }

    public NetworkObject uyopyonPrefab; // UnityエディタでUyopyonStateプリハブをアサイン
    public NetworkObject playerActionDataPrefab; // UnityエディタでPlayerActionDataプリハブをアサイン

    // TODO: フェーズ2で正式実装予定 - GameParametersへの参照
    public GameParameters gameParams;

    // === プレイヤーごとの生成座標 ===
    [Header("Uyopyon生成座標設定")]
    [Tooltip("プレイヤー1（自分）の生成座標 - 各クライアントでローカル表示に使用")]
    public Vector3 player1SpawnPosition = new Vector3(-3, 0, 0);

    [Tooltip("プレイヤー2（相手）の生成座標 - 各クライアントでローカル表示に使用")]
    public Vector3 player2SpawnPosition = new Vector3(3, 0, 0);

    // プレイヤーIDと対応するUyopyonStateの参照を保持
    private Dictionary<PlayerRef, UyopyonState> _playerStates = new Dictionary<PlayerRef, UyopyonState>();

    // プレイヤーIDと対応するPlayerActionDataの参照を保持
    private Dictionary<PlayerRef, PlayerActionData> _playerActionData = new Dictionary<PlayerRef, PlayerActionData>();

    /// <summary>
    /// 全プレイヤーのUyopyonStateを取得するプロパティ
    /// </summary>
    public Dictionary<PlayerRef, UyopyonState> uyopyonStateDict => _playerStates;

    /// <summary>
    /// 全プレイヤーのPlayerActionDataを取得するプロパティ
    /// </summary>
    public Dictionary<PlayerRef, PlayerActionData> playerActionDataDict => _playerActionData;

    [Networked]
    private int CurrentRound { get; set; } = 0;

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

    /// <summary>
    /// NetworkPlayer.Spawned()から呼ばれる。ホスト側でのみ実行。
    /// 各プレイヤーに対応するうーぴょんオブジェクトを生成する。
    /// </summary>
    //[Server] // ホストでのみ実行することを保証
    public void SpawnUyopyon(PlayerRef player, string playerName)
    {
        Debug.Log($"[GameManager.SpawnUyopyon] 呼び出されました: player={player}, playerName='{playerName}'");

        if (_playerStates.ContainsKey(player))
        {
            Debug.LogWarning($"[GameManager.SpawnUyopyon] Player={player}のUyopyonは既に存在します。スキップします。");
            return;
        }

        // 初期スポーン位置は原点（各クライアントでRender()が適切な位置に調整する）
        Vector3 spawnPosition = Vector3.zero;

        // プレイヤーのInput Authorityを指定して、うーぴょんオブジェクトを生成
        NetworkObject newUyopyon = Runner.Spawn(
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
        if (_playerActionData.ContainsKey(player)) return;

        NetworkObject newActionData = Runner.Spawn(
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
}
