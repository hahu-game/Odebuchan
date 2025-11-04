using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// ゲーム全体のフェーズ管理を行うクラス
/// ホスト側でフェーズを管理し、全クライアントに同期する
/// </summary>
public class GameFlowManager : NetworkBehaviour
{
    // シングルトンパターン
    public static GameFlowManager Instance { get; private set; }

    // GameParametersへの参照（Inspectorで設定）
    public GameParameters gameParams;

    // TurnProcessorへの参照（Inspectorで設定または自動取得）
    public TurnProcessor turnProcessor;

    // === ネットワーク同期プロパティ ===

    /// <summary>
    /// 現在の日数
    /// </summary>
    [Networked]
    public int CurrentDay { get; set; } = 1;

    /// <summary>
    /// 現在のフェーズ
    /// </summary>
    [Networked]
    public GamePhase CurrentPhase { get; set; } = GamePhase.Preparation;

    /// <summary>
    /// ゲーム開始時にランダム選出された特殊能力（3つ）
    /// </summary>
    [Networked, Capacity(3)]
    public NetworkArray<SpecialAbilityType> AvailableSpecialAbilities { get; }

    /// <summary>
    /// 選択フェーズの残り時間（秒）
    /// </summary>
    [Networked]
    public int SelectionTimeRemaining { get; set; } = 0;

    /// <summary>
    /// ゲーム終了フラグ
    /// </summary>
    [Networked]
    public bool IsGameEnded { get; set; } = false;

    /// <summary>
    /// 勝者（ゲーム終了時に設定）
    /// </summary>
    [Networked]
    public PlayerRef Winner { get; set; }

    // === ローカル変数 ===
    private bool _gameStarted = false;

    private void Awake()
    {
        // シングルトンパターンの実装
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void Spawned()
    {
        Debug.Log($"[GameFlowManager] Spawned called. HasStateAuthority={Object.HasStateAuthority}, _gameStarted={_gameStarted}");

        // TurnProcessorの自動取得（Inspectorで設定されていない場合）
        if (turnProcessor == null)
        {
            turnProcessor = GetComponent<TurnProcessor>();
            if (turnProcessor == null)
            {
                Debug.LogError("TurnProcessor が見つかりません。GameFlowManager と同じ GameObject にアタッチしてください。");
            }
        }

        // ホストのみがゲーム開始処理を実行
        if (Object.HasStateAuthority && !_gameStarted)
        {
            _gameStarted = true;
            Debug.Log("[GameFlowManager] ホストとしてゲーム開始処理を開始します");
            _ = StartGame();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 5秒ごとに現在のステータスをログ出力（デバッグ用）
        // Fusion 2.0では、デフォルトのTickRateは60
        if (Object.HasStateAuthority && Runner.Tick % (60 * 5) == 0)
        {
            Debug.Log($"[GameFlowManager][定期チェック] Day={CurrentDay}, Phase={CurrentPhase}, Object.IsValid={Object.IsValid}");
        }
    }

    /// <summary>
    /// ゲーム開始処理
    /// </summary>
    private async Task StartGame()
    {
        Debug.Log("[GameFlowManager] StartGame() 開始");
        Debug.Log($"[GameFlowManager] Object={Object}, IsValid={Object?.IsValid}, HasStateAuthority={Object?.HasStateAuthority}");

        // 明示的な初期化（再開・リトライ時の堅牢性向上）
        CurrentDay = 1;
        CurrentPhase = GamePhase.Preparation;

        // 特殊能力6種から3種をランダム選出
        SelectRandomSpecialAbilities();

        // ブラックアウト「ゲームスタート！」を表示
        RPC_ShowBlackout("ゲームスタート！");

        // 1秒待機
        Debug.Log($"[GameFlowManager] ブラックアウト待機開始: {gameParams.BlackoutDuration}秒");
        await Task.Delay((int)(gameParams.BlackoutDuration * 1000));
        Debug.Log("[GameFlowManager] ブラックアウト待機終了");

        // ログエリアに「ゲームスタート！」を追加
        RPC_AddLog("ゲームスタート！");

        // 準備フェーズへ移行
        Debug.Log("[GameFlowManager] StartGame() 完了、準備フェーズへ移行します");
        await StartPreparationPhase();
    }

    /// <summary>
    /// 特殊能力をランダムに3つ選出
    /// </summary>
    private void SelectRandomSpecialAbilities()
    {
        // 全6種の特殊能力をリストアップ
        List<SpecialAbilityType> allAbilities = new List<SpecialAbilityType>
        {
            SpecialAbilityType.Gaishoku,
            SpecialAbilityType.Kintre,
            SpecialAbilityType.Gamushara,
            SpecialAbilityType.Benkyou,
            SpecialAbilityType.Jukusui,
            SpecialAbilityType.Dokagui
        };

        // ランダムに3つ選出
        List<SpecialAbilityType> selected = new List<SpecialAbilityType>();
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, allAbilities.Count);
            selected.Add(allAbilities[randomIndex]);
            allAbilities.RemoveAt(randomIndex);
        }

        // NetworkArrayに設定
        for (int i = 0; i < 3; i++)
        {
            AvailableSpecialAbilities.Set(i, selected[i]);
        }

        Debug.Log($"選出された特殊能力: {selected[0]}, {selected[1]}, {selected[2]}");
    }

    /// <summary>
    /// 準備フェーズ開始（スタブ、後で実装）
    /// </summary>
    private async Task StartPreparationPhase()
    {
        try
        {
            Debug.Log($"[GameFlowManager] ========== StartPreparationPhase() 開始 ==========");
            Debug.Log($"[GameFlowManager] CurrentDay={CurrentDay}, CurrentPhase={CurrentPhase}");
            Debug.Log($"[GameFlowManager] Object={Object}, IsValid={Object?.IsValid}, HasStateAuthority={Object?.HasStateAuthority}");

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 準備フェーズ開始前: Object無効 (Object={Object}, IsValid={Object?.IsValid})");
                return;
            }

            CurrentPhase = GamePhase.Preparation;
            Debug.Log($"[GameFlowManager] {CurrentDay}日目の準備フェーズを開始しました (CurrentPhase={CurrentPhase})");

            // この段階では、次のフェーズへの遷移のみ実装
            Debug.Log("[GameFlowManager] 準備フェーズ: 1秒待機開始");
            await Task.Delay(1000);
            Debug.Log("[GameFlowManager] 準備フェーズ: 1秒待機終了");

            Debug.Log($"[GameFlowManager] 待機後チェック: Object={Object}, IsValid={Object?.IsValid}");
            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 準備フェーズ待機後: Object無効 (Object={Object}, IsValid={Object?.IsValid})");
                return;
            }

            Debug.Log($"[GameFlowManager] 準備フェーズ終了、選択フェーズへ遷移します (CurrentDay={CurrentDay})");
            await StartSelectionPhase();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameFlowManager] 準備フェーズでエラー発生: {e.GetType().Name}");
            Debug.LogError($"[GameFlowManager] Message: {e.Message}");
            Debug.LogError($"[GameFlowManager] StackTrace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 選択フェーズ開始（スタブ、後で実装）
    /// </summary>
    private async Task StartSelectionPhase()
    {
        try
        {
            Debug.Log($"[GameFlowManager] ========== StartSelectionPhase() 開始 ==========");
            Debug.Log($"[GameFlowManager] CurrentDay={CurrentDay}, CurrentPhase={CurrentPhase}");
            Debug.Log($"[GameFlowManager] Object={Object}, IsValid={Object?.IsValid}");

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 選択フェーズ開始前: Object無効");
                return;
            }

            CurrentPhase = GamePhase.Selection;
            Debug.Log($"[GameFlowManager] 選択フェーズを開始しました (CurrentPhase={CurrentPhase})");

            // この段階では、次のフェーズへの遷移のみ実装
            Debug.Log("[GameFlowManager] 選択フェーズ: 3秒待機開始");
            await Task.Delay(3000);
            Debug.Log("[GameFlowManager] 選択フェーズ: 3秒待機終了");

            Debug.Log($"[GameFlowManager] 待機後チェック: Object={Object}, IsValid={Object?.IsValid}");
            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 選択フェーズ待機後: Object無効");
                return;
            }

            Debug.Log($"[GameFlowManager] 選択フェーズ終了、実行フェーズへ遷移します (CurrentDay={CurrentDay})");
            await StartExecutionPhase();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameFlowManager] 選択フェーズでエラー発生: {e.GetType().Name}");
            Debug.LogError($"[GameFlowManager] Message: {e.Message}");
            Debug.LogError($"[GameFlowManager] StackTrace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 実行フェーズ開始（スタブ、後で実装）
    /// </summary>
    private async Task StartExecutionPhase()
    {
        try
        {
            Debug.Log($"[GameFlowManager] ========== StartExecutionPhase() 開始 ==========");
            Debug.Log($"[GameFlowManager] CurrentDay={CurrentDay}, CurrentPhase={CurrentPhase}");
            Debug.Log($"[GameFlowManager] Object={Object}, IsValid={Object?.IsValid}");

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 実行フェーズ開始前: Object無効");
                return;
            }

            CurrentPhase = GamePhase.Execution;
            Debug.Log($"[GameFlowManager] 実行フェーズを開始しました (CurrentPhase={CurrentPhase})");

            // この段階では、次のフェーズへの遷移のみ実装
            Debug.Log("[GameFlowManager] 実行フェーズ: 3秒待機開始");
            await Task.Delay(3000);
            Debug.Log("[GameFlowManager] 実行フェーズ: 3秒待機終了");

            Debug.Log($"[GameFlowManager] 待機後チェック: Object={Object}, IsValid={Object?.IsValid}");
            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 実行フェーズ待機後: Object無効");
                return;
            }

            Debug.Log($"[GameFlowManager] 実行フェーズ終了、次の日へ (現在: {CurrentDay}日目)");

            // 次の日へ
            CurrentDay++;
            Debug.Log($"[GameFlowManager] CurrentDay を {CurrentDay} にインクリメントしました");
            Debug.Log($"[GameFlowManager] {CurrentDay}日目の準備フェーズへ遷移します");

            await StartPreparationPhase();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameFlowManager] 実行フェーズでエラー発生: {e.GetType().Name}");
            Debug.LogError($"[GameFlowManager] Message: {e.Message}");
            Debug.LogError($"[GameFlowManager] StackTrace: {e.StackTrace}");
        }
    }

    // === RPC（全クライアントへの通知） ===

    /// <summary>
    /// ブラックアウトパネルを表示する
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBlackout(string text)
    {
        Debug.Log($"[RPC] ブラックアウト表示: {text}");
        UIController.Instance?.ShowBlackout(text, gameParams.BlackoutDuration);
    }

    /// <summary>
    /// ログエリアにメッセージを追加する
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AddLog(string message)
    {
        Debug.Log($"[RPC] ログ追加: {message}");
        UIController.Instance?.AddLog(message);
    }
}
