using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲーム全体の進行を管理する（ホストのみがロジックを実行）。
/// 2つのUyopyonStateオブジェクトをPlayerRefで管理する。
/// </summary>
public class GameManager : NetworkBehaviour
{
    // シングルトンパターン（シーン内に1つ）
    public static GameManager Instance { get; private set; }

    public NetworkObject uyopyonPrefab; // UnityエディタでUyopyonStateプリハブをアサイン

    // TODO: フェーズ2で正式実装予定 - GameParametersへの参照
    public GameParameters gameParams;

    // プレイヤーIDと対応するUyopyonStateの参照を保持
    private Dictionary<PlayerRef, UyopyonState> _playerStates = new Dictionary<PlayerRef, UyopyonState>();

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
            inputAuthority: player // 入力権限をこのプレイヤーに渡す
        );

        if (newUyopyon.TryGetBehaviour<UyopyonState>(out var state))
        {
            state.OwnerPlayer = player;
            _playerStates.Add(player, state); // 辞書に追加

            Debug.Log($"Uyopyon spawned for Player {player}. Total: {_playerStates.Count}");
        }
    }


}
