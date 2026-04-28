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
        ActionData? localMorningAction = null,
        int pendingBuffEnergy = 0)
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
            energyChange += state.PlayBuffEnergy + pendingBuffEnergy;
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
        GameParameters gameParams,
        int pendingBuffWeight = 0)
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
            weightChange += state.PlayBuffWeight + pendingBuffWeight;
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
    /// 午前行動によって午後計算に適用される予定バフ変化量を返す（選択フェーズUI表示用）
    /// がむしゃらはランダムのため予測不可として0を返す
    /// </summary>
    public static (int weightBuffDelta, int energyBuffDelta)
        GetPendingMorningBuffDelta(ActionData? morningAction, UyopyonState state, GameParameters gameParams)
    {
        if (!morningAction.HasValue) return (0, 0);

        var action = morningAction.Value;
        if (action.Type == ActionType.Play)
            return (gameParams.PlayWeightBuffIncrement, gameParams.PlayEnergyBuffIncrement);

        if (action.Type == ActionType.SpecialAbility)
        {
            string abilityName = action.SpecialAbilityName.ToString();
            switch (abilityName)
            {
                case "Kintre":
                    // PlayBuff = PlayBuff*2 + base なので、増加分 = PlayBuff + base
                    return (state.PlayBuffWeight + gameParams.EatWeightChange,
                            state.PlayBuffEnergy + gameParams.SleepEnergyChange);
                case "Benkyou":
                    int weightBuff = 30 * (state.StudyCombo + 1);
                    return (weightBuff, 0);
            }
        }

        return (0, 0);
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
    /// きんとれによるバフ倍率変化を計算（現在の倍率の2.0倍）
    /// </summary>
    public static float CalculateKintreBuffMultiplierChange(float currentMultiplier)
    {
        return currentMultiplier * 2.0f;
    }

    /// <summary>
    /// きんとれによるバフ増減量を計算（PlayBuff = PlayBuff*2 + base による増加分）
    /// </summary>
    public static (int energyBuff, int weightBuff) CalculateKintreBuffChange(
        UyopyonState state,
        GameParameters gameParams)
    {
        return (state.PlayBuffEnergy + gameParams.SleepEnergyChange,
                state.PlayBuffWeight + gameParams.EatWeightChange);
    }


    /// <summary>
    /// べんきょうによるバフ増加量を計算（30 × 連続使用数）
    /// </summary>
    public static int CalculateBenkyouBuffIncrement(int studyCombo)
    {
        return 30 * studyCombo;
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
            // 9.3修正: じゅくすいは「ねむるの効果 + 100」なので、SleepEnergyChange + JukusuiEnergyBonusを返す
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
