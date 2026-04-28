/// <summary>
/// リザルト画面に必要な全データを保持するプレーンなstruct。
/// ネットワーク非依存。Shutdown後もローカルで参照可能。
/// </summary>
public struct LocalResultData
{
    public string WinnerName;
    public bool WinnerIsHost;
    public int Day;
    public bool IsMorning;

    public string MyName;
    public bool MyIsHost;
    public int MyWeight;
    public int MyEnergy;
    public string MyAbilityDisplayName;
    public int MyJankenWinCount;

    public string OppName;
    public bool OppIsHost;
    public int OppWeight;
    public int OppEnergy;
    public string OppAbilityDisplayName;
    public int OppJankenWinCount;
}
