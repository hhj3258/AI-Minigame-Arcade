using System;
using UnityEngine;

/// <summary>
/// EXP 누적, 레벨업 처리. 레벨업 이벤트 발행 시 Time.timeScale = 0.
/// </summary>
public class LevelSystem
{
    public int Level       { get; private set; } = 1;
    public int CurrentExp  { get; private set; }
    public int MaxExp      => GetExpThreshold(Level);

    public event Action<int> OnLevelUp;         // 파라미터: 새 레벨
    public event Action<int, int> OnExpChanged; // (currentExp, maxExp)

    private readonly int[] _expThresholds; // index 0 = Lv1→2 필요 EXP

    private const int DefaultExpPerLevel = 100;

    public LevelSystem(int[] expThresholds)
    {
        _expThresholds = expThresholds ?? Array.Empty<int>();
        Level      = 1;
        CurrentExp = 0;
    }

    public void AddExp(int amount)
    {
        CurrentExp += amount;
        OnExpChanged?.Invoke(CurrentExp, MaxExp);

        while (CurrentExp >= MaxExp)
        {
            CurrentExp -= MaxExp;
            Level++;
            Time.timeScale = 0f; // 레벨업 중 일시정지
            OnLevelUp?.Invoke(Level);
        }
    }

    /// <summary>
    /// 업그레이드 선택 완료 후 호출. 게임 재개.
    /// </summary>
    public void ResumeAfterLevelUp()
    {
        Time.timeScale = 1f;
    }

    private int GetExpThreshold(int level)
    {
        int idx = level - 1; // Lv1 → index 0
        if (_expThresholds != null && idx < _expThresholds.Length)
            return Mathf.Max(1, _expThresholds[idx]);
        return DefaultExpPerLevel * level;
    }

    public void Reset()
    {
        Level      = 1;
        CurrentExp = 0;
        Time.timeScale = 1f;
    }
}
