using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// SurvivorGame HUD (UIToolkit). HP 바, EXP 바, 타이머, 처치 수를 업데이트합니다.
/// </summary>
public class SurvivorHUD
{
    private readonly VisualElement _hpFill;
    private readonly VisualElement _hpContainer;
    private readonly Label         _hpLabel;
    private readonly VisualElement _expFill;
    private readonly Label         _levelLabel;
    private readonly Label         _timerLabel;
    private readonly Label         _killLabel;

    private const float HpDangerThreshold = 0.3f;

    public SurvivorHUD(VisualElement root)
    {
        _hpFill      = root.Q("hp-fill");
        _hpContainer = root.Q("hp-container");
        _hpLabel     = root.Q<Label>("hp-label");
        _expFill     = root.Q("exp-fill");
        _levelLabel  = root.Q<Label>("level-label");
        _timerLabel  = root.Q<Label>("timer-label");
        _killLabel   = root.Q<Label>("kill-label");
    }

    public void UpdateHP(int current, int max)
    {
        if (max <= 0) return;

        float ratio = (float)current / max;

        if (_hpFill != null)
            _hpFill.style.width = Length.Percent(ratio * 100f);

        if (_hpLabel != null)
            _hpLabel.text = $"HP {current} / {max}";

        if (_hpContainer != null)
        {
            if (ratio <= HpDangerThreshold)
                _hpContainer.AddToClassList("hp-danger");
            else
                _hpContainer.RemoveFromClassList("hp-danger");
        }
    }

    public void UpdateExp(int current, int max, int level)
    {
        if (max <= 0) return;

        float ratio = (float)current / max;

        if (_expFill != null)
            _expFill.style.width = Length.Percent(ratio * 100f);

        if (_levelLabel != null)
            _levelLabel.text = $"Lv.{level}";
    }

    public void UpdateTimer(float seconds)
    {
        if (_timerLabel == null) return;

        int mins = (int)(seconds / 60f);
        int secs = (int)(seconds % 60f);
        _timerLabel.text = $"{mins:00}:{secs:00}";

        // 120초 도달 시 보스 경고
        if (seconds >= 120f)
            _timerLabel.AddToClassList("boss-incoming");
        else
            _timerLabel.RemoveFromClassList("boss-incoming");
    }

    public void UpdateKillCount(int count)
    {
        if (_killLabel != null)
            _killLabel.text = $"처치 {count}";
    }
}
