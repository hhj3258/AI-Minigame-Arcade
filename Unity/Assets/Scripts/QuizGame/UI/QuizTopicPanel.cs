using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 퀴즈 주제 선택 패널.
/// 버튼 진입 애니메이션 및 hover 처리를 담당한다.
/// </summary>
public class QuizTopicPanel
{
    public event Action<int> OnTopicSelected;

    private readonly VisualElement       _panel;
    private readonly List<VisualElement> _buttons = new();

    public QuizTopicPanel(VisualElement root, QuizSettings settings)
    {
        _panel = root.Q<VisualElement>("topic-select-panel");

        var topicList = root.Q<VisualElement>(className: "topic-list");
        if (topicList == null) return;

        var btns = topicList.Query<VisualElement>(className: "topic-btn").ToList();
        for (int i = 0; i < btns.Count; i++)
        {
            int idx = i;
            _buttons.Add(btns[i]);

            btns[i].RegisterCallback<PointerEnterEvent>(_ => btns[idx].AddToClassList("hovered"));
            btns[i].RegisterCallback<PointerLeaveEvent>(_ => btns[idx].RemoveFromClassList("hovered"));
            UIAnim.RegisterPressAnim(btns[i], () => OnTopicSelected?.Invoke(idx));

            // QuizSettings에서 이모지 + 이름 설정
            if (settings != null && i < settings.Topics.Length)
            {
                var entry = settings.Topics[i];
                btns[i].Add(new Label($"{entry.Emoji} {entry.Name}"));
            }
        }
    }

    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);

    /// <summary>패널이 표시될 때 버튼을 순차 진입 애니메이션으로 등장시킨다.</summary>
    public void AnimateButtons()
    {
        const int AnimDuration = 350;
        const int StepMs       = 250;

        for (int i = 0; i < _buttons.Count; i++)
        {
            var btn = _buttons[i];
            btn.style.visibility = Visibility.Hidden;
            btn.RemoveFromClassList("anim-in");
            btn.RemoveFromClassList("anim-done");

            int delayMs = 60 + i * StepMs;
            btn.schedule.Execute(() =>
            {
                btn.style.visibility = StyleKeyword.Null;
                btn.AddToClassList("anim-in");
            }).StartingIn(delayMs);

            btn.schedule.Execute(() => btn.AddToClassList("anim-done"))
               .StartingIn(delayMs + AnimDuration + 70);
        }
    }

    private static void SetVisible(bool visible, VisualElement panel)
    {
        if (panel == null) return;
        if (visible) panel.RemoveFromClassList("hidden");
        else         panel.AddToClassList("hidden");
    }

    private void SetVisible(bool visible) => SetVisible(visible, _panel);
}
