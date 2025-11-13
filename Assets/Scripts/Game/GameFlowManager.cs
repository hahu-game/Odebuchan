using Fusion;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
    private bool _selectionPhaseActive = false;
    private int _selectionPhaseTickCounter = 0;

    // === 7.2で追加: 特殊能力選択完了フラグ ===
    private bool _abilitySelectionComplete = false;
    private SpecialAbilityType? _selectedAbilityFromClient = null;

    // === ゲーム全体で選択された特殊能力の記録 ===
    private List<SpecialAbilityType> _selectedAbilities = new List<SpecialAbilityType>();

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

        // TurnProcessorの取得
        turnProcessor = GetComponent<TurnProcessor>();
        if (turnProcessor == null)
        {
            Debug.LogError("[GameFlowManager] TurnProcessorが見つかりません！同じGameObjectにアタッチしてください。");
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
        // ホストのみが処理を実行
        if (!Object.HasStateAuthority)
            return;

        // ゲーム終了済みの場合は処理を行わない
        if (IsGameEnded)
            return;

        // 5秒ごとに現在のステータスをログ出力（デバッグ用）
        // Fusion 2.0では、デフォルトのTickRateは60
        if (Runner.Tick % (60 * 5) == 0)
        {
            Debug.Log($"[GameFlowManager][定期チェック] Day={CurrentDay}, Phase={CurrentPhase}, Object.IsValid={Object.IsValid}");
        }

        // 選択フェーズ中のタイマー処理
        if (CurrentPhase == GamePhase.Selection && _selectionPhaseActive)
        {
            _selectionPhaseTickCounter++;

            // 60ティック = 1秒（TickRate = 60の場合）
            if (_selectionPhaseTickCounter >= 60)
            {
                _selectionPhaseTickCounter = 0;
                SelectionTimeRemaining--;

                // 残り時間が10秒になったら警告ログ
                if (SelectionTimeRemaining == 10)
                {
                    RPC_AddLog("残り10秒です！");
                }
            }
        }
    }

    /// <summary>
    /// ゲーム開始処理
    /// </summary>
    private async UniTask StartGame()
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
        await UniTask.Delay((int)(gameParams.BlackoutDuration * 1000));
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
    /// 準備フェーズ開始
    /// </summary>
    private async UniTask StartPreparationPhase()
    {
        try
        {
            Debug.Log($"[GameFlowManager] ========== StartPreparationPhase() 開始 ==========");
            Debug.Log($"[GameFlowManager] {CurrentDay}日目の準備フェーズを開始します");

            // ゲーム終了チェック
            if (IsGameEnded)
            {
                Debug.Log("[GameFlowManager] ゲーム終了済みのため、準備フェーズをスキップします");
                return;
            }

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 準備フェーズ開始前: Object無効");
                return;
            }

            CurrentPhase = GamePhase.Preparation;

            // ブラックアウトで「〇日目」を表示
            RPC_ShowBlackout($"{CurrentDay}日目");

            // ブラックアウト表示時間待機
            Debug.Log($"[GameFlowManager] ブラックアウト待機: {gameParams.BlackoutDuration}秒");
            await UniTask.Delay((int)(gameParams.BlackoutDuration * 1000));

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 準備フェーズ待機後: Object無効");
                return;
            }

            // ログに日数を追加
            RPC_AddLog($"===== {CurrentDay}日目 =====");

            // 全プレイヤーの行動データをリセット（2日目以降）
            if (CurrentDay > 1)
            {
                ResetAllPlayerActions();
            }

            // 糖尿病チェック
            CheckDiabetes();

            // 熱中症による強制つういんチェック（前日午後に熱中症が発症した場合）
            CheckHeatstrokeMorningClinic();

            // 進化判定（7.1で実装）
            await CheckEvolution();

            // 選択フェーズへ遷移
            Debug.Log("[GameFlowManager] 準備フェーズ終了、選択フェーズへ遷移");
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
    /// 選択フェーズ開始
    /// </summary>
    private async UniTask StartSelectionPhase()
    {
        try
        {
            Debug.Log($"[GameFlowManager] ========== StartSelectionPhase() 開始 ==========");
            Debug.Log($"[GameFlowManager] 選択フェーズを開始します");

            // ゲーム終了チェック
            if (IsGameEnded)
            {
                Debug.Log("[GameFlowManager] ゲーム終了済みのため、選択フェーズをスキップします");
                return;
            }

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 選択フェーズ開始前: Object無効");
                return;
            }

            CurrentPhase = GamePhase.Selection;
            SelectionTimeRemaining = gameParams.SelectionPhaseTimeLimit; // 60秒
            _selectionPhaseTickCounter = 0; // ティックカウンターをリセット

            // UIの操作ロック解除
            RPC_SetActionButtonsInteractable(true);

            // 行動ボタンのOutlineを表示
            RPC_ShowActionButtonOutlines();

            // UIの行動選択状態をリセット（2日目以降で午前から選択できるように）
            RPC_ResetActionSelection();

            // ログに選択フェーズ開始を追加
            RPC_AddLog($"行動を選択してください（制限時間: {SelectionTimeRemaining}秒）");

            // タイマー開始フラグ
            _selectionPhaseActive = true;

            // 両プレイヤーが確定するか、タイムアウトまで待機
            await WaitForSelectionComplete();

            // 選択フェーズ終了
            _selectionPhaseActive = false;

            // Object状態を再確認
            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] WaitForSelectionComplete後: Object無効");
                return;
            }

            // 行動ボタンのOutlineを非表示
            RPC_HideActionButtonOutlines();

            // UIの操作をロック
            RPC_SetActionButtonsInteractable(false);

            // 実行フェーズへ遷移
            Debug.Log("[GameFlowManager] 選択フェーズ終了、実行フェーズへ遷移");
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
    /// 実行フェーズ開始
    /// </summary>
    private async UniTask StartExecutionPhase()
    {
        try
        {
            Debug.Log($"[GameFlowManager] ========== StartExecutionPhase() 開始 ==========");
            Debug.Log($"[GameFlowManager] 実行フェーズを開始します");

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 実行フェーズ開始前: Object無効");
                return;
            }

            CurrentPhase = GamePhase.Execution;

            // ブラックアウト「行動開始！」を表示
            RPC_ShowBlackout("行動開始！");
            await UniTask.Delay((int)(gameParams.BlackoutDuration * 1000));

            // ログに実行フェーズ開始を追加
            RPC_AddLog("===== 行動実行 =====");

            // 相手の選択を表示（2秒待機）
            RPC_RevealAllActions();
            await UniTask.Delay(2000);

            // PlayerActionDataの辞書を取得
            var playerActionDict = GameManager.Instance.playerActionDataDict;

            if (playerActionDict == null || playerActionDict.Count == 0)
            {
                Debug.LogError("[GameFlowManager] PlayerActionDataが取得できません");
                return;
            }

            Debug.Log($"[GameFlowManager] PlayerActionData取得完了: {playerActionDict.Count}人");

            // === 午前の行動実行 ===
            RPC_AddLog("--- 午前 ---");
            await UniTask.Delay(500);

            Debug.Log("[GameFlowManager] 午前の処理を開始");
            await turnProcessor.ProcessMorning(playerActionDict);
            Debug.Log("[GameFlowManager] 午前の処理が完了");
            await UniTask.Delay(2000); // アニメーション表示時間

            // 勝利判定（8.1で実装予定）
            // if (CheckVictory(out PlayerRef winner)) { ... }

            // ゲーム終了チェック
            if (IsGameEnded)
            {
                Debug.Log("[GameFlowManager] ゲーム終了済みのため、処理を中断します");
                return;
            }

            // === 午後の行動実行 ===
            RPC_AddLog("--- 午後 ---");
            await UniTask.Delay(500);

            Debug.Log("[GameFlowManager] 午後の処理を開始");
            await turnProcessor.ProcessAfternoon(playerActionDict);
            Debug.Log("[GameFlowManager] 午後の処理が完了");
            await UniTask.Delay(2000); // アニメーション表示時間

            // 勝利判定（8.1で実装予定）
            // if (CheckVictory(out PlayerRef winner)) { ... }

            // ゲーム終了チェック
            if (IsGameEnded)
            {
                Debug.Log("[GameFlowManager] ゲーム終了済みのため、処理を中断します");
                return;
            }

            if (!Object || !Object.IsValid)
            {
                Debug.LogError($"[GameFlowManager] 実行フェーズ待機後: Object無効");
                return;
            }

            // 次の日の準備フェーズへ遷移
            CurrentDay++;
            Debug.Log($"[GameFlowManager] {CurrentDay}日目へ移行します");
            await StartPreparationPhase();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameFlowManager] 実行フェーズでエラー発生: {e.GetType().Name}");
            Debug.LogError($"[GameFlowManager] Message: {e.Message}");
            Debug.LogError($"[GameFlowManager] StackTrace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 選択完了まで待機
    /// </summary>
    private async UniTask WaitForSelectionComplete()
    {
        while (_selectionPhaseActive)
        {
            // Objectの状態をチェック
            if (!Object || !Object.IsValid)
            {
                Debug.LogError("[GameFlowManager] WaitForSelectionComplete: Object無効");
                return;
            }

            // 両プレイヤーが確定したかチェック
            if (AreAllPlayersReady())
            {
                Debug.Log("[GameFlowManager] 全プレイヤーが確定しました");
                return;
            }

            // タイムアウトチェック
            if (SelectionTimeRemaining <= 0)
            {
                Debug.Log("[GameFlowManager] 選択時間が終了しました");
                // 未選択の行動を「ねむる」に設定
                SetDefaultActionsForUnreadyPlayers();
                return;
            }

            // 100ms待機（サーバー負荷軽減）
            await UniTask.Delay(100);
        }
    }

    /// <summary>
    /// 全プレイヤーが確定したかチェック
    /// </summary>
    private bool AreAllPlayersReady()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerActionDataDict == null)
        {
            return false;
        }

        foreach (var kvp in GameManager.Instance.playerActionDataDict)
        {
            if (!kvp.Value.IsActionFixed)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 未確定のプレイヤーの行動を「ねむる」に設定
    /// </summary>
    private void SetDefaultActionsForUnreadyPlayers()
    {
        // StateAuthorityチェック（このメソッドはホストでのみ実行されるべき）
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[GameFlowManager] StateAuthorityがないため、デフォルト行動を設定できません");
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.playerActionDataDict == null)
        {
            Debug.LogWarning("[GameFlowManager] GameManager または playerActionDataDict が null です");
            return;
        }

        Debug.Log($"[GameFlowManager] タイムアウト: 未確定プレイヤーにデフォルト行動を設定開始（プレイヤー数: {GameManager.Instance.playerActionDataDict.Count}）");

        foreach (var kvp in GameManager.Instance.playerActionDataDict)
        {
            Debug.Log($"[GameFlowManager] プレイヤー {kvp.Key} チェック: IsActionFixed={kvp.Value.IsActionFixed}");

            if (!kvp.Value.IsActionFixed)
            {
                Debug.Log($"[GameFlowManager] プレイヤー {kvp.Key} の行動をデフォルト（ねむる）に設定します");

                // 直接プロパティを変更（StateAuthorityがあるため）
                kvp.Value.MorningAction = ActionData.Default();
                kvp.Value.AfternoonAction = ActionData.Default();
                kvp.Value.IsActionFixed = true;

                Debug.Log($"[GameFlowManager] Player {kvp.Key} にデフォルト行動を設定完了: Morning={kvp.Value.MorningAction.Type}/{kvp.Value.MorningAction.Genre}, Afternoon={kvp.Value.AfternoonAction.Type}/{kvp.Value.AfternoonAction.Genre}, IsActionFixed={kvp.Value.IsActionFixed}");
            }
            else
            {
                Debug.Log($"[GameFlowManager] プレイヤー {kvp.Key} は既に確定済みのためスキップ");
            }
        }

        Debug.Log("[GameFlowManager] デフォルト行動設定処理完了");
    }

    /// <summary>
    /// 全プレイヤーの行動データをリセット（新しい日の開始時）
    /// </summary>
    private void ResetAllPlayerActions()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerActionDataDict == null)
        {
            Debug.LogWarning("[GameFlowManager] GameManager または playerActionDataDict が null です");
            return;
        }

        Debug.Log("[GameFlowManager] 全プレイヤーの行動データをリセットします");

        foreach (var kvp in GameManager.Instance.playerActionDataDict)
        {
            if (kvp.Value != null)
            {
                kvp.Value.ResetForNewDay();
            }
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

    /// <summary>
    /// UIの操作可否を設定
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetActionButtonsInteractable(bool interactable)
    {
        UIController.Instance?.SetActionButtonsInteractable(interactable);
        Debug.Log($"[RPC] 行動ボタンの操作: {(interactable ? "可能" : "不可")}");
    }

    /// <summary>
    /// 行動ボタンのOutlineを表示
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowActionButtonOutlines()
    {
        UIController.Instance?.ShowActionButtonOutlines();
        Debug.Log("[RPC] 行動ボタンのOutlineを表示");
    }

    /// <summary>
    /// 行動ボタンのOutlineを非表示
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideActionButtonOutlines()
    {
        UIController.Instance?.HideActionButtonOutlines();
        Debug.Log("[RPC] 行動ボタンのOutlineを非表示");
    }

    /// <summary>
    /// 実行フェーズ開始時に、全プレイヤーの行動を公開する
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RevealAllActions()
    {
        Debug.Log("[GameFlowManager] RPC_RevealAllActions が呼ばれました");

        if (GameManager.Instance == null || GameManager.Instance.playerActionDataDict == null)
        {
            Debug.LogWarning("[GameFlowManager] GameManager または playerActionDataDict が null です");
            return;
        }

        // すべてのプレイヤーの行動をUIに表示
        foreach (var kvp in GameManager.Instance.playerActionDataDict)
        {
            var playerRef = kvp.Key;
            var actionData = kvp.Value;

            if (UIController.Instance != null)
            {
                // 午前の行動を表示
                if (actionData.IsActionFixed)
                {
                    UIController.Instance.UpdateActionDisplay(playerRef, true, actionData.MorningAction);
                    Debug.Log($"[GameFlowManager] Player {playerRef} の午前の行動を公開: {actionData.MorningAction.Type}");
                }

                // 午後の行動を表示
                if (actionData.IsActionFixed)
                {
                    UIController.Instance.UpdateActionDisplay(playerRef, false, actionData.AfternoonAction);
                    Debug.Log($"[GameFlowManager] Player {playerRef} の午後の行動を公開: {actionData.AfternoonAction.Type}");
                }
            }
        }
    }


    /// <summary>
    /// 選択フェーズ開始時に、全クライアントでUIの行動選択状態をリセットする
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetActionSelection()
    {
        Debug.Log("[GameFlowManager] RPC_ResetActionSelection が呼ばれました");

        if (UIController.Instance != null)
        {
            UIController.Instance.ResetActionSelection();
        }
    }

    /// <summary>
    /// 7.2: 特殊能力選択パネルを表示するRPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSpecialAbilityChoice(PlayerRef targetPlayer, 
        SpecialAbilityType ability1, SpecialAbilityType ability2, SpecialAbilityType ability3, int choiceCount,
        SpecialAbilityType disabled1, SpecialAbilityType disabled2, int disabledCount)
    {
        Debug.Log($"[RPC] 特殊能力選択パネル表示: targetPlayer={targetPlayer.PlayerId}, choiceCount={choiceCount}, disabledCount={disabledCount}");
        
        // NetworkArray を直接渡せないため、個別の引数で受け取り配列に変換
        var choices = new SpecialAbilityType[choiceCount];
        if (choiceCount >= 1) choices[0] = ability1;
        if (choiceCount >= 2) choices[1] = ability2;
        if (choiceCount >= 3) choices[2] = ability3;
        
        // 無効化リスト
        SpecialAbilityType[] disabledAbilities = null;
        if (disabledCount > 0)
        {
            disabledAbilities = new SpecialAbilityType[disabledCount];
            if (disabledCount >= 1) disabledAbilities[0] = disabled1;
            if (disabledCount >= 2) disabledAbilities[1] = disabled2;
        }
        
        UIController.Instance?.ShowSpecialAbilityChoice(targetPlayer, choices, disabledAbilities);
    }

    /// <summary>
    /// 7.2: 待機中パネルを表示するRPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowWaitingPanel(PlayerRef targetPlayer, string message)
    {
        Debug.Log($"[RPC] 待機パネル表示: targetPlayer={targetPlayer.PlayerId}");
        
        // ローカルプレイヤーが targetPlayer の場合のみ表示
        if (Runner.LocalPlayer == targetPlayer)
        {
            UIController.Instance?.ShowWaitingPanel(message);
        }
    }

    /// <summary>
    /// 7.2: 待機中パネルを非表示にするRPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideWaitingPanel(PlayerRef targetPlayer)
    {
        Debug.Log($"[RPC] 待機パネル非表示: targetPlayer={targetPlayer.PlayerId}");
        
        // ローカルプレイヤーが targetPlayer の場合のみ非表示
        if (Runner.LocalPlayer == targetPlayer)
        {
            UIController.Instance?.HideWaitingPanel();
        }
    }

    /// <summary>
    /// 7.2: 特殊能力選択完了を通知するRPC（クライアント→サーバー）
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_NotifyAbilitySelected(SpecialAbilityType selectedAbility)
    {
        Debug.Log($"[RPC] 特殊能力選択完了通知を受信: {selectedAbility}");
        _selectedAbilityFromClient = selectedAbility;
        _abilitySelectionComplete = true;
    }

    /// <summary>
    /// 7.2: 特殊能力選択完了を通知（UIControllerから呼び出される）
    /// </summary>
    public void NotifyAbilitySelected(SpecialAbilityType selectedAbility)
    {
        Debug.Log($"[GameFlowManager] NotifyAbilitySelected: {selectedAbility}");
        
        // サーバーにRPCで通知
        RPC_NotifyAbilitySelected(selectedAbility);
    }

    /// <summary>
    /// 糖尿病の朝処理（準備フェーズで呼び出される）
    /// </summary>
    private void CheckDiabetes()
    {
        Debug.Log("[GameFlowManager] 糖尿病チェック開始");

        // GameManagerから全プレイヤーのUyopyonStateを取得
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager] GameManager.Instanceがnullです");
            return;
        }

        var allUyopyons = GameManager.Instance.uyopyonStateDict;
        if (allUyopyons == null || allUyopyons.Count == 0)
        {
            Debug.LogWarning("[GameFlowManager] UyopyonStateが見つかりません");
            return;
        }

        foreach (var kvp in allUyopyons)
        {
            PlayerRef player = kvp.Key;
            UyopyonState state = kvp.Value;

            if (state == null)
            {
                Debug.LogWarning($"[GameFlowManager] Player {player} のUyopyonStateがnullです");
                continue;
            }

            // 糖尿病の状態異常チェック
            if (state.HasStatusAilment(StatusAilment.Diabetes))
            {
                // 重さ-20、元気-20
                state.Weight += gameParams.DiabetesMorningWeightChange;
                state.Energy += gameParams.DiabetesMorningEnergyChange;

                // プレイヤー名を取得
                string playerName = GetPlayerName(player);

                // ログに追加
                string log = $"{playerName}は糖尿病の影響を受けた！重さ{FormatNumber(gameParams.DiabetesMorningWeightChange)}、元気{FormatNumber(gameParams.DiabetesMorningEnergyChange)}";
                RPC_AddLog(log);

                Debug.Log($"[GameFlowManager] {log}");
            }
        }
    }

    /// <summary>
    /// 熱中症による強制つういんチェック（準備フェーズで呼び出される）
    /// 前日午後に熱中症が発症した場合、午前の行動を「つういん」に固定する
    /// </summary>
    private void CheckHeatstrokeMorningClinic()
    {
        Debug.Log("[GameFlowManager] 熱中症による強制つういんチェック開始");

        // GameManagerから全プレイヤーのPlayerActionDataを取得
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager] GameManager.Instanceがnullです");
            return;
        }

        var allPlayerActions = GameManager.Instance.playerActionDataDict;
        if (allPlayerActions == null || allPlayerActions.Count == 0)
        {
            Debug.LogWarning("[GameFlowManager] PlayerActionDataが見つかりません");
            return;
        }

        foreach (var kvp in allPlayerActions)
        {
            PlayerRef player = kvp.Key;
            PlayerActionData actionData = kvp.Value;

            if (actionData == null)
            {
                Debug.LogWarning($"[GameFlowManager] Player {player} のPlayerActionDataがnullです");
                continue;
            }

            // MorningActionLockedがtrueの場合、午前の行動を「つういん」に設定
            if (actionData.MorningActionLocked)
            {
                actionData.MorningAction = new ActionData(ActionType.Clinic, Genre.Rock);

                // プレイヤー名を取得
                string playerName = GetPlayerName(player);

                // ログに追加
                string log = $"{playerName}は熱中症の影響で午前の行動が「つういん」に固定されました";
                RPC_AddLog(log);

                Debug.Log($"[GameFlowManager] {playerName} の午前の行動を「つういん」に設定");

                // ロックフラグを解除（つういん実行後に熱中症が治るため）
                actionData.MorningActionLocked = false;
            }
        }
    }

    /// <summary>
    /// 7.1: 進化判定と特殊能力選択
    /// Weight >= 200 && !HasEvolved の条件で進化処理を実行
    /// </summary>
    private async UniTask CheckEvolution()
    {
        Debug.Log("[GameFlowManager] 進化判定開始");

        // GameManagerから全プレイヤーのUyopyonStateを取得
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager] GameManager.Instanceがnullです");
            return;
        }

        var allUyopyons = GameManager.Instance.uyopyonStateDict;
        if (allUyopyons == null || allUyopyons.Count == 0)
        {
            Debug.LogWarning("[GameFlowManager] UyopyonStateが見つかりません");
            return;
        }

        // 7.2: 進化条件を満たした全プレイヤーを収集
        var candidates = new List<KeyValuePair<PlayerRef, UyopyonState>>();
        
        foreach (var kvp in allUyopyons)
        {
            PlayerRef player = kvp.Key;
            UyopyonState state = kvp.Value;

            if (state == null)
            {
                Debug.LogWarning($"[GameFlowManager] Player {player} のUyopyonStateがnullです");
                continue;
            }

            // 進化条件チェック: Weight >= 200 && !HasEvolved
            if (state.Weight >= 200 && !state.HasEvolved)
            {
                Debug.Log($"[GameFlowManager] Player {player} が進化条件を満たしました (Weight: {state.Weight}, HasEvolved: {state.HasEvolved})");
                candidates.Add(kvp);
            }
        }

        // 進化候補がいない場合は終了
        if (candidates.Count == 0)
        {
            Debug.Log("[GameFlowManager] 進化条件を満たしたプレイヤーはいません");
            return;
        }

        Debug.Log($"[GameFlowManager] {candidates.Count}人が進化条件を満たしました");

        // 7.2: 優先順位を決定
        var orderedCandidates = DetermineEvolutionOrder(candidates);


        // 7.2: 順番に各プレイヤーに選択させる
        for (int i = 0; i < orderedCandidates.Count; i++)
        {
            var kvp = orderedCandidates[i];
            PlayerRef currentPlayer = kvp.Key;
            UyopyonState state = kvp.Value;
            string playerName = GetPlayerName(currentPlayer);

            Debug.Log($"[GameFlowManager] Player {currentPlayer} ({playerName}) の進化処理開始 (順位: {i + 1}/{orderedCandidates.Count})");

            // 7.2修正: 現在の選択中プレイヤーの待機パネルを確実に非表示
            RPC_HideWaitingPanel(currentPlayer);
            Debug.Log($"[GameFlowManager] Player {currentPlayer.PlayerId} の待機パネルを非表示（選択開始前）");

            // 7.2: 他のプレイヤーに待機パネルを表示
            for (int j = 0; j < orderedCandidates.Count; j++)
            {
                if (i != j)
                {
                    PlayerRef waitingPlayer = orderedCandidates[j].Key;
                    RPC_ShowWaitingPanel(waitingPlayer, "対戦相手が特殊能力を選択中です。");
                    Debug.Log($"[GameFlowManager] Player {waitingPlayer.PlayerId} に待機パネルを表示");
                }
            }

            // UIの更新を確実にするため少し待機
            await UniTask.Delay(200);

            // 利用可能な特殊能力から、すでに選ばれたものを除外
            var availableChoices = new List<SpecialAbilityType>();
            for (int k = 0; k < AvailableSpecialAbilities.Length; k++)
            {
                var ability = AvailableSpecialAbilities[k];
                availableChoices.Add(ability);
            }

            // 最大3つの選択肢を準備
            var choices = availableChoices.Take(3).ToArray();

            if (choices.Length == 0)
            {
                Debug.LogError($"[GameFlowManager] Player {currentPlayer} に提供できる特殊能力がありません");
                continue;
            }

            Debug.Log($"[GameFlowManager] Player {currentPlayer} に {choices.Length} 個の選択肢を表示、{_selectedAbilities.Count} 個が無効化");

            // ログに追加
            RPC_AddLog($"{playerName}が進化条件を満たした！特殊能力を選択してください");

            // 7.2: 選択完了フラグをリセット
            _abilitySelectionComplete = false;
            _selectedAbilityFromClient = null;

            // 7.2: RPC経由で特殊能力選択UIを表示（無効化リストも渡す）
            SpecialAbilityType ability1 = choices.Length > 0 ? choices[0] : SpecialAbilityType.Gaishoku;
            SpecialAbilityType ability2 = choices.Length > 1 ? choices[1] : SpecialAbilityType.Gaishoku;
            SpecialAbilityType ability3 = choices.Length > 2 ? choices[2] : SpecialAbilityType.Gaishoku;
            // 無効化する特殊能力を設定
            
            SpecialAbilityType disabled1 = _selectedAbilities.Count > 0 ? _selectedAbilities[0] : SpecialAbilityType.Gaishoku;
            SpecialAbilityType disabled2 = _selectedAbilities.Count > 1 ? _selectedAbilities[1] : SpecialAbilityType.Gaishoku;
            
            RPC_ShowSpecialAbilityChoice(currentPlayer, ability1, ability2, ability3, choices.Length, disabled1, disabled2, _selectedAbilities.Count);

            // 選択完了を待機（RPCベース）
            Debug.Log($"[GameFlowManager] プレイヤー {currentPlayer} の特殊能力選択を待機中...");
            
            float timeout = 60f; // 60秒のタイムアウト
            float elapsed = 0f;
            
            while (!_abilitySelectionComplete)
            {
                await UniTask.Yield();
                elapsed += Time.deltaTime;
                
                if (elapsed >= timeout)
                {
                    Debug.LogWarning($"[GameFlowManager] プレイヤー {currentPlayer} の特殊能力選択がタイムアウトしました");
                    break;
                }
            }

            // 選択された特殊能力を取得
            SpecialAbilityType? selectedAbility = _selectedAbilityFromClient;
            
            if (selectedAbility.HasValue)
            {
                Debug.Log($"[GameFlowManager] プレイヤー {currentPlayer} が {selectedAbility.Value} を選択しました");

                // UyopyonStateを更新（ネットワーク同期される）
                state.SpecialAbilityName = selectedAbility.Value.ToString();
                state.HasEvolved = true;
                state.VisualType = "Evolved"; // 進化後のビジュアルタイプ

                Debug.Log($"[GameFlowManager] UyopyonState更新完了: HasEvolved={state.HasEvolved}, SpecialAbilityName={state.SpecialAbilityName}");

                // 7.2: 選ばれた特殊能力を記録（次のプレイヤーの選択肢から除外）
                _selectedAbilities.Add(selectedAbility.Value);

                // ログに追加
                RPC_AddLog($"{playerName}は{GetSpecialAbilityDisplayName(selectedAbility.Value)}を習得した！");
                
                Debug.Log($"[GameFlowManager] プレイヤー {currentPlayer} の進化完了: {selectedAbility.Value}");
            }
            else
            {
                Debug.LogWarning($"[GameFlowManager] プレイヤー {currentPlayer} の特殊能力が選択されませんでした");
                
                // タイムアウト時もHasEvolvedをtrueにして、再度選択させないようにする
                state.HasEvolved = true;
                Debug.Log($"[GameFlowManager] タイムアウトのためHasEvolved=trueに設定");
            }

            // 7.2: 全プレイヤーの待機パネルを非表示
            for (int j = 0; j < orderedCandidates.Count; j++)
            {
                PlayerRef player = orderedCandidates[j].Key;
                RPC_HideWaitingPanel(player);
                Debug.Log($"[GameFlowManager] Player {player.PlayerId} の待機パネルを非表示（選択完了後）");
            }

            // UIの更新を確実にするため少し待機
            await UniTask.Delay(300);
        }

        Debug.Log("[GameFlowManager] 進化判定終了");
    }

    /// <summary>
    /// 特殊能力の表示名を取得
    /// </summary>
    private string GetSpecialAbilityDisplayName(SpecialAbilityType ability)
    {
        switch (ability)
        {
            case SpecialAbilityType.Gaishoku:
                return "がいしょく";
            case SpecialAbilityType.Kintre:
                return "きんとれ";
            case SpecialAbilityType.Gamushara:
                return "がむしゃら";
            case SpecialAbilityType.Benkyou:
                return "べんきょう";
            case SpecialAbilityType.Jukusui:
                return "じゅくすい";
            case SpecialAbilityType.Dokagui:
                return "どかぐい";
            default:
                return ability.ToString();
        }
    }

    /// <summary>
    /// 7.2: 進化条件を満たした複数プレイヤーの優先順位を決定
    /// </summary>
    /// <param name="candidates">進化候補のプレイヤーとUyopyonStateのリスト</param>
    /// <returns>優先順位順にソートされたリスト</returns>
    private List<KeyValuePair<PlayerRef, UyopyonState>> DetermineEvolutionOrder(List<KeyValuePair<PlayerRef, UyopyonState>> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return new List<KeyValuePair<PlayerRef, UyopyonState>>();
        }

        Debug.Log($"[GameFlowManager] DetermineEvolutionOrder: {candidates.Count}人の進化候補");

        // 優先順位でソート
        var sorted = candidates.OrderByDescending(kvp =>
        {
            // 1. 重さが大きい方が先
            // 2. 重さが同値なら元気が大きい方が先
            // 3. 元気も同値ならランダム（PlayerRef.PlayerId でソート）
            return kvp.Value.Weight * 10000 + kvp.Value.Energy;
        }).ToList();

        // 元気も同値の場合はランダムにする必要がある
        // 同じ Weight と Energy の組み合わせがある場合、ランダムに並び替え
        var finalOrder = new List<KeyValuePair<PlayerRef, UyopyonState>>();
        var currentGroup = new List<KeyValuePair<PlayerRef, UyopyonState>>();
        float lastWeight = -1;
        float lastEnergy = -1;

        foreach (var kvp in sorted)
        {
            if (kvp.Value.Weight == lastWeight && kvp.Value.Energy == lastEnergy)
            {
                // 同じグループに追加
                currentGroup.Add(kvp);
            }
            else
            {
                // 前のグループをシャッフルして追加
                if (currentGroup.Count > 0)
                {
                    ShuffleList(currentGroup);
                    finalOrder.AddRange(currentGroup);
                    currentGroup.Clear();
                }

                // 新しいグループを開始
                currentGroup.Add(kvp);
                lastWeight = kvp.Value.Weight;
                lastEnergy = kvp.Value.Energy;
            }
        }

        // 最後のグループをシャッフルして追加
        if (currentGroup.Count > 0)
        {
            ShuffleList(currentGroup);
            finalOrder.AddRange(currentGroup);
        }

        // デバッグログ
        for (int i = 0; i < finalOrder.Count; i++)
        {
            var kvp = finalOrder[i];
            Debug.Log($"[GameFlowManager] 進化順位 {i + 1}: Player {kvp.Key.PlayerId} (Weight: {kvp.Value.Weight}, Energy: {kvp.Value.Energy})");
        }

        return finalOrder;
    }

    /// <summary>
    /// リストをシャッフルするヘルパーメソッド
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    /// <summary>
    /// プレイヤー名を取得するヘルパーメソッド
    /// </summary>
    private string GetPlayerName(PlayerRef player)
    {
        // 方法1: Runner.GetPlayerObjectから取得
        var networkPlayerObj = Runner.GetPlayerObject(player);
        if (networkPlayerObj != null && networkPlayerObj.TryGetBehaviour<NetworkPlayer>(out var np))
        {
            string name = np.PlayerName.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        // 方法2: シーン内の全NetworkPlayerから検索
        var allNetworkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var networkPlayer in allNetworkPlayers)
        {
            if (networkPlayer.OwnerPlayerRef == player)
            {
                string name = networkPlayer.PlayerName.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
        }

        // フォールバック: PlayerIDを表示
        Debug.LogWarning($"[GameFlowManager] Player {player} の名前が取得できませんでした");
        return $"Player{player.PlayerId}";
    }

    /// <summary>
    /// 数値を色付きフォーマットで返す（正の値は青、負の値は赤）
    /// </summary>
    private string FormatNumber(int value)
    {
        if (value > 0)
        {
            return $"<color=blue><b>+{value}</b></color>";
        }
        else if (value < 0)
        {
            return $"<color=red><b>{value}</b></color>";
        }
        else
        {
            return "±0";
        }
    }

    /// <summary>
    /// 8.1 & 8.2 ゲーム終了処理
    /// 勝者を記録し、アニメーションを再生し、ゲーム終了フラグを立てる
    /// </summary>
    public async UniTask EndGame(PlayerRef winner, bool isMorning)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[GameFlowManager] EndGame: StateAuthorityがありません");
            return;
        }

        Debug.Log($"[GameFlowManager] EndGame: ゲームを終了します。勝者={winner}");

        // ゲーム終了フラグを設定
        IsGameEnded = true;
        Winner = winner;
        CurrentPhase = GamePhase.GameEnd;

        // 勝者・敗者の名前を取得
        string winnerName = GetPlayerName(winner);

        // 敗者を特定
        var allPlayers = GameManager.Instance.uyopyonStateDict.Keys.ToList();
        PlayerRef loser = allPlayers.FirstOrDefault(p => p != winner);
        string loserName = GetPlayerName(loser);

        // ログに記録
        RPC_AddLog($"=== ゲーム終了 ===");
        RPC_AddLog($"勝者: {winnerName}");
        RPC_AddLog($"敗者: {loserName}");

        Debug.Log($"[GameFlowManager] ゲーム終了。勝者: {winnerName}, 敗者: {loserName}");

        // 8.2: 勝利・敗北アニメーションを再生
        RPC_PlayVictoryAnimation(winner);
        RPC_PlayDefeatAnimation(loser);

        // TODO: BGM変更（AudioManager実装後、10.4で実装予定）
        // AudioManager.Instance?.PlayBGM("GameEnd");

        // 8.2: 4秒待機
        await UniTask.Delay(4000);
        Debug.Log("[GameFlowManager] アニメーション再生完了、4秒待機後");

        // 8.3: リザルト画面表示
        RPC_ShowResultPanel(winner, CurrentDay, isMorning);
    }

    /// <summary>
    /// 8.2: 勝利アニメーション再生をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayVictoryAnimation(PlayerRef player)
    {
        Debug.Log($"[GameFlowManager] RPC_PlayVictoryAnimation: Player {player}");
        if (UIController.Instance != null)
        {
            UIController.Instance.PlayVictoryAnimation(player);
        }
    }

    /// <summary>
    /// 8.2: 敗北アニメーション再生をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDefeatAnimation(PlayerRef player)
    {
        Debug.Log($"[GameFlowManager] RPC_PlayDefeatAnimation: Player {player}");
        if (UIController.Instance != null)
        {
            UIController.Instance.PlayDefeatAnimation(player);
        }
    }

    /// <summary>
    /// 8.3: リザルト画面表示をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResultPanel(PlayerRef winner, int day, bool isMorning)
    {
        Debug.Log($"[GameFlowManager] RPC_ShowResultPanel: winner={winner}, day={day}, isMorning={isMorning}");
        if (UIController.Instance != null)
        {
            UIController.Instance.ShowResultPanel(winner, day, isMorning);
        }
        else
        {
            Debug.LogError("[GameFlowManager] UIController.Instance が null です");
        }
    }
}
