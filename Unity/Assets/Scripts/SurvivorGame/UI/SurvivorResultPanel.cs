using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 게임 오버 결과 패널 (UIToolkit).
/// 생존 시간, 처치 수, 점수를 표시하고 재시작 버튼을 제공합니다.
/// </summary>
public class SurvivorResultPanel
{
    private readonly VisualElement _panel;
    private readonly Label         _surviveLabelVal;
    private readonly Label         _killCountVal;
    private readonly Label         _scoreVal;
    private readonly Button        _restartButton;

    public event Action OnRestartClicked;

    public SurvivorResultPanel(VisualElement root)
    {
        _panel           = root.Q("result-panel");
        _surviveLabelVal = root.Q<Label>("survive-time-label");
        _killCountVal    = root.Q<Label>("kill-count-label");
        _scoreVal        = root.Q<Label>("score-label");
        _restartButton   = root.Q<Button>("restart-button");

        if (_restartButton != null)
            _restartButton.clicked += () => OnRestartClicked?.Invoke();
    }

    public void Show(SurvivorRunData data)
    {
        if (_panel == null) return;

        float t    = data.SurviveTime;
        int   mins = (int)(t / 60f);
        int   secs = (int)(t % 60f);

        if (_surviveLabelVal != null)
            _surviveLabelVal.text = $"{mins:00}:{secs:00}";

        if (_killCountVal != null)
            _killCountVal.text = data.KillCount.ToString();

        if (_scoreVal != null)
            _scoreVal.text = data.Score.ToString("N0");

        _panel.RemoveFromClassList("hidden");
    }

    public void Hide()
    {
        _panel?.AddToClassList("hidden");
    }
}
