using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 퀴즈 게임 컨트롤러
/// </summary>
public class QuizUIGame : MonoBehaviour
{
    private enum GamePhase { TopicSelect, Loading, Playing, Explanation, Result }

    [SerializeField] private QuizSettings _quizSettings;
    [SerializeField] private SupabaseQuizClient _supabaseClient;

    // --- UI 요소 참조 ---
    private VisualElement _root;
    private VisualElement _panelTopic;
    private VisualElement _panelLoading;
    private VisualElement _panelGameplay;
    private VisualElement _panelResult;

    private List<VisualElement> _topicButtons = new List<VisualElement>();

    private Label _topicBadge;
    private Label _questionCounter;
    private Label _questionText;
    private Button[] _choiceButtons;
    private VisualElement _explanationBox;
    private Label _explanationText;
    private Label _timerLabel;
    private Label _scoreLabel;

    private VisualElement _questionCard;
    private VisualElement _resultBody;
    private VisualElement _resultScoreCard;

    private Label _resultEmoji;
    private Label _resultTitle;
    private Label _resultScore;
    private Label _resultComment;
    private Button _restartButton;

    private VisualElement _homeBtn;

    // --- 게임 상태 ---
    private GamePhase _phase;
    private string _selectedTopic;
    private string _selectedTopicEmoji;
    private int _currentIndex;
    private int _correctCount;
    private float _remainingTime;
    private bool _isRunning;
    private bool _hasAnsweredCurrentQuestion;

    private readonly List<QuizQuestion> _questions = new List<QuizQuestion>();
    private QuizQuestionService _questionService;

    // --- 로딩 도트 애니메이션 ---
    private VisualElement[] _dots;
    private CancellationTokenSource _dotCts;

    private static readonly string[] IndexLabels = { "①", "②", "③", "④" };


    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("UIDocument 컴포넌트를 찾지 못했습니다.", this);
            return;
        }

        _root = doc.rootVisualElement;

        // 패널 참조
        _panelTopic    = _root.Q<VisualElement>("topic-select-panel");
        _panelLoading  = _root.Q<VisualElement>("loading-panel");
        _panelGameplay = _root.Q<VisualElement>("gameplay-panel");
        _panelResult   = _root.Q<VisualElement>("result-panel");

        // 주제 버튼 등록
        var topicList = _root.Q<VisualElement>(className: "topic-list");
        if (topicList != null)
        {
            var btns = topicList.Query<VisualElement>(className: "topic-btn").ToList();
            for (int i = 0; i < btns.Count; i++)
            {
                int idx = i;
                _topicButtons.Add(btns[i]);
                btns[i].RegisterCallback<PointerEnterEvent>(_ => btns[idx].AddToClassList("hovered"));
                btns[i].RegisterCallback<PointerLeaveEvent>(_ => btns[idx].RemoveFromClassList("hovered"));
                RegisterPressAnim(btns[i], () => OnTopicSelected(idx));

                // QuizSettings에서 이모지 + 이름 설정
                if (_quizSettings != null && i < _quizSettings.Topics.Length)
                {
                    var entry = _quizSettings.Topics[i];
                    var label = new Label($"{entry.Emoji} {entry.Name}");
                    btns[i].Add(label);
                }
            }
        }

        // 게임플레이 패널 요소
        _topicBadge      = _root.Q<Label>(className: "topic-badge");
        _questionCounter = _root.Q<Label>(className: "counter-text");
        _questionText    = _root.Q<Label>(className: "question-text");
        _explanationBox  = _root.Q<VisualElement>(className: "explanation-box");
        _explanationText = _root.Q<Label>(className: "explanation-text");

        var statusBar = _root.Q<VisualElement>(className: "status-bar");
        if (statusBar != null)
        {
            var statusLabels = statusBar.Query<Label>().ToList();
            if (statusLabels.Count >= 2)
            {
                _timerLabel = statusLabels[0];
                _scoreLabel = statusLabels[1];
            }
        }

        var choicesContainer = _root.Q<VisualElement>(className: "choices");
        if (choicesContainer != null)
        {
            var btns = choicesContainer.Query<Button>().ToList();
            _choiceButtons = new Button[btns.Count];
            for (int i = 0; i < btns.Count; i++)
            {
                int idx = i;
                _choiceButtons[i] = btns[i];
                RegisterPressAnim(btns[i], () => OnChoiceClicked(idx));
            }
        }

        // 애니메이션 대상 요소
        _questionCard   = _root.Q<VisualElement>(className: "question-card");
_resultBody     = _root.Q<VisualElement>(className: "result-body");
        _resultScoreCard = _root.Q<VisualElement>(className: "result-score-card");

        // 결과 패널 요소
        _resultEmoji   = _root.Q<Label>(className: "result-emoji");
        _resultTitle   = _root.Q<Label>(className: "result-title");
        _resultScore   = _root.Q<Label>(className: "result-score");
        _resultComment = _root.Q<Label>(className: "result-comment");
        _restartButton = _root.Q<Button>(className: "restart-btn");
        if (_restartButton != null)
        {
            RegisterPressAnim(_restartButton, OnRestartClicked);
        }

        // 홈 버튼
        _homeBtn = _root.Q<VisualElement>("home-btn");
        if (_homeBtn != null)
        {
            _homeBtn.RegisterCallback<PointerEnterEvent>(_ => _homeBtn.AddToClassList("hovered"));
            _homeBtn.RegisterCallback<PointerLeaveEvent>(_ => _homeBtn.RemoveFromClassList("hovered"));
            RegisterPressAnim(_homeBtn, OnHomeClicked);
        }

        // 로딩 도트 참조
        _dots = new VisualElement[]
        {
            _root.Q("dot-0"),
            _root.Q("dot-1"),
            _root.Q("dot-2"),
        };

        if (_supabaseClient == null)
            _supabaseClient = GetComponent<SupabaseQuizClient>();

        _questionService = new QuizQuestionService(_supabaseClient, _quizSettings);

        ShowPhase(GamePhase.TopicSelect);
    }

    private void Update()
    {
        if (!_isRunning || _phase != GamePhase.Playing) return;

        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0f) _remainingTime = 0f;

        if (_timerLabel != null)
            _timerLabel.text = $"⏱ {_remainingTime:0.0}s";

        if (_remainingTime <= 0f && !_hasAnsweredCurrentQuestion)
            HandleTimeout();
    }

    private void OnTopicSelected(int index)
    {
        if (_quizSettings == null || index < 0 || index >= _quizSettings.Topics.Length) return;
        _selectedTopic = _quizSettings.Topics[index].Name;
        _selectedTopicEmoji = _quizSettings.Topics[index].Emoji;
        LoadQuestionsAsync().Forget();
    }

    private async UniTaskVoid LoadQuestionsAsync()
    {
        ShowPhase(GamePhase.Loading);
        _isRunning = false;
        _hasAnsweredCurrentQuestion = false;
        _questions.Clear();

        List<QuizQuestion> loaded = null;
        if (_questionService != null)
            loaded = await _questionService.GetQuestionsAsync(_selectedTopic, _quizSettings.QuestionCount);

        if (loaded == null || loaded.Count == 0)
        {
            Debug.LogWarning("퀴즈 로드 실패, 더미 데이터 사용", this);
            CreateDummyQuestions();
        }
        else
        {
            _questions.AddRange(loaded);
        }

        _currentIndex = 0;
        _correctCount = 0;
        _isRunning = true;
        _hasAnsweredCurrentQuestion = false;

        ShowCurrentQuestion();
        ShowPhase(GamePhase.Playing);
    }

    private void ShowCurrentQuestion()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        QuizQuestion q = _questions[_currentIndex];

        if (_topicBadge != null)
        {
            string emoji = string.IsNullOrEmpty(_selectedTopicEmoji) ? "" : _selectedTopicEmoji + " ";
            _topicBadge.text = emoji + (string.IsNullOrEmpty(q.Topic) ? _selectedTopic : q.Topic);
        }

        if (_questionCounter != null)
            _questionCounter.text = $"{_currentIndex + 1} / {_questions.Count}";

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
                    // visibility:hidden은 transition 대상 아님 → 즉시 숨김 (역방향 fade-out 방지)
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

        // 해설 패널 숨김
        if (_explanationBox != null)
            _explanationBox.AddToClassList("hidden");

        _remainingTime = _quizSettings != null ? _quizSettings.TimeLimitSeconds : 20f;

        if (_timerLabel != null)
            _timerLabel.text = $"⏱ {_remainingTime:0.0}s";

        if (_scoreLabel != null)
            _scoreLabel.text = $"★ {_correctCount} / {_questions.Count}";

        _hasAnsweredCurrentQuestion = false;

        AnimateQuestionEntrance();
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (!_isRunning || _phase != GamePhase.Playing || _hasAnsweredCurrentQuestion) return;
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        _hasAnsweredCurrentQuestion = true;

        QuizQuestion q = _questions[_currentIndex];
        bool isCorrect = choiceIndex == q.AnswerIndex;

        // 모든 선택지 비활성화
        if (_choiceButtons != null)
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) btn.SetEnabled(false);

            // 선택한 버튼 색상 적용
            if (choiceIndex < _choiceButtons.Length && _choiceButtons[choiceIndex] != null)
                _choiceButtons[choiceIndex].AddToClassList(isCorrect ? "correct" : "wrong");

            // 오답 시 정답 하이라이트
            if (!isCorrect && q.AnswerIndex < _choiceButtons.Length && _choiceButtons[q.AnswerIndex] != null)
                _choiceButtons[q.AnswerIndex].AddToClassList("correct");
        }

        if (isCorrect) _correctCount++;

        if (_scoreLabel != null)
            _scoreLabel.text = $"★ {_correctCount} / {_questions.Count}";

        // 해설 표시
        if (_explanationBox != null)
            _explanationBox.RemoveFromClassList("hidden");

        if (_explanationText != null)
            _explanationText.text = q.Explanation;

        _phase = GamePhase.Explanation;
        _isRunning = false;
        DelayNextQuestion().Forget();
    }

    private void HandleTimeout()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        _hasAnsweredCurrentQuestion = true;
        _isRunning = false;

        QuizQuestion q = _questions[_currentIndex];

        if (_choiceButtons != null)
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) btn.SetEnabled(false);

            if (q.AnswerIndex < _choiceButtons.Length && _choiceButtons[q.AnswerIndex] != null)
                _choiceButtons[q.AnswerIndex].AddToClassList("correct");
        }

        if (_explanationBox != null)
            _explanationBox.RemoveFromClassList("hidden");

        if (_explanationText != null)
            _explanationText.text = $"시간 초과! {q.Explanation}";

        _phase = GamePhase.Explanation;
        DelayNextQuestion().Forget();
    }

    private async UniTaskVoid DelayNextQuestion()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(2));
        if (_phase != GamePhase.Explanation) return;
        NextQuestion();
    }

    private void NextQuestion()
    {
        _currentIndex++;
        if (_currentIndex >= _questions.Count)
        {
            ShowResult();
            return;
        }

        _phase = GamePhase.Playing;
        _isRunning = true;
        ShowCurrentQuestion();
    }

    private void ShowResult()
    {
        _isRunning = false;
        int total = _questions.Count;
        bool cleared = _correctCount >= total * 0.6f;

        if (_resultEmoji != null)
            _resultEmoji.text = cleared ? "🎉" : "😢";

        if (_resultTitle != null)
            _resultTitle.text = cleared ? "클리어!" : "실패";

        if (_resultScore != null)
            _resultScore.text = $"{_correctCount} / {total}";

        if (_resultComment != null)
            _resultComment.text = "총평을 불러오는 중...";

        ShowPhase(GamePhase.Result);
        AnimateResultEntrance();
        FetchAndShowCommentAsync().Forget();
    }

    private async UniTaskVoid FetchAndShowCommentAsync()
    {
        if (_resultComment == null || _questionService == null)
        {
            if (_resultComment != null)
                _resultComment.text = "총평을 불러오지 못했습니다.";
            return;
        }

        string comment = await _questionService.GetCommentAsync(_correctCount, _questions.Count, _selectedTopic);
        _resultComment.text = string.IsNullOrEmpty(comment) ? "총평을 불러오지 못했습니다." : comment;
    }

    private void OnRestartClicked()
    {
        ResetToTopicSelect();
    }

    private void OnHomeClicked()
    {
        ResetToTopicSelect();
    }

    private void ResetToTopicSelect()
    {
        _questions.Clear();
        _currentIndex = 0;
        _correctCount = 0;
        _isRunning = false;
        _hasAnsweredCurrentQuestion = false;
        ShowPhase(GamePhase.TopicSelect);
    }

    private void ShowPhase(GamePhase phase)
    {
        _phase = phase;
        SetPanel(_panelTopic,    phase == GamePhase.TopicSelect);
        SetPanel(_panelLoading,  phase == GamePhase.Loading);
        SetPanel(_panelGameplay, phase == GamePhase.Playing || phase == GamePhase.Explanation);
        SetPanel(_panelResult,   phase == GamePhase.Result);

        if (phase == GamePhase.Loading)
            StartDotAnimation();
        else
            StopDotAnimation();

        if (phase == GamePhase.TopicSelect)
            AnimateTopicButtons();
    }

    private void StartDotAnimation()
    {
        StopDotAnimation();
        _dotCts = new CancellationTokenSource();
        // 각 도트를 독립 루프로 실행 (오프셋: 0ms / 250ms / 500ms)
        int[] offsets = { 0, 250, 500 };
        for (int i = 0; i < _dots.Length; i++)
        {
            if (_dots[i] != null)
                AnimateDotAsync(_dots[i], offsets[i], _dotCts.Token).Forget();
        }
    }

    private async UniTaskVoid AnimateDotAsync(VisualElement dot, int initialDelayMs, CancellationToken ct)
    {
        try
        {
            if (initialDelayMs > 0)
                await UniTask.Delay(initialDelayMs, cancellationToken: ct);

            while (!ct.IsCancellationRequested)
            {
                dot.AddToClassList("active");
                await UniTask.Delay(400, cancellationToken: ct);
                dot.RemoveFromClassList("active");
                await UniTask.Delay(350, cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void StopDotAnimation()
    {
        _dotCts?.Cancel();
        _dotCts?.Dispose();
        _dotCts = null;
        if (_dots == null) return;
        foreach (var dot in _dots)
            dot?.RemoveFromClassList("active");
    }

    // ── 유틸 ────────────────────────────────────────────

    // 버튼 프레스 애니메이션
    // PointerDown : 즉시 0.93 축소
    // PointerUp   : 즉시 1.12 오버슈트 → 1프레임 후 CSS ease-out으로 1.0 복귀 (스프링 효과)
    // PointerLeave: 눌린 상태일 때만 동일 처리 (액션 미실행)
    private static readonly StyleScale PressedScale =
        new(new Scale(new Vector3(0.93f, 0.93f, 1f)));
    private static readonly StyleScale OvershootScale =
        new(new Scale(new Vector3(1.12f, 1.12f, 1f)));
    private static readonly StyleList<TimeValue> ZeroTransition =
        new(new List<TimeValue> { new(0) });

    private void RegisterPressAnim(VisualElement el, Action onReleaseDone = null)
    {
        bool fireOnEnd = false;
        bool isPressed = false;

        el.RegisterCallback<PointerDownEvent>(_ =>
        {
            isPressed = true;
            fireOnEnd = false;
            // transitionDuration은 건드리지 않음 → CSS 0.2s ease-out으로 부드럽게 축소
            el.style.scale = PressedScale;
        }, TrickleDown.TrickleDown);

        el.RegisterCallback<PointerUpEvent>(_ =>
        {
            if (!isPressed) return;
            isPressed = false;
            fireOnEnd = onReleaseDone != null;
            // 즉시 오버슈트(1.12) 후 1프레임 뒤 CSS transition으로 1.0 복귀
            el.style.transitionDuration = ZeroTransition;
            el.style.scale = OvershootScale;
            el.schedule.Execute(() =>
            {
                el.style.transitionDuration = StyleKeyword.Null;
                el.style.scale = new StyleScale(StyleKeyword.Null);
            }).StartingIn(1);
        });

        el.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            if (!isPressed) return;
            isPressed = false;
            fireOnEnd = false;
            el.style.transitionDuration = ZeroTransition;
            el.style.scale = OvershootScale;
            el.schedule.Execute(() =>
            {
                el.style.transitionDuration = StyleKeyword.Null;
                el.style.scale = new StyleScale(StyleKeyword.Null);
            }).StartingIn(1);
        });

        if (onReleaseDone != null)
        {
            el.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (!fireOnEnd) return;
                foreach (var p in evt.stylePropertyNames)
                {
                    if (p.ToString() != "scale") continue;
                    fireOnEnd = false;
                    onReleaseDone();
                    break;
                }
            });
        }
    }

    // ── 애니메이션 ──────────────────────────────────────

    private void AnimateTopicButtons()
    {
        // 버튼 1개당 애니메이션 duration 350ms + 여유 20ms = 250ms 간격으로 순차 재생 (살짝 겹침)
        const int AnimDuration = 350;
        const int StepMs = 250;

        for (int i = 0; i < _topicButtons.Count; i++)
        {
            var btn = _topicButtons[i];
            // visibility:hidden으로 즉시 숨김 → 역방향 fade-out 방지
            btn.style.visibility = Visibility.Hidden;
            btn.RemoveFromClassList("anim-in");
            btn.RemoveFromClassList("anim-done");
            int delayMs = 60 + i * StepMs;
            btn.schedule.Execute(() =>
            {
                btn.style.visibility = StyleKeyword.Null;
                btn.AddToClassList("anim-in");
            }).StartingIn(delayMs);
            // 진입 애니메이션 완료 후 opacity/translate transition 제거 → hover 역행 방지
            btn.schedule.Execute(() => btn.AddToClassList("anim-done")).StartingIn(delayMs + AnimDuration + 70);
        }
    }

    private void AnimateQuestionEntrance()
    {
        // 질문 카드: visibility:hidden으로 즉시 숨긴 뒤 fade-in
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

        // 선택지 버튼 위→아래 순차 등장 (visibility 해제 후 CSS transition 발동)
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

    private static void SetPanel(VisualElement panel, bool visible)
    {
        if (panel == null) return;
        if (visible)
            panel.RemoveFromClassList("hidden");
        else
            panel.AddToClassList("hidden");
    }

    private void CreateDummyQuestions()
    {
        _questions.Clear();
        _questions.Add(new QuizQuestion
        {
            Topic = "상식",
            Question = "태양계에서 가장 큰 행성은 무엇일까요?",
            Choices = new[] { "지구", "목성", "토성", "금성" },
            AnswerIndex = 1,
            Explanation = "목성은 태양계에서 가장 큰 가스 행성입니다."
        });
        _questions.Add(new QuizQuestion
        {
            Topic = "과학",
            Question = "물의 화학식은 무엇일까요?",
            Choices = new[] { "CO2", "H2O", "O2", "NaCl" },
            AnswerIndex = 1,
            Explanation = "물은 두 개의 수소 원자와 한 개의 산소 원자로 이루어져 있습니다."
        });
        _questions.Add(new QuizQuestion
        {
            Topic = "역사",
            Question = "고려를 세운 왕은 누구일까요?",
            Choices = new[] { "이성계", "세종대왕", "왕건", "광개토대왕" },
            AnswerIndex = 2,
            Explanation = "왕건이 후삼국을 통일하고 고려를 세웠습니다."
        });
    }
}
