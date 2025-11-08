using Fusion;

/// <summary>
/// プレイヤーが選択した行動データを保持する構造体
/// Fusionのネットワーク同期に対応するためINetworkStructを実装
/// </summary>
public struct ActionData : INetworkStruct
{
    /// <summary>
    /// 行動の種類（たべる、ねむる、あそぶ、つういん、特殊能力）
    /// </summary>
    public ActionType Type;

    /// <summary>
    /// 行動のジャンル（ジャンケンの手）
    /// </summary>
    public Genre Genre;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="type">行動の種類</param>
    /// <param name="genre">ジャンル</param>
    public ActionData(ActionType type, Genre genre)
    {
        Type = type;
        Genre = genre;
    }

    /// <summary>
    /// 特定の行動に対応するActionDataを生成するヘルパーメソッド
    /// </summary>
    public static ActionData CreateEat() => new ActionData(ActionType.Eat, Genre.Rock);
    public static ActionData CreateSleep() => new ActionData(ActionType.Sleep, Genre.Paper);
    public static ActionData CreatePlay() => new ActionData(ActionType.Play, Genre.Scissors);
    public static ActionData CreateClinic() => new ActionData(ActionType.Clinic, Genre.None);

    /// <summary>
    /// 特殊能力の場合、特殊能力の種類に応じてジャンルを設定
    /// </summary>
    public static ActionData CreateSpecialAbility(SpecialAbilityType abilityType)
    {
        Genre genre = abilityType switch
        {
            SpecialAbilityType.Gaishoku => Genre.Paper,
            SpecialAbilityType.Kintre => Genre.Scissors,
            SpecialAbilityType.Gamushara => Genre.None,
            SpecialAbilityType.Benkyou => Genre.Scissors,
            SpecialAbilityType.Jukusui => Genre.Paper,
            SpecialAbilityType.Dokagui => Genre.Rock,
            _ => Genre.None
        };

        return new ActionData(ActionType.SpecialAbility, genre);
    }

    /// <summary>
    /// デフォルトの行動（ねむる）を返す
    /// タイムアウト時や未選択時に使用
    /// </summary>
    public static ActionData Default() => CreateSleep();

    /// <summary>
    /// 空の行動（未選択状態）を返す
    /// 準備フェーズでのリセット時に使用
    /// </summary>
    public static ActionData Empty() => new ActionData(ActionType.None, Genre.None);
}
