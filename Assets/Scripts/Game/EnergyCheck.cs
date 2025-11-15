using UnityEngine;
using Fusion;

/// <summary>
/// 元気チェック用のユーティリティクラス
/// 行動選択時に元気が足りるかどうかをチェックする
/// </summary>
public static class EnergyCheck
{
    /// <summary>
    /// 指定した行動が実行可能かどうかをチェック
    /// 午後の場合は、午前の行動も考慮して合計で元気がマイナスにならないかをチェック
    /// 連続使用ペナルティも考慮する
    /// </summary>
    /// <param name="currentEnergy">現在の元気</param>
    /// <param name="actionType">チェックする行動タイプ</param>
    /// <param name="isMorning">午前の選択かどうか</param>
    /// <param name="morningAction">既に選択済みの午前の行動（午後の場合のみ使用）</param>
    /// <param name="previousAction">前の行動（午前なら前日午後、午後なら今日午前）</param>
    /// <param name="gameParams">ゲームパラメーター</param>
    /// <returns>実行可能ならtrue</returns>
    public static bool CanPerformAction(int currentEnergy, ActionType actionType, bool isMorning, ActionData? morningAction, ActionData? previousAction, GameParameters gameParams)
    {
        if (gameParams == null) return true;

        int totalEnergyCost = 0;

        UnityEngine.Debug.Log($"[EnergyCheck] CanPerformAction開始: actionType={actionType}, currentEnergy={currentEnergy}, isMorning={isMorning}");
        UnityEngine.Debug.Log($"[EnergyCheck] morningAction={morningAction?.Type}, previousAction={previousAction?.Type}");

        // 午後の場合は、午前の行動の元気消費も加算
        if (!isMorning && morningAction.HasValue && morningAction.Value.Type != ActionType.None)
        {
            int morningCost = GetActionEnergyCost(morningAction.Value, gameParams);
            totalEnergyCost += morningCost;
            UnityEngine.Debug.Log($"[EnergyCheck] 午後選択: 午前の行動コスト {morningCost} を加算 → totalEnergyCost={totalEnergyCost}");

            // 午前の行動が前日午後と連続使用かチェック（午後選択時のみ、午前の連続ペナルティを考慮）
            if (previousAction.HasValue && morningAction.Value.Type == previousAction.Value.Type && morningAction.Value.Type != ActionType.None)
            {
                totalEnergyCost -= gameParams.ConsecutiveActionPenalty; // -20
                UnityEngine.Debug.Log($"[EnergyCheck] 午前の行動が前日午後と連続: ペナルティ-{gameParams.ConsecutiveActionPenalty} → totalEnergyCost={totalEnergyCost}");
            }
        }

        // チェック対象の行動の元気消費を加算
        int actionCost = GetActionEnergyCostByType(actionType, gameParams);
        totalEnergyCost += actionCost;
        UnityEngine.Debug.Log($"[EnergyCheck] 行動コスト {actionCost} を加算 → totalEnergyCost={totalEnergyCost}");

        // 連続使用ペナルティをチェック
        ActionType compareAction = isMorning
            ? (previousAction.HasValue ? previousAction.Value.Type : ActionType.None)  // 午前: 前日午後と比較
            : (morningAction.HasValue ? morningAction.Value.Type : ActionType.None);   // 午後: 今日午前と比較

        UnityEngine.Debug.Log($"[EnergyCheck] 連続使用チェック: compareAction={compareAction}");

        if (actionType == compareAction && actionType != ActionType.None)
        {
            totalEnergyCost -= gameParams.ConsecutiveActionPenalty; // -20
            UnityEngine.Debug.Log($"[EnergyCheck] 連続使用ペナルティ: -{gameParams.ConsecutiveActionPenalty} → totalEnergyCost={totalEnergyCost}");
        }

        // 現在の元気 + 合計消費量 >= 0 ならOK
        bool result = (currentEnergy + totalEnergyCost) >= 0;
        UnityEngine.Debug.Log($"[EnergyCheck] 結果: {currentEnergy} + {totalEnergyCost} = {currentEnergy + totalEnergyCost} >= 0 → {result}");
        return result;
    }

    /// <summary>
    /// 特殊能力が実行可能かどうかをチェック
    /// 午後の場合は、午前の行動も考慮して合計で元気がマイナスにならないかをチェック
    /// 連続使用ペナルティも考慮する
    /// </summary>
    /// <param name="currentEnergy">現在の元気</param>
    /// <param name="abilityName">特殊能力名</param>
    /// <param name="isMorning">午前の選択かどうか</param>
    /// <param name="morningAction">既に選択済みの午前の行動（午後の場合のみ使用）</param>
    /// <param name="previousAction">前の行動（午前なら前日午後、午後なら今日午前）</param>
    /// <param name="gameParams">ゲームパラメーター</param>
    /// <returns>実行可能ならtrue</returns>
    public static bool CanUseSpecialAbility(int currentEnergy, string abilityName, bool isMorning, ActionData? morningAction, ActionData? previousAction, GameParameters gameParams)
    {
        if (gameParams == null) return true;

        int totalEnergyCost = 0;

        // 午後の場合は、午前の行動の元気消費も加算
        if (!isMorning && morningAction.HasValue && morningAction.Value.Type != ActionType.None)
        {
            totalEnergyCost += GetActionEnergyCost(morningAction.Value, gameParams);

            // 午前の行動が前日午後と連続使用かチェック（午後選択時のみ、午前の連続ペナルティを考慮）
            if (previousAction.HasValue && morningAction.Value.Type == previousAction.Value.Type && morningAction.Value.Type != ActionType.None)
            {
                totalEnergyCost -= gameParams.ConsecutiveActionPenalty; // -20
            }
        }

        // 特殊能力の元気消費を加算
        totalEnergyCost += GetSpecialAbilityEnergyCost(abilityName, gameParams);

        // 連続使用ペナルティをチェック（特殊能力は常にActionType.SpecialAbility）
        // 特殊能力の連続使用は能力名で判定する必要がある
        bool isConsecutive = false;
        if (isMorning && previousAction.HasValue)
        {
            // 午前: 前日午後が特殊能力で、同じ能力名かチェック
            if (previousAction.Value.Type == ActionType.SpecialAbility && previousAction.Value.SpecialAbilityName.ToString() == abilityName)
            {
                isConsecutive = true;
            }
        }
        else if (!isMorning && morningAction.HasValue)
        {
            // 午後: 今日午前が特殊能力で、同じ能力名かチェック
            if (morningAction.Value.Type == ActionType.SpecialAbility && morningAction.Value.SpecialAbilityName.ToString() == abilityName)
            {
                isConsecutive = true;
            }
        }

        if (isConsecutive)
        {
            totalEnergyCost -= gameParams.ConsecutiveActionPenalty; // -20
        }

        // 現在の元気 + 合計消費量 >= 0 ならOK
        return (currentEnergy + totalEnergyCost) >= 0;
    }

    /// <summary>
    /// ActionDataから元気消費量を取得
    /// </summary>
    private static int GetActionEnergyCost(ActionData action, GameParameters gameParams)
    {
        if (action.Type == ActionType.SpecialAbility)
        {
            return GetSpecialAbilityEnergyCost(action.SpecialAbilityName.ToString(), gameParams);
        }
        else
        {
            return GetActionEnergyCostByType(action.Type, gameParams);
        }
    }

    /// <summary>
    /// ActionTypeから元気消費量を取得
    /// </summary>
    private static int GetActionEnergyCostByType(ActionType actionType, GameParameters gameParams)
    {
        switch (actionType)
        {
            case ActionType.Eat:
                return gameParams.EatEnergyChange; // -15

            case ActionType.Sleep:
                return gameParams.SleepEnergyChange; // +50 (回復)

            case ActionType.Play:
                return gameParams.PlayEnergyChange; // -30

            case ActionType.Clinic:
                return gameParams.ClinicEnergyChange; // -20

            default:
                return 0;
        }
    }

    /// <summary>
    /// 特殊能力名から元気消費量を取得
    /// </summary>
    private static int GetSpecialAbilityEnergyCost(string abilityName, GameParameters gameParams)
    {
        if (string.IsNullOrEmpty(abilityName)) return 0;

        if (abilityName == "Gaishoku")
        {
            return gameParams.GaishokuEnergyChange; // -15
        }
        else if (abilityName == "Kintre")
        {
            return gameParams.KintreEnergyCost; // -120
        }
        else if (abilityName == "Gamushura")
        {
            return gameParams.GamushuraEnergyCost; // -40
        }
        else if (abilityName == "Benkyou")
        {
            return gameParams.BenkyouEnergyCost; // -25
        }
        else if (abilityName == "Jukusui")
        {
            return gameParams.JukusuiEnergyBonus; // +20 (回復)
        }
        else if (abilityName == "Dokagui")
        {
            return gameParams.DokaguiEnergyCost; // -15
        }
        else
        {
            return 0; // 不明な能力は0
        }
    }
}
