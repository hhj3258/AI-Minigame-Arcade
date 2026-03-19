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

    private List<Button> _topicButtons = new List<Button>();

    private Label _topicBadge;
    private Label _questionCounter;
    private Label _questionText;
    private Button[] _choiceButtons;
    private VisualElement _explanationBox;
    private Label _explanationText;
    private Label _timerLabel;
    private Label _scoreLabel;

    private Label _resultEmoji;
    private Label _resultTitle;
    private Label _resultScore;
    private Label _resultComment;
    private Button _restartButton;

    // --- 게임 상태 ---
    private GamePhase _phase;
    private string _selectedTopic;
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

        // 탭 네비게이션 숨김 (게임 플로우로 대체)
        var tabNav = _root.Q<VisualElement>(className: "tab-nav");
        if (tabNav != null)
            tabNav.style.display = DisplayStyle.None;

        // 패널 참조
        _panelTopic    = _root.Q<VisualElement>("panel-topic");
        _panelLoading  = _root.Q<VisualElement>("panel-loading");
        _panelGameplay = _root.Q<VisualElement>("panel-gameplay");
        _panelResult   = _root.Q<VisualElement>("panel-result");

        // 주제 버튼 등록
        var topicList = _root.Q<VisualElement>(className: "topic-list");
        if (topicList != null)
        {
            var btns = topicList.Query<Button>().ToList();
            for (int i = 0; i < btns.Count; i++)
            {
                int idx = i;
                _topicButtons.Add(btns[i]);
                btns[i].clicked += () => OnTopicSelected(idx);

                // QuizSettings에 해당 인덱스 주제가 있으면 텍스트 덮어씀
                if (_quizSettings != null && i < _quizSettings.Topics.Length)
                    btns[i].text = _quizSettings.Topics[i];
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
                btns[i].clicked += () => OnChoiceClicked(idx);
            }
        }

        // 결과 패널 요소
        _resultEmoji   = _root.Q<Label>(className: "result-emoji");
        _resultTitle   = _root.Q<Label>(className: "result-title");
        _resultScore   = _root.Q<Label>(className: "result-score");
        _resultComment = _root.Q<Label>(className: "result-comment");
        _restartButton = _root.Q<Button>(className: "restart-btn");
        if (_restartButton != null)
            _restartButton.clicked += OnRestartClicked;

        // SupabaseClient 자동 탐색
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
        _selectedTopic = _quizSettings.Topics[index];
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
            _topicBadge.text = string.IsNullOrEmpty(q.Topic) ? _selectedTopic : q.Topic;

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
            _resultScore.text = $"{_correctCount} / {total} 정답";

        if (_resultComment != null)
            _resultComment.text = "총평을 불러오는 중...";

        ShowPhase(GamePhase.Result);
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
    }

    private void StartDotAnimation()
    {
        StopDotAnimation();
        _dotCts = new CancellationTokenSource();
        // 각 도트를 독립 루프로 실행 (오프셋: 0ms / 133ms / 267ms)
        int[] offsets = { 0, 133, 267 };
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
                await UniTask.Delay(200, cancellationToken: ct);
                dot.RemoveFromClassList("active");
                await UniTask.Delay(200, cancellationToken: ct);
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
