/// <summary>
/// 런타임 게임 상태. ScriptableObject가 아닌 plain class — SurvivorGame이 생성/보유.
/// </summary>
public class SurvivorRunData
{
    public int   CurrentHP;
    public int   MaxHP;
    public int   KillCount;
    public float SurviveTime;   // 초
    public int   Score => KillCount * 10 + (int)SurviveTime;

    // EXP / 레벨
    public int   CurrentExp;
    public int   Level = 1;

    // 무기 레벨 (0 = 미보유, 1~3 = 보유 레벨)
    public int ShotgunLevel;
    public int OrbLevel;
    public int MissileLevel;

    public void Reset(int maxHP)
    {
        MaxHP        = maxHP;
        CurrentHP    = maxHP;
        KillCount    = 0;
        SurviveTime  = 0f;
        CurrentExp   = 0;
        Level        = 1;
        ShotgunLevel  = 0;
        OrbLevel      = 0;
        MissileLevel  = 0;
    }
}
