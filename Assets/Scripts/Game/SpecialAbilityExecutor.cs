using Fusion;
using UnityEngine;

/// <summary>
/// 特殊能力の実行ロジックを管理するクラス
/// </summary>
public class SpecialAbilityExecutor
{
    private readonly GameParameters gameParams;
    private readonly TurnProcessor turnProcessor;

    public SpecialAbilityExecutor(GameParameters gameParams, TurnProcessor turnProcessor)
    {
        this.gameParams = gameParams;
        this.turnProcessor = turnProcessor;
    }

    /// <summary>
    /// 7.3 特殊能力: がいしょくの実行
    /// 効果: たべると同じ効果（元気-15、重さ+50 + PlayBuffWeight）
    /// ジャンル: Paper（パー）
    /// </summary>
    public void ExecuteGaishoku(PlayerRef player, UyopyonState state, string playerName)
    {
        // たべると同じ効果（BuffMultiplier適用）
        int energyChange = gameParams.EatEnergyChange;
        int weightChange = (int)((gameParams.EatWeightChange + state.PlayBuffWeight) * state.BuffMultiplier);

        state.Energy += energyChange;
        state.Weight += weightChange;

        // ログに追加
        string log = $"{playerName}は「がいしょく」を実行！元気{turnProcessor.FormatNumber(energyChange)}、重さ{turnProcessor.FormatNumber(weightChange)}";
        turnProcessor.AddLog(log);

        Debug.Log($"[SpecialAbility] {log}");

        // 病気発症判定
        turnProcessor.CheckSickness(player, state.Weight);
    }

    /// <summary>
    /// 7.4 特殊能力: がむしゃらの実行
    /// 効果: 元気-40、4パターンからランダム選択
    /// ジャンル: None（なし）
    /// </summary>
    public void ExecuteGamushara(PlayerRef player, UyopyonState state, string playerName)
    {
        // 元気減少
        state.Energy -= 40;

        // 4パターンからランダム選択
        int pattern = UnityEngine.Random.Range(0, 4);
        string effectLog = "";

        switch (pattern)
        {
            case 0:
                // パターン1: たべるの重さ増加 × 1.2倍
                int weightChange1 = (int)((gameParams.EatWeightChange + state.PlayBuffWeight) * state.BuffMultiplier * 1.2f);
                state.Weight += weightChange1;
                effectLog = $"がむしゃらにたべた！重さ{turnProcessor.FormatNumber(weightChange1)}";
                break;

            case 1:
                // パターン2: ねむるの元気増加 × 1.2倍
                int energyChange2 = (int)((gameParams.SleepEnergyChange + state.PlayBuffEnergy) * state.BuffMultiplier * 1.2f);
                state.Energy += energyChange2;
                effectLog = $"がむしゃらにねた！元気{turnProcessor.FormatNumber(energyChange2)}";
                break;

            case 2:
                // パターン3: たべる時の重さバフ +15, ねむる時の元気バフ +15
                state.PlayBuffWeight += 15;
                state.PlayBuffEnergy += 15;
                effectLog = "がむしゃらにあそんだ！たべる時の重さバフ+15、ねむる時の元気バフ+15";
                break;

            case 3:
                // パターン4: MIX - 重さ増加×0.5、元気増加×0.5、バフ各+8
                int weightChange3 = (int)((gameParams.EatWeightChange + state.PlayBuffWeight) * state.BuffMultiplier * 0.5f);
                int energyChange4 = (int)((gameParams.SleepEnergyChange + state.PlayBuffEnergy) * state.BuffMultiplier * 0.5f);
                state.Weight += weightChange3;
                state.Energy += energyChange4;
                state.PlayBuffWeight += 8;
                state.PlayBuffEnergy += 8;
                effectLog = $"がむしゃらにたべてねてあそんだ！元気{turnProcessor.FormatNumber(energyChange4)}、重さ{turnProcessor.FormatNumber(weightChange3)}、たべる時の重さバフ+8、ねむる時の元気バフ+8";
                break;
        }

        // ログに追加
        string log = $"{playerName}は「がむしゃら」を実行！元気-40、{effectLog}";
        turnProcessor.AddLog(log);

        Debug.Log($"[SpecialAbility] {log}");
    }

    /// <summary>
    /// 7.5 特殊能力: べんきょうの実行
    /// 効果: 元気-25、たべる時の重さバフ +10（固定、連続回数に関わらず）
    /// ジャンル: Scissors（チョキ）
    /// </summary>
    public void ExecuteBenkyou(PlayerRef player, UyopyonState state, string playerName)
    {
        // 元気減少
        state.Energy -= 25;

        // 固定で+10（連続回数に関わらず毎回+10）
        state.PlayBuffWeight += 10;

        // 連続カウンタを増加
        state.StudyCombo += 1;

        // ログに追加
        string log;
        if (state.StudyCombo > 1)
        {
            log = $"{playerName}は「べんきょう」を実行！元気-25、たべる時の重さバフ+10（{state.StudyCombo}回連続）";
        }
        else
        {
            log = $"{playerName}は「べんきょう」を実行！元気-25、たべる時の重さバフ+10";
        }
        turnProcessor.AddLog(log);

        Debug.Log($"[SpecialAbility] {log}");
    }

    /// <summary>
    /// 7.6 特殊能力: じゅくすいの実行
    /// 効果: ねむるの元気増加 +20
    /// ジャンル: Paper（パー）
    /// 選択条件: 重さ合計が奇数
    /// </summary>
    public void ExecuteJukusui(PlayerRef player, UyopyonState state, string playerName)
    {
        // ねむるの効果 +20（BuffMultiplier適用）
        int energyChange = (int)((gameParams.SleepEnergyChange + 20 + state.PlayBuffEnergy) * state.BuffMultiplier);

        state.Energy += energyChange;

        // ログに追加
        string log = $"{playerName}は「じゅくすい」を実行！元気{turnProcessor.FormatNumber(energyChange)}";
        turnProcessor.AddLog(log);

        Debug.Log($"[SpecialAbility] {log}");
    }

    /// <summary>
    /// 7.6 特殊能力: どかぐいの実行
    /// 効果: たべるの重さ増加 +30
    /// ジャンル: Rock（グー）
    /// 選択条件: 重さ合計が偶数
    /// </summary>
    public void ExecuteDokagui(PlayerRef player, UyopyonState state, string playerName)
    {
        // たべるの効果 +30（BuffMultiplier適用）
        int energyChange = gameParams.EatEnergyChange;
        int weightChange = (int)((gameParams.EatWeightChange + 30 + state.PlayBuffWeight) * state.BuffMultiplier);

        state.Energy += energyChange;
        state.Weight += weightChange;

        // ログに追加
        string log = $"{playerName}は「どかぐい」を実行！元気{turnProcessor.FormatNumber(energyChange)}、重さ{turnProcessor.FormatNumber(weightChange)}";
        turnProcessor.AddLog(log);

        Debug.Log($"[SpecialAbility] {log}");

        // 病気発症判定
        turnProcessor.CheckSickness(player, state.Weight);
    }

    /// <summary>
    /// 7.7 特殊能力: きんとれの実行
    /// 効果: 元気-120、重さ半減、BuffMultiplier = 1.5
    /// ジャンル: Scissors（チョキ）
    /// </summary>
    public void ExecuteKintre(PlayerRef player, UyopyonState state, string playerName)
    {
        // 元気減少
        state.Energy -= 120;

        // 重さを半分に
        int oldWeight = state.Weight;
        state.Weight = (int)(state.Weight * 0.5f);
        int weightChange = state.Weight - oldWeight;

        // バフ倍率を設定
        state.BuffMultiplier = 1.5f;

        // ログに追加
        string log = $"{playerName}は「きんとれ」を実行！元気-120、重さ{turnProcessor.FormatNumber(weightChange)}、今後の効果が1.5倍に！";
        turnProcessor.AddLog(log);

        Debug.Log($"[SpecialAbility] {log}");
    }
}
