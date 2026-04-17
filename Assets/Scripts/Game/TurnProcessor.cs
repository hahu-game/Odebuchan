using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Cysharp.Threading.Tasks;

/// <summary>
/// 1日の処理（午前・午後の行動実行）を管理するクラス
/// ホスト側でのみ実行される
/// </summary>
public class TurnProcessor : NetworkBehaviour
{
    // シングルトンパターン（シーン内に1つ）
    public static TurnProcessor Instance { get; private set; }

    // GameParametersへの参照（GameFlowManagerから取得）
    private GameParameters gameParams;

    // GameManagerへの参照
    private GameManager gameManager;

    // 特殊能力の実行ロジック
    private SpecialAbilityExecutor specialAbility;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // GameParametersの取得
        if (GameFlowManager.Instance != null)
        {
            gameParams = GameFlowManager.Instance.gameParams;
        }

        // GameManagerの取得
        gameManager = GameManager.Instance;

        if (gameParams == null)
        {
            Debug.LogError("[TurnProcessor] GameParameters が設定されていません");
        }

        if (gameManager == null)
        {
            Debug.LogError("[TurnProcessor] GameManager が見つかりません");
        }

        // SpecialAbilityExecutorのインスタンスを作成
        specialAbility = new SpecialAbilityExecutor(gameParams, this);
    }

    /// <summary>
    /// 午前の処理を実行
    /// </summary>
    public async Task ProcessMorning(Dictionary<PlayerRef, PlayerActionData> playerActions)
    {
        Debug.Log("[TurnProcessor] === 午前の処理を開始 ===");

        // 各プレイヤーの午前の行動を取得
        var players = playerActions.Keys.ToList();
        if (players.Count != 2)
        {
            Debug.LogError("[TurnProcessor] プレイヤーが2人ではありません");
            return;
        }

        PlayerRef p1 = players[0];
        PlayerRef p2 = players[1];

        ActionData p1Action = playerActions[p1].MorningAction;
        ActionData p2Action = playerActions[p2].MorningAction;

        // 1. ジャンケン判定と効果適用
        await ProcessJanken(p1, p2, p1Action.Genre, p2Action.Genre, true);

        // 2. 連続使用ペナルティチェック（前日午後と今日午前）
        CheckConsecutivePenalty(p1, p1Action.Type, playerActions[p1].LastAfternoonAction.Type);
        CheckConsecutivePenalty(p2, p2Action.Type, playerActions[p2].LastAfternoonAction.Type);

        // 3. 行動実行
        await ExecuteAction(p1, p1Action, true);
        await ExecuteAction(p2, p2Action, true);

        // 4. 勝利判定（8.1 & 8.2 & 8.3）
        await CheckVictory(true); // 午前

        Debug.Log("[TurnProcessor] === 午前の処理が完了 ===");
    }

    /// <summary>
    /// 午後の処理を実行
    /// </summary>
    public async Task ProcessAfternoon(Dictionary<PlayerRef, PlayerActionData> playerActions)
    {
        Debug.Log("[TurnProcessor] === 午後の処理を開始 ===");

        // 各プレイヤーの午後の行動を取得
        var players = playerActions.Keys.ToList();
        if (players.Count != 2)
        {
            Debug.LogError("[TurnProcessor] プレイヤーが2人ではありません");
            return;
        }

        PlayerRef p1 = players[0];
        PlayerRef p2 = players[1];

        ActionData p1Action = playerActions[p1].AfternoonAction;
        ActionData p2Action = playerActions[p2].AfternoonAction;

        // 1. ジャンケン判定と効果適用
        await ProcessJanken(p1, p2, p1Action.Genre, p2Action.Genre, false);

        // 2. 連続使用ペナルティチェック（今日午前と今日午後）
        CheckConsecutivePenalty(p1, p1Action.Type, playerActions[p1].MorningAction.Type);
        CheckConsecutivePenalty(p2, p2Action.Type, playerActions[p2].MorningAction.Type);

        // 3. 行動実行
        await ExecuteAction(p1, p1Action, false);
        await ExecuteAction(p2, p2Action, false);

        // 4. 午後の行動を記録（次の日の午前判定用）
        // 注: これはホスト側で実行されるが、PlayerActionDataのプロパティ変更は自動的に同期される
        playerActions[p1].LastAfternoonAction = p1Action;
        playerActions[p2].LastAfternoonAction = p2Action;

        // 5. 勝利判定（8.1 & 8.2 & 8.3）
        await CheckVictory(false); // 午後

        Debug.Log("[TurnProcessor] === 午後の処理が完了 ===");
    }

    /// <summary>
    /// ジャンケン判定と効果適用
    /// </summary>
    private async Task ProcessJanken(PlayerRef p1, PlayerRef p2, Genre p1Genre, Genre p2Genre, bool isMorning)
    {
        Debug.Log($"[TurnProcessor] ジャンケン判定: P1({p1})={p1Genre}, P2({p2})={p2Genre}");

        // Noneが含まれる場合、ジャンケン判定なし
        if (p1Genre == Genre.None || p2Genre == Genre.None)
        {
            RPC_AddLog("ジャンケン判定なし");
            return;
        }

        // あいこの場合
        if (p1Genre == p2Genre)
        {
            RPC_AddLog("あいこ！ジャンケン効果なし");
            return;
        }

        // 勝者を判定
        PlayerRef winner = PlayerRef.None;
        PlayerRef loser = PlayerRef.None;
        Genre winGenre = Genre.None;
        Genre loseGenre = Genre.None;

        if (IsWinner(p1Genre, p2Genre))
        {
            winner = p1;
            loser = p2;
            winGenre = p1Genre;
            loseGenre = p2Genre;
        }
        else
        {
            winner = p2;
            loser = p1;
            winGenre = p2Genre;
            loseGenre = p1Genre;
        }

        Debug.Log($"[TurnProcessor] ジャンケン結果: Winner={winner}({winGenre}), Loser={loser}({loseGenre})");

        // ジャンケン効果を適用
        ApplyJankenEffect(winner, loser, winGenre, loseGenre);

        // エフェクト・効果音再生（2秒待機）
        RPC_ShowJankenResult(winner, winGenre, isMorning);

        // じゃんけん勝敗SE再生
        RPC_PlayJankenResultSE(winner, loser);

        await UniTask.Delay((int)(gameParams.ActionWaitDuration * 1000));
    }

    /// <summary>
    /// 行動を実行
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="action">行動データ</param>
    /// <param name="isMorning">午前の行動かどうか</param>
    private async Task ExecuteAction(PlayerRef player, ActionData action, bool isMorning)
    {
        // 腰痛チェック（30%の確率で行動がキャンセルされる）
        CheckBackPain(player, ref action);

        Debug.Log($"[TurnProcessor] Player {player} が {action.Type} を実行（{(isMorning ? "午前" : "午後")}）");

        // ActionType.None の場合は何も処理しない（腰痛でキャンセルされた場合など）
        if (action.Type == ActionType.None)
        {
            Debug.Log($"[TurnProcessor] Player {player} は行動がキャンセルされました");
            return;
        }

        UyopyonState state = GetUyopyonState(player);
        if (state == null)
        {
            Debug.LogError("[TurnProcessor] UyopyonState が取得できません");
            return;
        }

        string playerName = GetColoredPlayerName(player);

        // べんきょう以外の行動を選択した場合、StudyComboをリセット
        bool isBenkyou = action.Type == ActionType.SpecialAbility &&
                         action.SpecialAbilityName.ToString() == "Benkyou";
        if (!isBenkyou && state.StudyCombo > 0)
        {
            state.StudyCombo = 0;
            Debug.Log($"[TurnProcessor] {playerName} のべんきょう連続カウントをリセットしました");
        }

        // 行動の種類に応じて処理を分岐
        switch (action.Type)
        {
            case ActionType.Eat:
                ExecuteEat(player, state, playerName);
                break;

            case ActionType.Sleep:
                ExecuteSleep(player, state, playerName);
                break;

            case ActionType.Play:
                ExecutePlay(player, state, playerName, isMorning);
                break;

            case ActionType.Clinic:
                ExecuteClinic(player, state, playerName);
                break;

            case ActionType.SpecialAbility:
                // 特殊能力の実行
                ExecuteSpecialAbility(player, state, playerName, isMorning);
                break;

            default:
                Debug.LogWarning($"[TurnProcessor] 未実装の行動: {action.Type}");
                break;
        }

        // アニメーション再生（2秒待機）
        RPC_PlayActionAnimation(player, action.Type);
        await UniTask.Delay((int)(gameParams.ActionWaitDuration * 1000));
    }

    /// <summary>
    /// UyopyonStateを取得するヘルパーメソッド
    /// </summary>
    private UyopyonState GetUyopyonState(PlayerRef player)
    {
        // GameManagerから取得（実装は後で）
        return gameManager.GetUyopyonState(player);
    }

    /// <summary>
    /// ジャンケンの勝敗判定
    /// </summary>
    private bool IsWinner(Genre g1, Genre g2)
    {
        if (g1 == Genre.Paper && g2 == Genre.Rock) return true;
        if (g1 == Genre.Rock && g2 == Genre.Scissors) return true;
        if (g1 == Genre.Scissors && g2 == Genre.Paper) return true;
        return false;
    }

    /// <summary>
    /// ジャンケン効果の適用
    /// </summary>
    private void ApplyJankenEffect(PlayerRef winner, PlayerRef loser, Genre winGenre, Genre loseGenre)
    {
        UyopyonState winnerState = GetUyopyonState(winner);
        UyopyonState loserState = GetUyopyonState(loser);

        if (winnerState == null || loserState == null)
        {
            Debug.LogError("[TurnProcessor] UyopyonState が取得できません");
            return;
        }

        // じゃんけん勝利数をカウント
        winnerState.JankenWinCount++;
        Debug.Log($"[TurnProcessor] じゃんけん勝利: {winner} の勝利数 = {winnerState.JankenWinCount}");

        string winnerName = GetColoredPlayerName(winner);
        string loserName = GetColoredPlayerName(loser);
        string effectLog = "";

        if (winGenre == Genre.Rock && loseGenre == Genre.Scissors)
        {
            // グー > チョキ
            // 勝者: 重さ+10
            winnerState.Weight += 10;
            // 敗者: たべる時の重さバフ-5、ねむる時の元気バフ-5
            loserState.PlayBuffWeight -= 5;
            loserState.PlayBuffEnergy -= 5;

            effectLog = $"{winnerName}はジャンケンに勝った！！重さ{FormatNumber(10)}。" +
                       $"{loserName}は、たべる時の重さ{FormatNumber(-5)}、ねむる時の元気{FormatNumber(-5)}";
        }
        else if (winGenre == Genre.Scissors && loseGenre == Genre.Paper)
        {
            // チョキ > パー
            // 勝者: たべる時の重さバフ+5、ねむる時の元気バフ+5
            winnerState.PlayBuffWeight += 5;
            winnerState.PlayBuffEnergy += 5;
            // 敗者: 元気-10
            loserState.Energy -= 10;

            effectLog = $"{winnerName}はジャンケンに勝った！！たべる時の重さ{FormatNumber(5)}、ねむる時の元気{FormatNumber(5)}。" +
                       $"{loserName}は、元気{FormatNumber(-10)}";
        }
        else if (winGenre == Genre.Paper && loseGenre == Genre.Rock)
        {
            // パー > グー
            // 勝者: 元気+10
            winnerState.Energy += 10;
            // 敗者: 重さ-10
            loserState.Weight -= 10;

            effectLog = $"{winnerName}はジャンケンに勝った！！元気{FormatNumber(10)}。" +
                       $"{loserName}は、重さ{FormatNumber(-10)}";
        }

        RPC_AddLog(effectLog);
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

        // 方法2: GameManagerのDictionaryから取得（FindObjectsByType削減）
        if (GameManager.Instance != null)
        {
            NetworkPlayer networkPlayer = GameManager.Instance.GetNetworkPlayer(player);
            if (networkPlayer != null)
            {
                string name = networkPlayer.PlayerName.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
        }

        // フォールバック: PlayerIDを表示
        Debug.LogWarning($"[TurnProcessor] Player {player} の名前が取得できませんでした");
        return $"Player{player.PlayerId}";
    }

    /// <summary>
    /// プレイヤー名を色付きで取得するヘルパーメソッド（ログ表示用）
    /// </summary>
    private string GetColoredPlayerName(PlayerRef player)
    {
        string playerName = GetPlayerName(player);

        if (UIController.Instance != null)
        {
            return UIController.Instance.FormatPlayerNameForLog(playerName, player);
        }

        return playerName;
    }

    /// <summary>
    /// 数値を色付きフォーマットで返す（正の値は青、負の値は赤）
    /// </summary>
    public string FormatNumber(int value)
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
    /// 連続使用ペナルティをチェックして適用
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="currentAction">現在の行動</param>
    /// <param name="previousAction">前の行動</param>
    /// <returns>ペナルティが適用された場合true</returns>
    private bool CheckConsecutivePenalty(PlayerRef player, ActionType currentAction, ActionType previousAction)
    {
        // 連続使用判定
        if (currentAction == previousAction && currentAction != ActionType.None)
        {
            // 元気-20のペナルティ
            var uyopyon = GetUyopyonState(player);
            if (uyopyon != null)
            {
                uyopyon.Energy -= gameParams.ConsecutiveActionPenalty; // 20

                string playerName = GetColoredPlayerName(player);
                RPC_AddLog($"{playerName}の連続使用ペナルティ！元気{FormatNumber(-gameParams.ConsecutiveActionPenalty)}");

                Debug.Log($"[TurnProcessor] {player} 連続使用ペナルティ適用: {previousAction} -> {currentAction}");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// ジャンケン結果をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowJankenResult(PlayerRef winner, Genre winGenre, NetworkBool isMorning)
    {
        Debug.Log($"[TurnProcessor RPC] ジャンケン結果: Winner={winner}, Genre={winGenre}, isMorning={isMorning}");
        UIController.Instance?.ShowJankenEffect(winner, winGenre, isMorning);
    }

    /// <summary>
    /// じゃんけん勝敗SE再生をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayJankenResultSE(PlayerRef winner, PlayerRef loser)
    {
        Debug.Log($"[TurnProcessor RPC] じゃんけん勝敗SE再生: Winner={winner}, Loser={loser}");

        // ローカルプレイヤーが勝者の場合は勝利SE、敗者の場合は敗北SEを再生
        if (Runner.LocalPlayer == winner)
        {
            AudioManager.Instance?.PlayJankenWinSE();
        }
        else if (Runner.LocalPlayer == loser)
        {
            AudioManager.Instance?.PlayJankenLoseSE();
        }
    }

    /// <summary>
    /// ログ追加をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AddLog(string message)
    {
        Debug.Log($"[TurnProcessor RPC] ログ追加: {message}");
        UIController.Instance?.AddLog(message);
    }

    /// <summary>
    /// ログ追加（SpecialAbilityから呼び出すためのpublicラッパー）
    /// </summary>
    public void AddLog(string message)
    {
        RPC_AddLog(message);
    }

    /// <summary>
    /// アニメーション再生をRPC経由で全クライアントに通知
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayActionAnimation(PlayerRef player, ActionType action)
    {
        Debug.Log($"[TurnProcessor RPC] アニメーション再生: Player={player}, Action={action}");
        UIController.Instance?.PlayActionAnimation(player, action);
    }

    /// <summary>
    /// たべるの実行
    /// </summary>
    private void ExecuteEat(PlayerRef player, UyopyonState state, string playerName)
    {
        // 元気変化を計算
        int energyChange = gameParams.EatEnergyChange;

        // 重さ変化を計算（PlayBuffWeightとBuffMultiplierを適用）
        int weightChange = (int)((gameParams.EatWeightChange + state.PlayBuffWeight) * state.BuffMultiplier);

        // 連続使用ペナルティのチェック（後で実装）
        // CheckConsecutiveActionPenalty()

        // 元気と重さを更新
        state.Energy += energyChange;
        state.Weight += weightChange;

        // ログに追加
        string log = $"{playerName}は「たべる」を実行！元気{FormatNumber(energyChange)}、重さ{FormatNumber(weightChange)}";
        RPC_AddLog(log);

        Debug.Log($"[TurnProcessor] {log}");

        // たべるSE再生
        AudioManager.Instance?.PlayEatSE();

        // 病気発症判定
        CheckSickness(player, state.Weight);
    }

    /// <summary>
    /// ねむるの実行
    /// </summary>
    private void ExecuteSleep(PlayerRef player, UyopyonState state, string playerName)
    {
        // 元気変化を計算（PlayBuffEnergyとBuffMultiplierを適用）
        int energyChange = (int)((gameParams.SleepEnergyChange + state.PlayBuffEnergy) * state.BuffMultiplier);

        // 睡眠時無呼吸症候群のチェック
        if (state.HasStatusAilment(StatusAilment.SleepApnea))
        {
            energyChange += gameParams.SleepApneaRecoveryReduction;
            RPC_AddLog($"{playerName}は睡眠時無呼吸症候群の影響を受けた！元気回復量{FormatNumber(gameParams.SleepApneaRecoveryReduction)}");
        }

        // 連続使用ペナルティのチェック（後で実装）
        // CheckConsecutiveActionPenalty()

        // 元気を更新
        state.Energy += energyChange;

        // ログに追加
        string log = $"{playerName}は「ねむる」を実行！元気{FormatNumber(energyChange)}";
        RPC_AddLog(log);

        Debug.Log($"[TurnProcessor] {log}");

        // ねむるSE再生
        AudioManager.Instance?.PlaySleepSE();
    }

    /// <summary>
    /// あそぶの実行
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="state">UyopyonState</param>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="isMorning">午前の行動かどうか</param>
    private void ExecutePlay(PlayerRef player, UyopyonState state, string playerName, bool isMorning)
    {
        // 元気変化を計算
        int energyChange = gameParams.PlayEnergyChange;

        // 連続使用ペナルティのチェック（後で実装）
        // CheckConsecutiveActionPenalty()

        // 元気を更新
        state.Energy += energyChange;

        // 永久バフを増加
        int buffWeightIncrement = gameParams.PlayWeightBuffIncrement;
        int buffEnergyIncrement = gameParams.PlayEnergyBuffIncrement;

        state.PlayBuffWeight += buffWeightIncrement;
        state.PlayBuffEnergy += buffEnergyIncrement;

        // ログに追加
        string log = $"{playerName}は「あそぶ」を実行！元気{FormatNumber(energyChange)}、" +
                     $"たべる時の重さバフ{FormatNumber(buffWeightIncrement)}、ねむる時の元気バフ{FormatNumber(buffEnergyIncrement)}";
        RPC_AddLog(log);

        Debug.Log($"[TurnProcessor] {log}");

        // あそぶSE再生
        AudioManager.Instance?.PlayPlaySE();

        // ケガ発症判定
        CheckInjury(player, state.Weight, isMorning);
    }

    /// <summary>
    /// つういんの実行
    /// </summary>
    private void ExecuteClinic(PlayerRef player, UyopyonState state, string playerName)
    {
        // 元気変化を計算
        int energyChange = gameParams.ClinicEnergyChange;

        // 連続使用ペナルティのチェック（後で実装）
        // CheckConsecutiveActionPenalty()

        // 元気を更新
        state.Energy += energyChange;

        // 全ての状態異常をクリア
        bool hadAilment = false;
        for (int i = 0; i < 4; i++)
        {
            if (state.StatusAilments[i] == 1)
            {
                hadAilment = true;
                break;
            }
        }

        state.ClearAllStatusAilments();

        // 熱中症によるロックフラグを解除（つついんで治療されたため）
        PlayerActionData playerActionData = GameManager.Instance?.GetPlayerActionData(player);
        if (playerActionData != null)
        {
            if (playerActionData.MorningActionLocked || playerActionData.AfternoonActionLocked)
            {
                playerActionData.MorningActionLocked = false;
                playerActionData.AfternoonActionLocked = false;
                Debug.Log($"[TurnProcessor] {playerName} の行動ロックフラグを解除しました（つついん実行後）");
            }
        }

        // ログに追加
        string log = $"{playerName}は「つういん」を実行！元気{FormatNumber(energyChange)}";
        if (hadAilment)
        {
            log += "、全ての状態異常が治療されました！";
            // 状態異常回復SE再生
            AudioManager.Instance?.PlayAilmentCureSE();
        }
        RPC_AddLog(log);

        Debug.Log($"[TurnProcessor] {log}");

        // つういんSE再生
        AudioManager.Instance?.PlayClinicSE();
    }
    // ========== 特殊能力実行メソッド (7.3-7.7) ==========

    /// <summary>
    /// 特殊能力を実行する
    /// </summary>
    private void ExecuteSpecialAbility(PlayerRef player, UyopyonState state, string playerName, bool isMorning)
    {
        // 特殊能力名からSpecialAbilityTypeを取得
        // NetworkString<_16>を文字列に変換（文字列補間を使用）
        string abilityName = $"{state.SpecialAbilityName}";

        if (string.IsNullOrEmpty(abilityName))
        {
            Debug.LogWarning($"[TurnProcessor] {playerName}の特殊能力が設定されていません");
            return;
        }

        if (!System.Enum.TryParse<SpecialAbilityType>(abilityName, out SpecialAbilityType abilityType))
        {
            Debug.LogError($"[TurnProcessor] 不明な特殊能力: {abilityName}");
            return;
        }

        Debug.Log($"[TurnProcessor] {playerName}が特殊能力 {abilityType} を実行");

        // StudyComboリセット（べんきょう以外の行動を実行した場合）
        if (abilityType != SpecialAbilityType.Benkyou)
        {
            state.StudyCombo = 0;
        }

        // 特殊能力の種類に応じて処理を分岐（SpecialAbilityクラスに委譲）
        switch (abilityType)
        {
            case SpecialAbilityType.Gaishoku:
                specialAbility.ExecuteGaishoku(player, state, playerName);
                break;

            case SpecialAbilityType.Gamushara:
                specialAbility.ExecuteGamushara(player, state, playerName);
                break;

            case SpecialAbilityType.Benkyou:
                specialAbility.ExecuteBenkyou(player, state, playerName, isMorning);
                break;

            case SpecialAbilityType.Jukusui:
                specialAbility.ExecuteJukusui(player, state, playerName);
                break;

            case SpecialAbilityType.Dokagui:
                specialAbility.ExecuteDokagui(player, state, playerName);
                break;

            case SpecialAbilityType.Kintre:
                specialAbility.ExecuteKintre(player, state, playerName, isMorning);
                break;

            default:
                Debug.LogWarning($"[TurnProcessor] 未実装の特殊能力: {abilityType}");
                break;
        }
    }

    /// <summary>
    /// 病気発症判定（たべる実行時に呼び出される）
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="weight">現在の重さ</param>
    public void CheckSickness(PlayerRef player, int weight)
    {
        // 発症確率を計算（重さ × パラメータ）
        float sicknessChance = weight * gameParams.SicknessProbabilityPerWeight;
        
        // ランダム判定（0～100の乱数）
        float roll = UnityEngine.Random.Range(0f, 100f);
        
        Debug.Log($"[TurnProcessor] CheckSickness: Player={player}, Weight={weight}, Chance={sicknessChance}%, Roll={roll}");
        
        // 発症判定
        if (roll < sicknessChance)
        {
            UyopyonState state = GetUyopyonState(player);
            if (state == null)
            {
                Debug.LogError("[TurnProcessor] UyopyonState が取得できません");
                return;
            }

            string playerName = GetColoredPlayerName(player);

            // すでに持っている病気をチェック
            bool hasSleepApnea = state.HasStatusAilment(StatusAilment.SleepApnea);
            bool hasDiabetes = state.HasStatusAilment(StatusAilment.Diabetes);
            
            StatusAilment newAilment;
            
            // 両方の病気をすでに持っている場合は何も起こらない
            if (hasSleepApnea && hasDiabetes)
            {
                Debug.Log($"[TurnProcessor] {playerName}はすでに両方の病気を持っています");
                return;
            }
            // 睡眠時無呼吸症候群のみ持っている場合は糖尿病を発症
            else if (hasSleepApnea)
            {
                newAilment = StatusAilment.Diabetes;
            }
            // 糖尿病のみ持っている場合は睡眠時無呼吸症候群を発症
            else if (hasDiabetes)
            {
                newAilment = StatusAilment.SleepApnea;
            }
            // 病気を持っていない場合は50%ずつでランダムに選択
            else
            {
                if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
                {
                    newAilment = StatusAilment.SleepApnea;
                }
                else
                {
                    newAilment = StatusAilment.Diabetes;
                }
            }
            
            // 状態異常を設定
            state.SetStatusAilment(newAilment, true);
            
            // 病気名を取得
            string ailmentName = newAilment == StatusAilment.SleepApnea ? "睡眠時無呼吸症候群" : "糖尿病";
            
            // ログに追加
            string log = $"{playerName}は<color=red><b>{ailmentName}</b></color>を発症した！";
            RPC_AddLog(log);

            Debug.Log($"[TurnProcessor] {playerName} が {ailmentName} を発症");

            // 病気発症SE再生
            AudioManager.Instance?.PlaySicknessOnsetSE();
        }
    }

    /// <summary>
    /// ケガ発症判定（あそぶ、きんとれ、べんきょう実行時に呼び出される）
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="weight">現在の重さ</param>
    /// <param name="isMorning">午前の行動かどうか</param>
    public void CheckInjury(PlayerRef player, int weight, bool isMorning)
    {
        // 発症確率を計算（重さ × パラメータ）
        float injuryChance = weight * gameParams.InjuryProbabilityPerWeight;
        
        // ランダム判定（0～100の乱数）
        float roll = UnityEngine.Random.Range(0f, 100f);
        
        Debug.Log($"[TurnProcessor] CheckInjury: Player={player}, Weight={weight}, Chance={injuryChance}%, Roll={roll}");
        
        // 発症判定
        if (roll < injuryChance)
        {
            UyopyonState state = GetUyopyonState(player);
            if (state == null)
            {
                Debug.LogError("[TurnProcessor] UyopyonState が取得できません");
                return;
            }

            string playerName = GetColoredPlayerName(player);

            // すでに持っているケガをチェック
            bool hasBackPain = state.HasStatusAilment(StatusAilment.BackPain);
            bool hasHeatstroke = state.HasStatusAilment(StatusAilment.Heatstroke);
            
            StatusAilment newAilment;
            
            // 両方のケガをすでに持っている場合は何も起こらない
            if (hasBackPain && hasHeatstroke)
            {
                Debug.Log($"[TurnProcessor] {playerName}はすでに両方のケガを持っています");
                return;
            }
            // 腰痛のみ持っている場合は熱中症を発症
            else if (hasBackPain)
            {
                newAilment = StatusAilment.Heatstroke;
            }
            // 熱中症のみ持っている場合は腰痛を発症
            else if (hasHeatstroke)
            {
                newAilment = StatusAilment.BackPain;
            }
            // ケガを持っていない場合は50%ずつでランダムに選択
            else
            {
                if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
                {
                    newAilment = StatusAilment.BackPain;
                }
                else
                {
                    newAilment = StatusAilment.Heatstroke;
                }
            }
            
            // 状態異常を設定
            state.SetStatusAilment(newAilment, true);
            
            // ケガ名を取得
            string ailmentName = newAilment == StatusAilment.BackPain ? "腰痛" : "熱中症";

            // ログに追加
            string log = $"{playerName}は<color=red><b>{ailmentName}</b></color>を発症した！";
            RPC_AddLog(log);

            Debug.Log($"[TurnProcessor] {playerName} が {ailmentName} を発症");

            // ケガ発症SE再生
            AudioManager.Instance?.PlayInjuryOnsetSE();

            // 熱中症の場合、強制つういん処理
            if (newAilment == StatusAilment.Heatstroke)
            {
                // GameManagerからPlayerActionDataを取得
                var playerActionData = gameManager.GetPlayerActionData(player);
                if (playerActionData == null)
                {
                    Debug.LogError("[TurnProcessor] PlayerActionData が取得できません");
                    return;
                }

                if (isMorning)
                {
                    // 午前に発症 → 今日の午後を「つういん」に強制変更
                    playerActionData.AfternoonAction = new ActionData(ActionType.Clinic, Genre.None);
                    playerActionData.AfternoonActionLocked = true;

                    RPC_AddLog($"{playerName}は熱中症で動けない！午後の行動が「つういん」に固定された");
                    Debug.Log($"[TurnProcessor] {playerName} の午後の行動を「つういん」に強制変更");
                }
                else
                {
                    // 午後に発症 → 翌日の午前を「つういん」に固定（準備フェーズで設定）
                    // MorningActionLockedフラグを立てておく
                    playerActionData.MorningActionLocked = true;

                    RPC_AddLog($"{playerName}は熱中症で動けない！翌日午前の行動が「つういん」に固定された");
                    Debug.Log($"[TurnProcessor] {playerName} の翌日午前の行動を「つういん」に固定（準備フェーズで設定）");
                }
            }
        }
    }

    /// <summary>
    /// 腰痛チェック（行動実行前に呼び出される）
    /// 30%の確率で行動がキャンセルされ、何もできなくなる
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <param name="action">現在の行動データ（参照渡しで変更される）</param>
    /// <returns>行動が変更された場合true</returns>
    private bool CheckBackPain(PlayerRef player, ref ActionData action)
    {
        UyopyonState state = GetUyopyonState(player);
        if (state == null)
        {
            Debug.LogError("[TurnProcessor] UyopyonState が取得できません");
            return false;
        }

        // 腰痛の状態異常チェック
        if (state.HasStatusAilment(StatusAilment.BackPain))
        {
            // 30%の確率で行動がキャンセルされる
            float roll = UnityEngine.Random.Range(0f, 100f);
            Debug.Log($"[TurnProcessor] CheckBackPain: Player={player}, Roll={roll}");

            if (roll < gameParams.BackPainCancelProbability * 100)
            {
                // 行動をキャンセル（何もしない）
                action = new ActionData
                {
                    Type = ActionType.None,
                    Genre = Genre.None
                };

                string playerName = GetColoredPlayerName(player);
                RPC_AddLog($"{playerName}は腰痛で動けない！行動ができなかった！");

                Debug.Log($"[TurnProcessor] {playerName} の行動が腰痛によりキャンセルされました");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 8.1 & 8.2 & 8.3 勝利判定
    /// 重さが終了条件以上のプレイヤーがいればゲーム終了
    /// </summary>
    public async Task<PlayerRef?> CheckVictory(bool isMorning)
    {
        Debug.Log("[TurnProcessor] CheckVictory: 勝利判定を開始");

        // 全プレイヤーのUyopyonStateを取得
        var playerStates = gameManager.uyopyonStateDict;
        if (playerStates.Count != 2)
        {
            Debug.LogWarning($"[TurnProcessor] プレイヤーが2人ではありません: {playerStates.Count}人");
            return null;
        }

        PlayerRef? winner = null;

        // === 9.2: 投了判定（最優先） ===
        foreach (var kvp in playerStates)
        {
            PlayerRef player = kvp.Key;

            if (GameManager.Instance.HasSurrendered(player))
            {
                // 投了したプレイヤーの相手を勝者とする
                foreach (var otherKvp in playerStates)
                {
                    if (otherKvp.Key != player)
                    {
                        winner = otherKvp.Key;
                        string winnerName = GetColoredPlayerName(winner.Value);
                        string loserName = GetColoredPlayerName(player);
                        Debug.Log($"[TurnProcessor] 投了による勝者決定: {winnerName} (投了者: {loserName})");

                        // GameFlowManagerにゲーム終了を通知
                        if (GameFlowManager.Instance != null)
                        {
                            await GameFlowManager.Instance.EndGame(winner.Value, isMorning);
                        }
                        else
                        {
                            Debug.LogError("[TurnProcessor] GameFlowManager.Instance が null です");
                        }

                        return winner;
                    }
                }
            }
        }

        // === 体重による勝利判定 ===
        int maxWeight = 0;

        // 各プレイヤーの重さをチェック
        foreach (var kvp in playerStates)
        {
            PlayerRef player = kvp.Key;
            UyopyonState state = kvp.Value;

            Debug.Log($"[TurnProcessor] Player {player} の重さ: {state.Weight}kg");

            // 終了条件の体重より上のプレイヤーがいれば勝者候補
            if (state.Weight >= gameParams.VictoryWeightThreshold)
            {
                // より重いプレイヤーを勝者とする（同じ重さの場合は先に到達した方）
                if (state.Weight > maxWeight)
                {
                    maxWeight = state.Weight;
                    winner = player;
                    Debug.Log($"[TurnProcessor] Player {player} が勝者候補（重さ: {state.Weight}kg）");
                }
            }
        }

        if (winner.HasValue)
        {
            string winnerName = GetColoredPlayerName(winner.Value);
            Debug.Log($"[TurnProcessor] 勝者決定: {winnerName} (重さ: {maxWeight}kg)");

            // GameFlowManagerにゲーム終了を通知（8.2: アニメーション再生を待機、8.3: リザルト画面表示）
            if (GameFlowManager.Instance != null)
            {
                await GameFlowManager.Instance.EndGame(winner.Value, isMorning);
            }
            else
            {
                Debug.LogError("[TurnProcessor] GameFlowManager.Instance が null です");
            }
        }
        else
        {
            Debug.Log("[TurnProcessor] 勝者なし、ゲーム続行");
        }

        return winner;
    }
}