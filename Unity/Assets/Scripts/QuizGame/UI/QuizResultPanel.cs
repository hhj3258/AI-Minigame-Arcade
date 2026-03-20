using System;
using UnityEngine.UIElements;

/// <summary>
/// 퀴즈 결과 패널.
/// 점수·AI 총평 표시 및 재시작/홈 버튼 이벤트를 담당한다.
/// </summary>
public class QuizResultPanel
{
    public event Action OnRestartClicked;
    public event Action OnHomeClicked;

    private readonly VisualElement _panel;
    private readonly VisualElement _resultBody;
    private readonly VisualElement _resultScoreCard;
    private readonly Label         _resultEmoji;
    private readonly Label         _resultTitle;
    private readonly Label         _resultScore;
    private readonly Label         _resultComment;

    public QuizResultPanel(VisualElement root)
    {
        _panel          = root.Q<VisualElement>("result-panel");
        _resultBody     = root.Q<VisualElement>(className: "result-body");
        _resultScoreCard= root.Q<VisualElement>(className: "result-score-card");
        _resultEmoji    = root.Q<Label>(className: "result-emoji");
        _resultTitle    = root.Q<Label>(className: "result-title");
        _resultScore    = root.Q<Label>(className: "result-score");
        _resultComment  = root.Q<Label>(className: "result-comment");

        var restartBtn = root.Q<Button>(className: "restart-btn");
        if (restartBtn != null)
            UIAnim.RegisterPressAnim(restartBtn, () => OnRestartClicked?.Invoke());

        var homeBtn = root.Q<VisualElement>("home-btn");
        if (homeBtn != null)
        {
            homeBtn.RegisterCallback<PointerEnterEvent>(_ => homeBtn.AddToClassList("hovered"));
            homeBtn.RegisterCallback<PointerLeaveEvent>(_ => homeBtn.RemoveFromClassList("hovered"));
            UIAnim.RegisterPressAnim(homeBtn, () => OnHomeClicked?.Invoke());
        }
    }

    public void Show(int correctCount, int totalCount)
    {
        if (_panel != null) _panel.RemoveFromClassList("hidden");

        bool cleared = correctCount >= totalCount * 0.6f;

        if (_resultEmoji  != null) _resultEmoji.text  = cleared ? "🎉" : "😢";
        if (_resultTitle  != null) _resultTitle.text  = cleared ? "클리어!" : "실패";
        if (_resultScore  != null) _resultScore.text  = $"{correctCount} / {totalCount}";
        if (_resultComment!= null) _resultComment.text = "총평을 불러오는 중...";

        AnimateResultEntrance();
    }

    public void Hide()
    {
        if (_panel != null) _panel.AddToClassList("hidden");
    }

    /// <summary>비동기로 불러온 AI 총평 텍스트를 업데이트한다.</summary>
    public void SetComment(string comment)
    {
        if (_resultComment != null)
            _resultComment.text = string.IsNullOrEmpty(comment) ? "총평을 불러오지 못했습니다." : comment;
    }

    // ── 애니메이션 ──────────────────────────────────────────
    private void AnimateResultEntrance()
    {
        if (_resultBody != null)
        {
            _resultBody.RemoveFromClassList("anim-in");
            _resultBody.schedule.Execute(() => _resultBody.AddToClassList("anim-in")).StartingIn(80);
        }
        if (_resultScoreCard != null)
        {
            _resultScoreCard.RemoveFromClassList("anim-pop-in");
            _resultScoreCard.AddToClassList("anim-pop-hidden");
            _resultScoreCard.schedule.Execute(() =>
            {
                _resultScoreCard.RemoveFromClassList("anim-pop-hidden");
                _resultScoreCard.AddToClassList("anim-pop-in");
            }).StartingIn(220);
        }
    }
}
