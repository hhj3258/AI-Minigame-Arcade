using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

/// <summary>
/// UI Toolkit 기반 퀴즈 게임 컨트롤러. IMinigame 구현체.
/// UI 패널 4종(TopicPanel, LoadingPanel, GameplayPanel, ResultPanel)을 조율하고
/// 게임 페이즈 및 상태를 관리한다.
/// </summary>
[MovedFrom(true, null, null, "QuizUIGame")]
public class QuizGame : MonoBehaviour
{
    private enum GamePhase { TopicSelect, Loading, Playing, Explanation, Result }

    [SerializeField] private QuizSettings       _quizSettings;
    [SerializeField] private SupabaseQuizClient _supabaseClient;

    // ── 패널 ─────────────────────────────────────────────
    private QuizTopicPanel    _topicPanel;
    private QuizLoadingPanel  _loadingPanel;
    private QuizGameplayPanel _gameplayPanel;
    private QuizResultPanel   _resultPanel;

    // ── 게임 상태 ─────────────────────────────────────────
    private GamePhase _phase;
    private string    _selectedTopic;
    private string    _selectedTopicEmoji;
    private int       _currentIndex;
    private int       _correctCount;
    private float     _remainingTime;
    private bool      _isRunning;
    private bool      _hasAnsweredCurrentQuestion;

    private readonly List<QuizQuestion> _questions = new();
    private QuizQuestionService         _questionService;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("UIDocument 컴포넌트를 찾지 못했습니다.", this);
            return;
        }

        var root = doc.rootVisualElement;

        if (_supabaseClient == null)
            _supabaseClient = GetComponent<SupabaseQuizClient>();

        _questionService = new QuizQuestionService(_supabaseClient, _quizSettings);

        // 패널 생성
        _topicPanel    = new QuizTopicPanel(root, _quizSettings);
        _loadingPanel  = new QuizLoadingPanel(root);
        _gameplayPanel = new QuizGameplayPanel(root);
        _resultPanel   = new QuizResultPanel(root);

        // 이벤트 구독
        _topicPanel.OnTopicSelected   += OnTopicSelected;
        _gameplayPanel.OnChoiceSelected+= OnChoiceClicked;
        _resultPanel.OnRestartClicked  += OnRestartClicked;
        _resultPanel.OnHomeClicked     += OnHomeClicked;

        ShowPhase(GamePhase.TopicSelect);
    }

    private void Update()
    {
        if (!_isRunning || _phase != GamePhase.Playing) return;

        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0f) _remainingTime = 0f;

        _gameplayPanel?.UpdateTimer(_remainingTime);

        if (_remainingTime <= 0f && !_hasAnsweredCurrentQuestion)
            HandleTimeout();
    }

    // ── 토픽 선택 ─────────────────────────────────────────
    private void OnTopicSelected(int index)
    {
        if (_quizSettings == null || index < 0 || index >= _quizSettings.Topics.Length) return;
        _selectedTopic      = _quizSettings.Topics[index].Name;
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

        _currentIndex  = 0;
        _correctCount  = 0;
        _isRunning     = true;
        _hasAnsweredCurrentQuestion = false;

        ShowCurrentQuestion();
        ShowPhase(GamePhase.Playing);
    }

    // ── 문제 표시 ─────────────────────────────────────────
    private void ShowCurrentQuestion()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        QuizQuestion q = _questions[_currentIndex];
        float timeLimit = _quizSettings != null ? _quizSettings.TimeLimitSeconds : 20f;

        _remainingTime = timeLimit;
        _hasAnsweredCurrentQuestion = false;

        _gameplayPanel?.ShowQuestion(q, _currentIndex, _questions.Count, timeLimit,
                                     _selectedTopicEmoji, _selectedTopic, _correctCount);
    }

    // ── 선택지 클릭 ───────────────────────────────────────
    private void OnChoiceClicked(int choiceIndex)
    {
        if (!_isRunning || _phase != GamePhase.Playing || _hasAnsweredCurrentQuestion) return;
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        _hasAnsweredCurrentQuestion = true;

        QuizQuestion q = _questions[_currentIndex];
        bool isCorrect = choiceIndex == q.AnswerIndex;

        if (isCorrect) _correctCount++;
        _gameplayPanel?.UpdateScore(_correctCount, _questions.Count);
        _gameplayPanel?.ShowAnswer(q.AnswerIndex, choiceIndex, q.Explanation);

        _phase     = GamePhase.Explanation;
        _isRunning = false;
        DelayNextQuestion().Forget();
    }

    private void HandleTimeout()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        _hasAnsweredCurrentQuestion = true;
        _isRunning = false;

        QuizQuestion q = _questions[_currentIndex];
        _gameplayPanel?.DisableChoices();
        _gameplayPanel?.ShowAnswer(q.AnswerIndex, -1, $"시간 초과! {q.Explanation}");

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
        _phase     = GamePhase.Playing;
        _isRunning = true;
        ShowCurrentQuestion();
    }

    // ── 결과 ──────────────────────────────────────────────
    private void ShowResult()
    {
        _isRunning = false;
        ShowPhase(GamePhase.Result);
        _resultPanel?.Show(_correctCount, _questions.Count);
        FetchAndShowCommentAsync().Forget();
    }

    private async UniTaskVoid FetchAndShowCommentAsync()
    {
        if (_questionService == null)
        {
            _resultPanel?.SetComment(null);
            return;
        }
        string comment = await _questionService.GetCommentAsync(_correctCount, _questions.Count, _selectedTopic);
        _resultPanel?.SetComment(comment);
    }

    private void OnRestartClicked() => ResetToTopicSelect();
    private void OnHomeClicked()    => ResetToTopicSelect();

    private void ResetToTopicSelect()
    {
        _questions.Clear();
        _currentIndex  = 0;
        _correctCount  = 0;
        _isRunning     = false;
        _hasAnsweredCurrentQuestion = false;
        ShowPhase(GamePhase.TopicSelect);
    }

    // ── 페이즈 전환 ───────────────────────────────────────
    private void ShowPhase(GamePhase phase)
    {
        _phase = phase;

        bool isPlaying = phase == GamePhase.Playing || phase == GamePhase.Explanation;

        if (phase == GamePhase.TopicSelect) { _topicPanel?.Show();    _topicPanel?.AnimateButtons(); }
        else                                  _topicPanel?.Hide();

        if (phase == GamePhase.Loading) _loadingPanel?.Show();
        else                            _loadingPanel?.Hide();

        if (isPlaying)  _gameplayPanel?.Show();
        else            _gameplayPanel?.Hide();

        if (phase == GamePhase.Result) _resultPanel?.Show(_correctCount, _questions.Count);
        else                           _resultPanel?.Hide();
    }

    // ── 더미 데이터 ───────────────────────────────────────
    private void CreateDummyQuestions()
    {
        _questions.Clear();
        _questions.Add(new QuizQuestion
        {
            Topic       = "상식",
            Question    = "태양계에서 가장 큰 행성은 무엇일까요?",
            Choices     = new[] { "지구", "목성", "토성", "금성" },
            AnswerIndex = 1,
            Explanation = "목성은 태양계에서 가장 큰 가스 행성입니다."
        });
        _questions.Add(new QuizQuestion
        {
            Topic       = "과학",
            Question    = "물의 화학식은 무엇일까요?",
            Choices     = new[] { "CO2", "H2O", "O2", "NaCl" },
            AnswerIndex = 1,
            Explanation = "물은 두 개의 수소 원자와 한 개의 산소 원자로 이루어져 있습니다."
        });
        _questions.Add(new QuizQuestion
        {
            Topic       = "역사",
            Question    = "고려를 세운 왕은 누구일까요?",
            Choices     = new[] { "이성계", "세종대왕", "왕건", "광개토대왕" },
            AnswerIndex = 2,
            Explanation = "왕건이 후삼국을 통일하고 고려를 세웠습니다."
        });
    }
}
