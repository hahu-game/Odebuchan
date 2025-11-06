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
            _playerStates.Add(player, state); // 辞書に追加

            Debug.Log($"[GameManager] Uyopyon spawned for Player {player} ({playerName}). Total: {_playerStates.Count}");
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
    /// PlayerActionDataを辞書に登録する（全クライアントで呼び出される）
    /// PlayerActionData.Spawned()から呼ばれる
    /// </summary>
    public void RegisterPlayerActionData(PlayerActionData actionData)
    {
        if (actionData.OwnerPlayer != PlayerRef.None && !_playerActionData.ContainsKey(actionData.OwnerPlayer))
        {
            _playerActionData.Add(actionData.OwnerPlayer, actionData);
            Debug.Log($"[GameManager] PlayerActionDataを登録: Player {actionData.OwnerPlayer}");
        }
    }


    /// <summary>
    /// 指定されたプレイヤーのUyopyonStateを取得
    /// </summary>
    public UyopyonState GetUyopyonState(PlayerRef player)
    {
        if (_playerStates.ContainsKey(player))
        {
            return _playerStates[player];
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
}
