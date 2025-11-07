using System;

/// <summary>
/// ゲームの進行フェーズを表す列挙型
/// </summary>
public enum GamePhase
{
    Preparation,  // 準備フェーズ（日数表示、進化判定など）
    Selection,    // 選択フェーズ（行動選択）
    Execution,    // 実行フェーズ（行動実行）
    GameEnd       // ゲーム終了
}

/// <summary>
/// プレイヤーが選択できる行動の種類
/// </summary>
public enum ActionType
{
    None,           // 未選択（空の状態）
    Eat,            // たべる
    Sleep,          // ねむる
    Play,           // あそぶ
    Clinic,         // つういん
    SpecialAbility  // 特殊能力（進化後のみ）
}

/// <summary>
/// 行動のジャンル（ジャンケンの手）
/// </summary>
public enum Genre
{
    Rock,      // グー
    Scissors,  // チョキ
    Paper,     // パー
    None       // なし（ジャンケン判定なし）
}

/// <summary>
/// 特殊能力の種類
/// </summary>
public enum SpecialAbilityType
{
    Gaishoku,   // がいしょく（パー）
    Kintre,     // きんとれ（チョキ）
    Gamushara,  // がむしゃら（なし）
    Benkyou,    // べんきょう（チョキ）
    Jukusui,    // じゅくすい（パー、条件: 重さ合計が奇数）
    Dokagui     // どかぐい（グー、条件: 重さ合計が偶数）
}

/// <summary>
/// 状態異常の種類
/// </summary>
public enum StatusAilment
{
    SleepApnea,  // 睡眠時無呼吸症候群（ねむる時の回復-20）
    Diabetes,    // 糖尿病（毎朝、重さ-20、元気-20）
    BackPain,    // 腰痛（30%で行動がねむるに変更）
    Heatstroke   // 熱中症（強制的につういん）
}
