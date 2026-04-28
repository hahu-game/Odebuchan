using UnityEngine;

/// <summary>
/// ゲーム全体のパラメーターを管理するScriptableObject
/// Inspector上で編集可能で、各スクリプトから参照される
/// </summary>
[CreateAssetMenu(fileName = "GameParameters", menuName = "Odebuchan/GameParameters")]
public class GameParameters : ScriptableObject
{
    [Header("=== 基本設定 ===")]
    [Tooltip("ゲーム開始時の重さ（kg）")]
    public int InitialWeight;

    [Tooltip("ゲーム開始時の元気（HP）")]
    public int InitialEnergy;

    [Tooltip("進化に必要な重さ（kg）")]
    public int EvolutionWeightThreshold;

    [Tooltip("勝利条件の重さ（kg）")]
    public int VictoryWeightThreshold;

    [Header("=== 行動パラメーター ===")]
    [Header("たべる")]
    public int EatEnergyChange;
    public int EatWeightChange;

    [Header("ねむる")]
    public int SleepEnergyChange;

    [Header("あそぶ")]
    public int PlayEnergyChange;
    public int PlayWeightBuffIncrement;
    public int PlayEnergyBuffIncrement;

    [Header("つういん")]
    public int ClinicEnergyChange;

    [Header("連続使用ペナルティ")]
    public int ConsecutiveActionPenalty;

    [Header("=== 状態異常 ===")]
    [Header("発症確率")]
    [Tooltip("病気・ケガの発症確率（重さ÷10 = %）")]
    public float SicknessProbabilityPerWeight;
    public float InjuryProbabilityPerWeight;

    [Header("病気の種類別確率")]
    public float SicknessSleepApneaProbability;
    public float SicknessDiabetesProbability;

    [Header("ケガの種類別確率")]
    public float InjuryBackPainProbability;
    public float InjuryHeatstrokeProbability;

    [Header("状態異常の効果")]
    public int SleepApneaRecoveryReduction;
    public int DiabetesMorningWeightChange;
    public int DiabetesMorningEnergyChange;
    public float BackPainCancelProbability;
    public float HeatstrokeForcedClinicProbability;
    
    [Header("=== ジャンケン効果 ===")]
    public int JankenWinEnergyBase;
    public int JankenLoseEnergyBase;
    public int JankenEnergyIncrementPerDay;
    public int JankenBuffDebuffAmount;

    [Header("=== 特殊能力パラメーター ===")]
    [Header("がいしょく")]
    [Tooltip("がいしょくはたべると同じ効果だが、ジャンルがパー")]
    public int GaishokuEnergyChange;
    public int GaishokuWeightChange;

    [Header("きんとれ")]
    public int KintreEnergyCost;
    public float KintreWeightMultiplier;
    public float KintreBuffMultiplier;

    [Header("がむしゃら")]
    public int GamushuraEnergyCost;
    public float GamushuraWeightMultiplier;
    public float GamushuraEnergyMultiplier;
    public int GamushuraBuffWeightIncrement;
    public int GamushuraBuffEnergyIncrement;
    public float GamusharaMixWeightMultiplier;
    public float GamusharaMixEnergyMultiplier;
    public int GamusharaMixBuffWeightIncrement;
    public int GamusharaMixBuffEnergyIncrement;

    [Header("べんきょう")]
    public int BenkyouEnergyCost;
    public int BenkyouBuffWeightIncrement;
    public int BenkyouComboBuffIncrement;

    [Header("じゅくすい")]
    public int JukusuiEnergyBonus;

    [Header("どかぐい")]
    public int DokaguiEnergyCost;
    public int DokaguiWeightBonus;

    [Header("=== タイミング設定 ===")]
    [Tooltip("ブラックアウトパネルの表示時間（秒）")]
    public float BlackoutDuration;

    [Tooltip("行動実行後の待機時間（秒）")]
    public float ActionWaitDuration;

    [Tooltip("選択フェーズの制限時間（秒）")]
    public int SelectionPhaseTimeLimit;

    [Header("=== その他 ===")]
    public int TotalSpecialAbilities;
    public int SelectedSpecialAbilitiesCount;

    /// <summary>
    /// Inspector上で値が変更されたときに呼ばれる
    /// 異常値をチェックして警告を表示
    /// </summary>
    private void OnValidate()
    {
        if (InitialWeight <= 0)
        {
            Debug.LogWarning("InitialWeightは1以上に設定してください");
        }

        if (InitialEnergy <= 0)
        {
            Debug.LogWarning("InitialEnergyは1以上に設定してください");
        }

        if (EvolutionWeightThreshold <= 0 || EvolutionWeightThreshold >= VictoryWeightThreshold)
        {
            Debug.LogWarning("EvolutionWeightThresholdは0より大きく、VictoryWeightThresholdより小さい値に設定してください");
        }

        if (SicknessSleepApneaProbability + SicknessDiabetesProbability != 1.0f)
        {
            Debug.LogWarning("SicknessSleepApneaProbabilityとSicknessDiabetesProbabilityの合計は1.0である必要があります");
        }

        if (InjuryBackPainProbability + InjuryHeatstrokeProbability != 1.0f)
        {
            Debug.LogWarning("InjuryBackPainProbabilityとInjuryHeatstrokeProbabilityの合計は1.0である必要があります");
        }
    }
}
