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
    }

    /// <summary>
    /// NetworkPlayer.Spawned()から呼ばれる。ホスト側でのみ実行。
    /// 各プレイヤーに対応するうーぴょんオブジェクトを生成する。
    /// </summary>
    //[Server] // ホストでのみ実行することを保証
    public void SpawnUyopyon(PlayerRef player, string playerName)
    {
        if (_playerStates.ContainsKey(player)) return;

        // プレイヤーのInput Authorityを指定して、うーぴょんオブジェクトを生成
        NetworkObject newUyopyon = Runner.Spawn(
            uyopyonPrefab,
            position: Vector3.zero + new Vector3(player.PlayerId * 3, 0, 0), // P1とP2で位置をずらす
            rotation: Quaternion.identity,
            inputAuthority: player, // 入力権限をこのプレイヤーに渡す
            // OnBeforeSpawned コールバックでOwnerPlayerを事前に設定
            onBeforeSpawned: (runner, obj) =>
            {
                if (obj.TryGetBehaviour<UyopyonState>(out var s))
                {
                    s.OwnerPlayer = player;
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
            inputAuthority: player
        );

        if (newActionData.TryGetBehaviour<PlayerActionData>(out var actionData))
        {
            actionData.OwnerPlayer = player;
            _playerActionData.Add(player, actionData);

            Debug.Log($"[GameManager] PlayerActionData spawned for Player {player} ({playerName}). Total: {_playerActionData.Count}");
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
