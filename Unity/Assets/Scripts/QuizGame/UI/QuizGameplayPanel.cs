using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// 퀴즈 게임플레이 패널.
/// 문제·선택지·타이머·해설 표시 및 선택지 클릭 이벤트를 담당한다.
/// </summary>
public class QuizGameplayPanel
{
    public event Action<int> OnChoiceSelected;

    private static readonly string[] IndexLabels = { "①", "②", "③", "④" };

    private readonly VisualElement _panel;
    private readonly Label         _topicBadge;
    private readonly Label         _questionCounter;
    private readonly Label         _questionText;
    private readonly VisualElement _explanationBox;
    private readonly Label         _explanationText;
    private readonly Label         _timerLabel;
    private readonly Label         _scoreLabel;
    private readonly Button[]      _choiceButtons;
    private readonly VisualElement _questionCard;

    public QuizGameplayPanel(VisualElement root)
    {
        _panel          = root.Q<VisualElement>("gameplay-panel");
        _topicBadge     = root.Q<Label>(className: "topic-badge");
        _questionCounter= root.Q<Label>(className: "counter-text");
        _questionText   = root.Q<Label>(className: "question-text");
        _explanationBox = root.Q<VisualElement>(className: "explanation-box");
        _explanationText= root.Q<Label>(className: "explanation-text");
        _questionCard   = root.Q<VisualElement>(className: "question-card");

        var statusBar = root.Q<VisualElement>(className: "status-bar");
        if (statusBar != null)
        {
            var labels = statusBar.Query<Label>().ToList();
            if (labels.Count >= 2)
            {
                _timerLabel = labels[0];
                _scoreLabel = labels[1];
            }
        }

        var choicesContainer = root.Q<VisualElement>(className: "choices");
        if (choicesContainer != null)
        {
            var btns = choicesContainer.Query<Button>().ToList();
            _choiceButtons = new Button[btns.Count];
            for (int i = 0; i < btns.Count; i++)
            {
                int idx = i;
                _choiceButtons[i] = btns[i];
                UIAnim.RegisterPressAnim(btns[i], () => OnChoiceSelected?.Invoke(idx));
            }
        }
    }

    public void Show() { if (_panel != null) _panel.RemoveFromClassList("hidden"); }
    public void Hide() { if (_panel != null) _panel.AddToClassList("hidden"); }

    /// <summary>새 문제를 화면에 표시한다.</summary>
    public void ShowQuestion(QuizQuestion q, int index, int total, float timeLimit,
                             string topicEmoji, string selectedTopic, int correctCount)
    {
        if (_topicBadge != null)
        {
            string emoji = string.IsNullOrEmpty(topicEmoji) ? "" : topicEmoji + " ";
            _topicBadge.text = emoji + (string.IsNullOrEmpty(q.Topic) ? selectedTopic : q.Topic);
        }

        if (_questionCounter != null)
            _questionCounter.text = $"{index + 1} / {total}";

        if (_questionText != null)
            _questionText.text = q.Question;

        // 선택지 초기화
        if (_choiceButtons != null)
        {
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                if (_choiceButtons[i] == null) continue;

                if (i < q.Choices.Length)
                {
                    string label = i < IndexLabels.Length ? IndexLabels[i] : $"{i + 1}";
                    _choiceButtons[i].text = $"{label} {q.Choices[i]}";
                    _choiceButtons[i].SetEnabled(true);
                    _choiceButtons[i].RemoveFromClassList("correct");
                    _choiceButtons[i].RemoveFromClassList("wrong");
                    _choiceButtons[i].style.visibility = Visibility.Hidden;
                    _choiceButtons[i].RemoveFromClassList("anim-in");
                    _choiceButtons[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    _choiceButtons[i].style.display = DisplayStyle.None;
                }
            }
        }

        if (_explanationBox != null) _explanationBox.AddToClassList("hidden");

        UpdateTimer(timeLimit);
        UpdateScore(correctCount, total);
        AnimateQuestionEntrance();
    }

    public void UpdateTimer(float remaining)
    {
        if (_timerLabel != null)
            _timerLabel.text = $"⏱ {remaining:0.0}s";
    }

    public void UpdateScore(int correct, int total)
    {
        if (_scoreLabel != null)
            _scoreLabel.text = $"★ {correct} / {total}";
    }

    /// <summary>정답/오답 피드백과 해설을 표시하고 선택지를 비활성화한다.</summary>
    public void ShowAnswer(int correctIndex, int chosenIndex, string explanation)
    {
        if (_choiceButtons != null)
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) btn.SetEnabled(false);

            bool isCorrect = chosenIndex == correctIndex;
            if (chosenIndex >= 0 && chosenIndex < _choiceButtons.Length && _choiceButtons[chosenIndex] != null)
                _choiceButtons[chosenIndex].AddToClassList(isCorrect ? "correct" : "wrong");

            if (!isCorrect && correctIndex < _choiceButtons.Length && _choiceButtons[correctIndex] != null)
                _choiceButtons[correctIndex].AddToClassList("correct");
        }

        if (_explanationBox != null)  _explanationBox.RemoveFromClassList("hidden");
        if (_explanationText != null) _explanationText.text = explanation;
    }

    public void DisableChoices()
    {
        if (_choiceButtons == null) return;
        foreach (var btn in _choiceButtons)
            if (btn != null) btn.SetEnabled(false);
    }

    // ── 애니메이션 ──────────────────────────────────────────
    private void AnimateQuestionEntrance()
    {
        if (_questionCard != null)
        {
            _questionCard.style.visibility = Visibility.Hidden;
            _questionCard.RemoveFromClassList("anim-in");
            _questionCard.schedule.Execute(() =>
            {
                _questionCard.style.visibility = StyleKeyword.Null;
                _questionCard.AddToClassList("anim-in");
            }).StartingIn(40);
        }

        const int StepMs = 220;
        if (_choiceButtons != null)
        {
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                if (_choiceButtons[i] == null) continue;
                var btn = _choiceButtons[i];
                int delayMs = 200 + i * StepMs;
                btn.schedule.Execute(() =>
                {
                    btn.style.visibility = StyleKeyword.Null;
                    btn.AddToClassList("anim-in");
                }).StartingIn(delayMs);
            }
        }
    }
}
