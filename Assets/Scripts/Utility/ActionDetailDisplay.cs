using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 行動の詳細情報（ツールチップ用テキスト）を生成するユーティリティクラス
/// </summary>
public static class ActionDetailDisplay
{
    /// <summary>
    /// 行動の詳細テキストを生成（ホバー時のツールチップ用）
    /// </summary>
    public static string GenerateDetailText(
        ActionType action,
        string specialAbilityName,
        UyopyonState state,
        PlayerActionData actionData,
        GameParameters gameParams)
    {
        List<string> details = new List<string>();

        // 午前か午後かを判定
        bool isMorning = (actionData.MorningAction.Type == ActionType.None);

        // 1. 最終的な効果（タイトル）
        int finalEnergy = ActionCalculator.CalculateEnergyChange(action, specialAbilityName, state, actionData, gameParams, isMorning);
        int finalWeight = ActionCalculator.CalculateWeightChange(action, specialAbilityName, state, gameParams);

        details.Add("【最終効果】");
        if (finalEnergy != 0) details.Add($"元気: {FormatChange(finalEnergy)}");
        if (finalWeight != 0) details.Add($"重さ: {FormatChange(finalWeight)}");
        details.Add("");

        // 2. 内訳
        details.Add("【内訳】");
        AddBreakdown(details, action, specialAbilityName, state, actionData, gameParams, isMorning);
        details.Add("");

        // 3. 発症確率・その他情報
        AddAdditionalInfo(details, action, specialAbilityName, state, gameParams);

        return string.Join("\n", details);
    }

    /// <summary>
    /// 効果の内訳を追加
    /// </summary>
    private static void AddBreakdown(
        List<string> details,
        ActionType action,
        string specialAbilityName,
        UyopyonState state,
        PlayerActionData actionData,
        GameParameters gameParams,
        bool isMorning)
    {
        // 元気の内訳
        int baseEnergy = GetBaseEnergy(action, specialAbilityName, gameParams);
        if (baseEnergy != 0)
        {
            details.Add($"・基本元気: {FormatChange(baseEnergy)}");

            // バフ（ねむる、じゅくすいのみ）
            if (action == ActionType.Sleep || specialAbilityName == "Jukusui")
            {
                if (state.PlayBuffEnergy != 0)
                {
                    details.Add($"・元気バフ: {FormatChange(state.PlayBuffEnergy)}");
                }
            }

            // 状態異常（睡眠時無呼吸症候群）
            if (action == ActionType.Sleep && state.HasStatusAilment(StatusAilment.SleepApnea))
            {
                details.Add($"・睡眠時無呼吸: {FormatChange(gameParams.SleepApneaRecoveryReduction)}");
            }

            // 連続使用デバフ
            if (IsConsecutive(action, specialAbilityName, actionData, isMorning))
            {
                details.Add($"・連続使用: {FormatChange(-gameParams.ConsecutiveActionPenalty)}");
            }
        }

        // 重さの内訳
        int baseWeight = GetBaseWeight(action, specialAbilityName, gameParams);
        if (baseWeight != 0)
        {
            details.Add($"・基本重さ: {FormatChange(baseWeight)}");

            // バフ（たべる、がいしょく、どかぐいのみ）
            if (action == ActionType.Eat || specialAbilityName == "Gaishoku" || specialAbilityName == "Dokagui")
            {
                if (state.PlayBuffWeight != 0)
                {
                    details.Add($"・重さバフ: {FormatChange(state.PlayBuffWeight)}");
                }
            }
        }

        // バフ増加（あそぶのみ）
        if (action == ActionType.Play)
        {
            details.Add($"・重さバフ増加: +{gameParams.PlayWeightBuffIncrement}");
            details.Add($"・元気バフ増加: +{gameParams.PlayEnergyBuffIncrement}");
        }
    }

    /// <summary>
    /// 追加情報を追加（発症確率、中断確率など）
    /// </summary>
    private static void AddAdditionalInfo(
        List<string> details,
        ActionType action,
        string specialAbilityName,
        UyopyonState state,
        GameParameters gameParams)
    {
        // たべる、がいしょく：病気・ケガの発症確率
        if (action == ActionType.Eat || specialAbilityName == "Gaishoku")
        {
            var (sicknessProbability, injuryProbability) = ActionCalculator.CalculateAilmentProbability(state, gameParams);
            details.Add("【発症確率】");
            details.Add($"病気: {sicknessProbability:F1}%");
            details.Add($"ケガ: {injuryProbability:F1}%");
        }

        // あそぶ：ぎっくり腰の中断確率
        if (action == ActionType.Play && state.HasStatusAilment(StatusAilment.BackPain))
        {
            float cancelProbability = gameParams.BackPainCancelProbability * 100;
            details.Add("【その他】");
            details.Add($"中断確率: {cancelProbability:F0}%（ぎっくり腰）");
        }

        // つういん：状態異常治療
        if (action == ActionType.Clinic)
        {
            details.Add("【効果】");
            details.Add("全ての状態異常を治療");
        }
    }

    // === ヘルパーメソッド ===

    private static int GetBaseEnergy(ActionType action, string specialAbilityName, GameParameters gameParams)
    {
        if (action == ActionType.SpecialAbility)
        {
            switch (specialAbilityName)
            {
                case "Gaishoku": return gameParams.GaishokuEnergyChange;
                case "Kintre": return gameParams.KintreEnergyCost;
                case "Gamushara": return gameParams.GamushuraEnergyCost;
                case "Benkyou": return gameParams.BenkyouEnergyCost;
                case "Jukusui": return gameParams.JukusuiEnergyBonus;
                case "Dokagui": return gameParams.DokaguiEnergyCost;
                default: return 0;
            }
        }

        switch (action)
        {
            case ActionType.Eat: return gameParams.EatEnergyChange;
            case ActionType.Sleep: return gameParams.SleepEnergyChange;
            case ActionType.Play: return gameParams.PlayEnergyChange;
            case ActionType.Clinic: return gameParams.ClinicEnergyChange;
            default: return 0;
        }
    }

    private static int GetBaseWeight(ActionType action, string specialAbilityName, GameParameters gameParams)
    {
        if (action == ActionType.SpecialAbility)
        {
            switch (specialAbilityName)
            {
                case "Gaishoku": return gameParams.GaishokuWeightChange;
                case "Dokagui": return gameParams.DokaguiWeightBonus;
                default: return 0;
            }
        }

        switch (action)
        {
            case ActionType.Eat: return gameParams.EatWeightChange;
            default: return 0;
        }
    }

    private static bool IsConsecutive(
        ActionType action,
        string specialAbilityName,
        PlayerActionData actionData,
        bool isMorning)
    {
        if (action == ActionType.None) return false;

        ActionType compareAction;
        string compareAbilityName = "";

        if (isMorning)
        {
            compareAction = actionData.LastAfternoonAction.Type;
            if (compareAction == ActionType.SpecialAbility)
            {
                compareAbilityName = actionData.LastAfternoonAction.SpecialAbilityName.ToString();
            }
        }
        else
        {
            compareAction = actionData.MorningAction.Type;
            if (compareAction == ActionType.SpecialAbility)
            {
                compareAbilityName = actionData.MorningAction.SpecialAbilityName.ToString();
            }
        }

        if (action != ActionType.SpecialAbility)
        {
            return action == compareAction;
        }

        return compareAction == ActionType.SpecialAbility && specialAbilityName == compareAbilityName;
    }

    private static string FormatChange(int value)
    {
        if (value > 0) return $"+{value}";
        else if (value < 0) return value.ToString();
        else return "±0";
    }
}
