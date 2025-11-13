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
    public int InitialWeight = 1;

    [Tooltip("ゲーム開始時の元気（HP）")]
    public int InitialEnergy = 100;

    [Tooltip("進化に必要な重さ（kg）")]
    public int EvolutionWeightThreshold = 200;

    [Tooltip("勝利条件の重さ（kg）")]
    public int VictoryWeightThreshold = 700;

    [Header("=== 行動パラメーター ===")]
    [Header("たべる")]
    public int EatEnergyChange = -15;
    public int EatWeightChange = 50;

    [Header("ねむる")]
    public int SleepEnergyChange = 50;

    [Header("あそぶ")]
    public int PlayEnergyChange = -30;
    public int PlayWeightBuffIncrement = 10;
    public int PlayEnergyBuffIncrement = 10;

    [Header("つういん")]
    public int ClinicEnergyChange = -20;

    [Header("連続使用ペナルティ")]
    public int ConsecutiveActionPenalty = 20;

    [Header("=== 状態異常 ===")]
    [Header("発症確率")]
    [Tooltip("病気・ケガの発症確率（重さ÷10 = %）")]
    public float SicknessProbabilityPerWeight = 0.1f;
    public float InjuryProbabilityPerWeight = 0.1f;

    [Header("病気の種類別確率")]
    public float SicknessSleepApneaProbability = 0.5f;
    public float SicknessDiabetesProbability = 0.5f;

    [Header("ケガの種類別確率")]
    public float InjuryBackPainProbability = 0.5f;
    public float InjuryHeatstrokeProbability = 0.5f;

    [Header("状態異常の効果")]
    public int SleepApneaRecoveryReduction = -20;
    public int DiabetesMorningWeightChange = -20;
    public int DiabetesMorningEnergyChange = -20;
    public float BackPainCancelProbability = 0.3f;
    public float HeatstrokeForcedClinicProbability = 1.0f;
    
    [Header("=== ジャンケン効果 ===")]
    public int JankenWinEnergyBase = 10;
    public int JankenLoseEnergyBase = -10;
    public int JankenEnergyIncrementPerDay = 5;
    public int JankenBuffDebuffAmount = 10;

    [Header("=== 特殊能力パラメーター ===")]
    [Header("がいしょく")]
    [Tooltip("がいしょくはたべると同じ効果だが、ジャンルがパー")]
    public int GaishokuEnergyChange = -15;
    public int GaishokuWeightChange = 50;

    [Header("きんとれ")]
    public int KintreEnergyCost = -120;
    public float KintreWeightMultiplier = 0.5f;
    public float KintreBuffMultiplier = 1.5f;

    [Header("がむしゃら")]
    public int GamushuraEnergyCost = -40;
    public float GamushuraWeightMultiplier = 1.2f;
    public float GamushuraEnergyMultiplier = 1.2f;
    public int GamushuraBuffWeightIncrement = 15;
    public int GamushuraBuffEnergyIncrement = 15;
    public float GamusharaMixWeightMultiplier = 0f;
    public float GamusharaMixEnergyMultiplier = 0.5f;
    public int GamusharaMixBuffWeightIncrement = 15;
    public int GamusharaMixBuffEnergyIncrement = 15;

    [Header("べんきょう")]
    public int BenkyouEnergyCost = -25;
    public int BenkyouBuffWeightIncrement = 10;
    public int BenkyouComboBuffIncrement = 10;

    [Header("じゅくすい")]
    public int JukusuiEnergyBonus = 20;

    [Header("どかぐい")]
    public int DokaguiEnergyCost = -15;
    public int DokaguiWeightBonus = 30;

    [Header("=== タイミング設定 ===")]
    [Tooltip("ブラックアウトパネルの表示時間（秒）")]
    public float BlackoutDuration = 1f;

    [Tooltip("行動実行後の待機時間（秒）")]
    public float ActionWaitDuration = 2f;

    [Tooltip("選択フェーズの制限時間（秒）")]
    public int SelectionPhaseTimeLimit = 60;

    [Header("=== その他 ===")]
    public int TotalSpecialAbilities = 6;
    public int SelectedSpecialAbilitiesCount = 3;

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
