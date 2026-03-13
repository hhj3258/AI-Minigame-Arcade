using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class QuizGame : MonoBehaviour, IMinigame
{
    private enum GamePhase
    {
        TopicSelect,
        Loading,
        Playing,
        Explanation,
        Result
    }

    [SerializeField]
    private UIDocument _uiDocument;

    [SerializeField]
    private QuizSettings _quizSettings;

    [SerializeField]
    private MonoBehaviour _apiClientObject;

    private VisualElement _quizRoot;
    private VisualElement _topicSelectPanel;
    private VisualElement _loadingPanel;
    private VisualElement _gamePanel;
    private VisualElement _resultPanel;
    private VisualElement _explanationPanel;

    private VisualElement _topicButtonsContainer;
    private VisualElement _choicesContainer;

    private readonly List<Button> _topicButtons = new List<Button>();
    private readonly List<Button> _choiceButtons = new List<Button>();

    private Label _topicBadge;
    private Label _questionCounterLabel;
    private Label _questionLabel;
    private Label _timerLabel;
    private Label _scoreLabel;
    private Label _explanationLabel;
    private Label _resultTitleLabel;
    private Label _resultScoreLabel;
    private Label _commentLabel;
    private Button _restartButton;

    private readonly List<QuizQuestion> _questions = new List<QuizQuestion>();

        private QuizQuestionService _questionService;
    private IClaudeApiClient _apiClient;

    private readonly List<Label> _loadingDots = new List<Label>();
    private IVisualElementScheduledItem _loadingDotSchedule;
private GamePhase _phase;

    private string _selectedTopic;
    private int _currentIndex;
    private int _correctCount;
    private float _remainingTime;
    private bool _isRunning;
    private bool _hasAnsweredCurrentQuestion;

    private void Awake()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        if (_uiDocument == null)
        {
            Debug.LogError("UIDocument가 할당되지 않았습니다.", this);
            return;
        }

        if (_quizSettings == null)
        {
            Debug.LogError("QuizSettings가 할당되지 않았습니다.", this);
            return;
        }

                if (_apiClientObject == null)
        {
            _apiClientObject = GetComponent<ClaudeApiClient>();
        }

        _apiClient = _apiClientObject as IClaudeApiClient;
        if (_apiClient == null)
        {
            Debug.LogError("IClaudeApiClient를 구현한 컴포넌트를 찾지 못했습니다.", this);
            return;
        }

        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("rootVisualElement를 가져오지 못했습니다.", this);
            return;
        }

        _quizRoot = root.Q<VisualElement>("quiz-root");
        if (_quizRoot == null)
        {
            Debug.LogError("quiz-root 요소를 찾지 못했습니다.", this);
            return;
        }

        _topicSelectPanel = _quizRoot.Q<VisualElement>("topic-select-panel");
        _loadingPanel = _quizRoot.Q<VisualElement>("loading-panel");
        _gamePanel = _quizRoot.Q<VisualElement>("game-panel");
        _resultPanel = _quizRoot.Q<VisualElement>("result-panel");
        _explanationPanel = _quizRoot.Q<VisualElement>("explanation-panel");

        _topicButtonsContainer = _quizRoot.Q<VisualElement>("topic-buttons-container");
        _choicesContainer = _quizRoot.Q<VisualElement>("choices-container");

        _topicBadge = _quizRoot.Q<Label>("topic-badge");
        _questionCounterLabel = _quizRoot.Q<Label>("question-counter");
        _questionLabel = _quizRoot.Q<Label>("question-label");
        _timerLabel = _quizRoot.Q<Label>("timer-label");
        _scoreLabel = _quizRoot.Q<Label>("score-label");
        _explanationLabel = _quizRoot.Q<Label>("explanation-label");
        _resultTitleLabel = _quizRoot.Q<Label>("result-title");
        _resultScoreLabel = _quizRoot.Q<Label>("result-score");
        _commentLabel = _quizRoot.Q<Label>("comment-label");
        
        VisualElement loadingDots = _quizRoot.Q<VisualElement>("loading-dots");
        if (loadingDots != null)
        {
            _loadingDots.Clear();
            for (int i = 0; i < 3; i++)
            {
                Label dot = loadingDots.Q<Label>($"dot-{i}");
                if (dot != null)
                {
                    _loadingDots.Add(dot);
                }
            }
        }
_restartButton = _quizRoot.Q<Button>("restart-button");

        if (_topicButtonsContainer == null ||
            _choicesContainer == null ||
            _topicSelectPanel == null ||
            _loadingPanel == null ||
            _gamePanel == null ||
            _resultPanel == null ||
            _explanationPanel == null ||
            _questionLabel == null ||
            _timerLabel == null ||
            _scoreLabel == null ||
            _questionCounterLabel == null ||
            _resultTitleLabel == null ||
            _resultScoreLabel == null ||
            _restartButton == null)
        {
            Debug.LogError("QuizGame UI 요소를 일부 찾지 못했습니다.", this);
            return;
        }

        _topicButtons.Clear();
        for (int i = 0; i < 5; i++)
        {
            Button topicButton = _topicButtonsContainer.Q<Button>($"topic-{i}");
            if (topicButton != null)
            {
                int index = i;
                topicButton.clicked += () => OnTopicSelected(index);
                _topicButtons.Add(topicButton);
            }
        }

        _choiceButtons.Clear();
        for (int i = 0; i < 4; i++)
        {
            Button button = _choicesContainer.Q<Button>($"choice-{i}");
            if (button != null)
            {
                int index = i;
                button.clicked += () => OnChoiceClicked(index);
                _choiceButtons.Add(button);
            }
        }

        if (_restartButton != null)
        {
            _restartButton.clicked += OnRestartClicked;
        }

        if (_quizSettings.Topics != null)
        {
            for (int i = 0; i < _topicButtons.Count; i++)
            {
                if (i < _quizSettings.Topics.Length)
                {
                    _topicButtons[i].text = _quizSettings.Topics[i];
                    _topicButtons[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    _topicButtons[i].style.display = DisplayStyle.None;
                }
            }
        }
    }

    private void Start()
    {
        _questionService = new QuizQuestionService(_apiClient, _quizSettings);
        ShowPhase(GamePhase.TopicSelect);
    }

    public UniTask InitializeAsync()
    {
        _questionService = new QuizQuestionService(_apiClient, _quizSettings);
        _questions.Clear();
        _currentIndex = 0;
        _correctCount = 0;
        _isRunning = false;
        _hasAnsweredCurrentQuestion = false;
        ShowPhase(GamePhase.TopicSelect);
        return UniTask.CompletedTask;
    }

    public void OnGameStart()
    {
        _questions.Clear();
        _currentIndex = 0;
        _correctCount = 0;
        _isRunning = false;
        _hasAnsweredCurrentQuestion = false;
        ShowPhase(GamePhase.TopicSelect);
    }

    public void OnGameEnd()
    {
        _isRunning = false;

        if (_questions.Count == 0)
        {
            return;
        }

        int totalCount = _questions.Count;
        if (_resultTitleLabel != null)
        {
            _resultTitleLabel.text = _correctCount >= totalCount * 0.6f ? "🎉 클리어!" : "😢 실패";
        }

        if (_resultScoreLabel != null)
        {
            _resultScoreLabel.text = $"{_correctCount} / {totalCount} 정답";
        }

        if (_commentLabel != null)
        {
            _commentLabel.text = "총평을 불러오는 중...";
        }

        ShowPhase(GamePhase.Result);
        FetchAndShowCommentAsync().Forget();
    }

    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        if (_phase != GamePhase.Playing)
        {
            return;
        }

        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0f)
        {
            _remainingTime = 0f;
        }

        if (_timerLabel != null)
        {
            _timerLabel.text = $"{_remainingTime:0.0}s";
        }

        if (_remainingTime <= 0f && !_hasAnsweredCurrentQuestion)
        {
            HandleTimeout();
        }
    }

    private void OnTopicSelected(int index)
    {
        if (_quizSettings.Topics == null || index < 0 || index >= _quizSettings.Topics.Length)
        {
            return;
        }

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
        {
            loaded = await _questionService.GetQuestionsAsync(_selectedTopic, _quizSettings.QuestionCount);
        }

        if (loaded == null || loaded.Count == 0)
        {
            Debug.LogWarning("퀴즈를 불러오지 못해 더미 데이터를 사용합니다.", this);
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
        if (_currentIndex < 0 || _currentIndex >= _questions.Count)
        {
            return;
        }

        QuizQuestion question = _questions[_currentIndex];

        if (_topicBadge != null)
        {
            _topicBadge.text = string.IsNullOrEmpty(question.Topic) ? _selectedTopic : question.Topic;
        }

        if (_questionCounterLabel != null)
        {
            _questionCounterLabel.text = $"{_currentIndex + 1} / {_questions.Count}";
        }

        if (_questionLabel != null)
        {
            _questionLabel.text = question.Question;
        }

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            Button button = _choiceButtons[i];
            button.RemoveFromClassList("choice-correct");
            button.RemoveFromClassList("choice-wrong");
            button.RemoveFromClassList("choice-correct-highlight");

            if (i < question.Choices.Length)
            {
                button.text = question.Choices[i];
                button.SetEnabled(true);
            }
            else
            {
                button.text = string.Empty;
                button.SetEnabled(false);
            }
        }

        if (_explanationPanel != null)
        {
            _explanationPanel.style.display = DisplayStyle.None;
        }

        _remainingTime = _quizSettings.TimeLimitSeconds;
        if (_timerLabel != null)
        {
            _timerLabel.text = $"{_remainingTime:0.0}s";
        }

        if (_scoreLabel != null)
        {
            _scoreLabel.text = $"{_correctCount} / {_questions.Count}";
        }

        _hasAnsweredCurrentQuestion = false;
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (!_isRunning)
        {
            return;
        }

        if (_phase != GamePhase.Playing)
        {
            return;
        }

        if (_hasAnsweredCurrentQuestion)
        {
            return;
        }

        if (_currentIndex < 0 || _currentIndex >= _questions.Count)
        {
            return;
        }

        _hasAnsweredCurrentQuestion = true;

        QuizQuestion question = _questions[_currentIndex];
        bool isCorrect = choiceIndex == question.AnswerIndex;

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            Button button = _choiceButtons[i];
            button.RemoveFromClassList("choice-correct");
            button.RemoveFromClassList("choice-wrong");
            button.RemoveFromClassList("choice-correct-highlight");
        }

        if (choiceIndex >= 0 && choiceIndex < _choiceButtons.Count)
        {
            Button chosen = _choiceButtons[choiceIndex];
            if (isCorrect)
            {
                chosen.AddToClassList("choice-correct");
            }
            else
            {
                chosen.AddToClassList("choice-wrong");
            }
        }

        if (!isCorrect && question.AnswerIndex >= 0 && question.AnswerIndex < _choiceButtons.Count)
        {
            Button correctButton = _choiceButtons[question.AnswerIndex];
            correctButton.AddToClassList("choice-correct-highlight");
        }

        if (isCorrect)
        {
            _correctCount++;
        }

        if (_scoreLabel != null)
        {
            _scoreLabel.text = $"{_correctCount} / {_questions.Count}";
        }

        if (_explanationPanel != null)
        {
            _explanationPanel.style.display = DisplayStyle.Flex;
        }

        if (_explanationLabel != null)
        {
            _explanationLabel.text = question.Explanation;
        }

        _phase = GamePhase.Explanation;
        _isRunning = false;
        DelayNextQuestion().Forget();
    }

    private void HandleTimeout()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count)
        {
            return;
        }

        _hasAnsweredCurrentQuestion = true;
        _isRunning = false;

        QuizQuestion question = _questions[_currentIndex];

        if (question.AnswerIndex >= 0 && question.AnswerIndex < _choiceButtons.Count)
        {
            Button correctButton = _choiceButtons[question.AnswerIndex];
            correctButton.AddToClassList("choice-correct-highlight");
        }

        if (_explanationPanel != null)
        {
            _explanationPanel.style.display = DisplayStyle.Flex;
        }

        if (_explanationLabel != null)
        {
            _explanationLabel.text = $"시간 초과! {question.Explanation}";
        }

        _phase = GamePhase.Explanation;
        DelayNextQuestion().Forget();
    }

    private async UniTaskVoid DelayNextQuestion()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(2));

        if (_phase != GamePhase.Explanation)
        {
            return;
        }

        NextQuestion();
    }

    private void NextQuestion()
    {
        _currentIndex++;

        if (_currentIndex >= _questions.Count)
        {
            OnGameEnd();
            return;
        }

        _phase = GamePhase.Playing;
        _isRunning = true;
        ShowCurrentQuestion();
    }

    private void OnRestartClicked()
    {
        OnGameStart();
    }

    private void ShowPhase(GamePhase phase)
    {
        _phase = phase;

        SetPanelDisplay(_topicSelectPanel, phase == GamePhase.TopicSelect);
        SetPanelDisplay(_loadingPanel, phase == GamePhase.Loading);
        SetPanelDisplay(_gamePanel, phase == GamePhase.Playing || phase == GamePhase.Explanation);
        SetPanelDisplay(_resultPanel, phase == GamePhase.Result);

        if (phase == GamePhase.Loading)
        {
            StartLoadingDots();
        }
        else
        {
            StopLoadingDots();
        }
    }

    private static void SetPanelDisplay(VisualElement panel, bool visible)
    {
        if (panel == null)
        {
            return;
        }

        panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private async UniTaskVoid FetchAndShowCommentAsync()
    {
        if (_commentLabel == null)
        {
            return;
        }

        if (_questionService == null)
        {
            _commentLabel.text = "총평을 불러오지 못했습니다.";
            return;
        }

        string comment = await _questionService.GetCommentAsync(_correctCount, _questions.Count, _selectedTopic);
        if (string.IsNullOrEmpty(comment))
        {
            _commentLabel.text = "총평을 불러오지 못했습니다.";
        }
        else
        {
            _commentLabel.text = comment;
        }
    }

    private void CreateDummyQuestions()
    {
        _questions.Clear();

        _questions.Add(new QuizQuestion
        {
            Topic = "상식",
            Question = "태양계에서 가장 큰 행성은 무엇일까요?",
            Choices = new[]
            {
                "지구",
                "목성",
                "토성",
                "금성"
            },
            AnswerIndex = 1,
            Explanation = "목성은 태양계에서 가장 큰 가스 행성입니다."
        });

        _questions.Add(new QuizQuestion
        {
            Topic = "과학",
            Question = "물의 화학식은 무엇일까요?",
            Choices = new[]
            {
                "CO2",
                "H2O",
                "O2",
                "NaCl"
            },
            AnswerIndex = 1,
            Explanation = "물은 두 개의 수소 원자와 한 개의 산소 원자로 이루어져 있습니다."
        });

        _questions.Add(new QuizQuestion
        {
            Topic = "역사",
            Question = "고려를 세운 왕은 누구일까요?",
            Choices = new[]
            {
                "이성계",
                "세종대왕",
                "왕건",
                "광개토대왕"
            },
            AnswerIndex = 2,
            Explanation = "왕건이 후삼국을 통일하고 고려를 세웠습니다."
        });
    }


    private void StopLoadingDots()
    {
        if (_loadingDotSchedule != null)
        {
            _loadingDotSchedule.Pause();
            _loadingDotSchedule = null;
        }

        for (int i = 0; i < _loadingDots.Count; i++)
        {
            _loadingDots[i].style.opacity = 1f;
        }
    }


    private void StartLoadingDots()
    {
        if (_loadingDots.Count == 0)
        {
            return;
        }

        StopLoadingDots();

        int step = 0;
        _loadingDotSchedule = _loadingDots[0].schedule.Execute(() =>
        {
            step++;
            int active = (step % 3) + 1;
            for (int i = 0; i < _loadingDots.Count; i++)
            {
                _loadingDots[i].style.opacity = i < active ? 1f : 0.2f;
            }
        }).Every(400);
    }


    private void OnDestroy()
    {
        StopLoadingDots();
    }
}


