using UnityEngine;

/// <summary>
/// 行動の効果を計算するユーティリティクラス
/// 連続使用デバフ、バフ、状態異常などを考慮した最終的な効果量を計算する
/// </summary>
public static class ActionCalculator
{
    /// <summary>
    /// 行動による元気変化を計算（連続使用デバフ、バフ、状態異常を含む）
    /// 9.3修正: BuffMultiplierの適用を追加
    /// 9.3修正: 選択フェーズ用にローカルの午前行動を渡せるようにlocalMorningActionパラメータを追加
    /// </summary>
    public static int CalculateEnergyChange(
        ActionType action,
        string specialAbilityName,
        UyopyonState state,
        PlayerActionData actionData,
        GameParameters gameParams,
        bool isMorning,
        ActionData? localMorningAction = null)
    {
        int energyChange = 0;

        // 基本の元気変化
        if (action == ActionType.SpecialAbility)
        {
            energyChange = GetSpecialAbilityBaseEnergy(specialAbilityName, gameParams);
        }
        else
        {
            energyChange = GetBaseEnergy(action, gameParams);
        }

        // バフの適用（ねむる、じゅくすいのみ）
        if (action == ActionType.Sleep || specialAbilityName == "Jukusui")
        {
            energyChange += state.PlayBuffEnergy;
        }

        // BuffMultiplierの適用（ねむる、じゅくすい、がいしょく、どかぐい用）
        if (action == ActionType.Sleep || specialAbilityName == "Jukusui" ||
            specialAbilityName == "Gaishoku" || specialAbilityName == "Dokagui")
        {
            energyChange = (int)(energyChange * state.BuffMultiplier);
        }

        // 状態異常の影響（睡眠時無呼吸症候群）
        if (action == ActionType.Sleep && state.HasStatusAilment(StatusAilment.SleepApnea))
        {
            energyChange += gameParams.SleepApneaRecoveryReduction; // -20
        }

        // 連続使用ペナルティ
        if (IsConsecutiveAction(action, specialAbilityName, actionData, isMorning, localMorningAction))
        {
            energyChange -= gameParams.ConsecutiveActionPenalty; // -20
        }

        return energyChange;
    }

    /// <summary>
    /// 行動による重さ変化を計算（バフを含む）
    /// 9.3修正: BuffMultiplierの適用を追加
    /// </summary>
    public static int CalculateWeightChange(
        ActionType action,
        string specialAbilityName,
        UyopyonState state,
        GameParameters gameParams)
    {
        int weightChange = 0;

        // 基本の重さ変化
        if (action == ActionType.SpecialAbility)
        {
            weightChange = GetSpecialAbilityBaseWeight(specialAbilityName, gameParams);
        }
        else
        {
            weightChange = GetBaseWeight(action, gameParams);
        }

        // バフの適用（たべる、がいしょく、どかぐいのみ）
        if (action == ActionType.Eat || specialAbilityName == "Gaishoku" || specialAbilityName == "Dokagui")
        {
            weightChange += state.PlayBuffWeight;
        }

        // BuffMultiplierの適用（たべる、がいしょく、どかぐい用）
        if (action == ActionType.Eat || specialAbilityName == "Gaishoku" || specialAbilityName == "Dokagui")
        {
            weightChange = (int)(weightChange * state.BuffMultiplier);
        }

        return weightChange;
    }

    /// <summary>
    /// 病気・ケガの発症確率を計算
    /// </summary>
    public static (float sicknessProbability, float injuryProbability) CalculateAilmentProbability(
        UyopyonState state,
        GameParameters gameParams)
    {
        float sicknessProbability = state.Weight * gameParams.SicknessProbabilityPerWeight;
        float injuryProbability = state.Weight * gameParams.InjuryProbabilityPerWeight;
        return (sicknessProbability, injuryProbability);
    }

    /// <summary>
    /// 確率を％表示用の文字列にフォーマット（小数点第1位まで）
    /// </summary>
    public static string FormatProbability(float probability)
    {
        return $"{probability:F1}%";
    }

    /// <summary>
    /// 行動によるバフ増加量を取得（あそぶ行動用）
    /// </summary>
    public static (int energyBuff, int weightBuff) GetBuffIncrement(GameParameters gameParams)
    {
        return (gameParams.PlayEnergyBuffIncrement, gameParams.PlayWeightBuffIncrement);
    }

    /// <summary>
    /// 連続使用かどうかを判定
    /// 9.3修正: 選択フェーズ用にローカルの午前行動を渡せるようにlocalMorningActionパラメータを追加
    /// </summary>
    private static bool IsConsecutiveAction(
        ActionType action,
        string specialAbilityName,
        PlayerActionData actionData,
        bool isMorning,
        ActionData? localMorningAction = null)
    {
        if (action == ActionType.None) return false;

        ActionType compareAction;
        string compareAbilityName = "";

        if (isMorning)
        {
            // 午前：前日午後と比較
            compareAction = actionData.LastAfternoonAction.Type;
            if (compareAction == ActionType.SpecialAbility)
            {
                compareAbilityName = actionData.LastAfternoonAction.SpecialAbilityName.ToString();
            }
        }
        else
        {
            // 午後：今日午前と比較
            // 9.3修正: localMorningActionが渡された場合はそれを優先（選択フェーズ用）
            if (localMorningAction.HasValue)
            {
                compareAction = localMorningAction.Value.Type;
                if (compareAction == ActionType.SpecialAbility)
                {
                    compareAbilityName = localMorningAction.Value.SpecialAbilityName.ToString();
                }
            }
            else
            {
                // フォールバック：ネットワーク同期された午前の行動を使用
                compareAction = actionData.MorningAction.Type;
                if (compareAction == ActionType.SpecialAbility)
                {
                    compareAbilityName = actionData.MorningAction.SpecialAbilityName.ToString();
                }
            }
        }

        bool isConsecutive;
        // 通常行動の場合
        if (action != ActionType.SpecialAbility)
        {
            isConsecutive = action == compareAction;
        }
        else
        {
            // 特殊能力の場合：能力名まで一致する必要がある
            isConsecutive = compareAction == ActionType.SpecialAbility && specialAbilityName == compareAbilityName;
        }

        DebugLogger.Log($"[ActionCalculator] IsConsecutive: isMorning={isMorning}, action={action}, specialAbility={specialAbilityName}, compareAction={compareAction}, compareAbility={compareAbilityName}, result={isConsecutive}");
        return isConsecutive;
    }

    
    // === 特殊能力用の計算メソッド ===

    /// <summary>
    /// きんとれによる重さ変化量を計算（現在の重さの半分を減らす）
    /// </summary>
    public static int CalculateKintreWeightChange(int currentWeight)
    {
        return -(int)(currentWeight * 0.5f);
    }

    /// <summary>
    /// きんとれによるバフ倍率変化を計算（現在の倍率の2.5倍）
    /// </summary>
    public static float CalculateKintreBuffMultiplierChange(float currentMultiplier)
    {
        return currentMultiplier * 2.5f;
    }

    /// <summary>
    /// きんとれによるバフ増減量を計算（新しい倍率での増加量）
    /// </summary>
    public static (int energyBuff, int weightBuff) CalculateKintreBuffChange(
        UyopyonState state,
        GameParameters gameParams)
    {
        // 現在のバフ量を2.5倍にするための増加量（= 現在のバフ量の1.5倍）
        int energyBuffIncrement = (int)(state.PlayBuffEnergy * 1.5f);
        int weightBuffIncrement = (int)(state.PlayBuffWeight * 1.5f);
        
        return (energyBuffIncrement, weightBuffIncrement);
    }


    /// <summary>
    /// べんきょうによるバフ増加量を計算（15 × 連続使用数）
    /// </summary>
    public static int CalculateBenkyouBuffIncrement(int studyCombo)
    {
        return 15 * studyCombo;
    }

    /// <summary>
    /// 特殊能力による病気発症確率を計算（がいしょく、どかぐい用）
    /// たべると同じ計算式
    /// </summary>
    public static float CalculateSpecialAbilitySicknessProbability(
        UyopyonState state,
        GameParameters gameParams)
    {
        return state.Weight * gameParams.SicknessProbabilityPerWeight;
    }

// === 基本値取得メソッド ===

    private static int GetBaseEnergy(ActionType action, GameParameters gameParams)
    {
        switch (action)
        {
            case ActionType.Eat: return gameParams.EatEnergyChange;
            case ActionType.Sleep: return gameParams.SleepEnergyChange;
            case ActionType.Play: return gameParams.PlayEnergyChange;
            case ActionType.Clinic: return gameParams.ClinicEnergyChange;
            default: return 0;
        }
    }

    private static int GetBaseWeight(ActionType action, GameParameters gameParams)
    {
        switch (action)
        {
            case ActionType.Eat: return gameParams.EatWeightChange;
            default: return 0;
        }
    }

    private static int GetSpecialAbilityBaseEnergy(string abilityName, GameParameters gameParams)
    {
        switch (abilityName)
        {
            case "Gaishoku": return gameParams.GaishokuEnergyChange;
            case "Kintre": return gameParams.KintreEnergyCost;
            case "Gamushara": return gameParams.GamushuraEnergyCost;
            case "Benkyou": return gameParams.BenkyouEnergyCost;
            // 9.3修正: じゅくすいは「ねむるの効果 + 20」なので、SleepEnergyChange + JukusuiEnergyBonusを返す
            case "Jukusui": return gameParams.SleepEnergyChange + gameParams.JukusuiEnergyBonus;
            case "Dokagui": return gameParams.DokaguiEnergyCost;
            default: return 0;
        }
    }

    private static int GetSpecialAbilityBaseWeight(string abilityName, GameParameters gameParams)
    {
        switch (abilityName)
        {
            case "Gaishoku": return gameParams.GaishokuWeightChange;
            // 9.3修正: どかぐいは「たべるの重さ増加 + 30」なので、EatWeightChange + DokaguiWeightBonusを返す
            case "Dokagui": return gameParams.EatWeightChange + gameParams.DokaguiWeightBonus;
            default: return 0;
        }
    }
}
