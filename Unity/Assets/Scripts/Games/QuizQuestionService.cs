using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class QuizQuestionService
{
    private readonly IClaudeApiClient _apiClient;
    private readonly QuizSettings _quizSettings;
    private QuizCacheData _cacheData;
    private readonly string _cachePath;

    public QuizQuestionService(IClaudeApiClient apiClient, QuizSettings quizSettings)
    {
        _apiClient = apiClient;
        _quizSettings = quizSettings;
        _cachePath = Path.Combine(Application.persistentDataPath, "quiz_cache.json");
    }

    public async UniTask<List<QuizQuestion>> GetQuestionsAsync(string topic, int questionCount)
    {
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogError("토픽이 비어 있습니다.");
            return null;
        }

        await LoadCacheFromDisk();

        List<QuizQuestion> cached = GetCachedQuestions(topic);
        if (cached.Count >= questionCount * 3)
        {
            return cached.OrderBy(_ => Guid.NewGuid()).Take(questionCount).ToList();
        }

        List<QuizQuestion> generated = await GenerateQuestionsFromClaudeAsync(topic);
        if (generated == null || generated.Count == 0)
        {
            Debug.LogError("Claude 퀴즈 생성 실패, 폴백 필요.");
            return null;
        }

        AddToCache(topic, generated);
        await SaveCacheToDisk();

        return generated.OrderBy(_ => Guid.NewGuid()).Take(questionCount).ToList();
    }

    public async UniTask<string> GetCommentAsync(int correctCount, int totalCount, string topic)
    {
        if (_apiClient == null)
        {
            Debug.LogError("IClaudeApiClient가 null입니다.");
            return null;
        }

        List<ChatMessage> messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "user",
                Content = $"주제: {topic}, 점수: {correctCount} / {totalCount}"
            }
        };

        string result = await _apiClient.SendMessageAsync(ClaudePrompts.QuizComment, messages);
        return result;
    }

    private async UniTask LoadCacheFromDisk()
    {
        if (_cacheData != null)
        {
            return;
        }

        if (!File.Exists(_cachePath))
        {
            _cacheData = new QuizCacheData();
            return;
        }

        try
        {
            string json = await UniTask.Run(() => File.ReadAllText(_cachePath));
            _cacheData = JsonConvert.DeserializeObject<QuizCacheData>(json) ?? new QuizCacheData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"퀴즈 캐시 로드 실패: {ex.Message}");
            _cacheData = new QuizCacheData();
        }
    }

    private async UniTask SaveCacheToDisk()
    {
        if (_cacheData == null)
        {
            return;
        }

        try
        {
            string json = JsonConvert.SerializeObject(_cacheData, Formatting.Indented);
            await UniTask.Run(() => File.WriteAllText(_cachePath, json));
        }
        catch (Exception ex)
        {
            Debug.LogError($"퀴즈 캐시 저장 실패: {ex.Message}");
        }
    }

    private List<QuizQuestion> GetCachedQuestions(string topic)
    {
        if (_cacheData == null || _cacheData.Entries == null)
        {
            return new List<QuizQuestion>();
        }

        QuizCacheEntry entry = _cacheData.Entries.FirstOrDefault(e => e.Topic == topic);
        return entry?.Questions ?? new List<QuizQuestion>();
    }

    private void AddToCache(string topic, List<QuizQuestion> questions)
    {
        if (_cacheData == null)
        {
            _cacheData = new QuizCacheData();
        }

        QuizCacheEntry existing = _cacheData.Entries.FirstOrDefault(e => e.Topic == topic);
        if (existing == null)
        {
            existing = new QuizCacheEntry
            {
                Topic = topic,
                Questions = new List<QuizQuestion>()
            };
            _cacheData.Entries.Add(existing);
        }

        existing.Questions.AddRange(questions);
    }

    private async UniTask<List<QuizQuestion>> GenerateQuestionsFromClaudeAsync(string topic)
    {
        if (_apiClient == null)
        {
            Debug.LogError("ClaudeApiClient가 null입니다.");
            return null;
        }

        if (_quizSettings == null)
        {
            Debug.LogError("QuizSettings가 null입니다.");
            return null;
        }

        List<ChatMessage> messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "user",
                Content = $"주제 '{topic}'에 대한 4지선다 퀴즈를 {_quizSettings.QuestionCount}개 생성해 주세요."
            }
        };

        string raw;
        try
        {
            raw = await _apiClient.SendMessageAsync(ClaudePrompts.QuizGenerate, messages);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Claude 퀴즈 생성 요청 중 예외 발생: {ex.Message}");
            return null;
        }

        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return ParseQuizJson(raw);
    }

    public List<QuizQuestion> ParseQuizJson(string response)
    {
        try
        {
            int start = response.IndexOf('[');
            int end = response.LastIndexOf(']');

            if (start < 0 || end <= start)
            {
                Debug.LogError($"퀴즈 JSON에서 배열 구간을 찾지 못했습니다: {response}");
                return new List<QuizQuestion>();
            }

            string json = response.Substring(start, end - start + 1);
            List<QuizQuestion> list = JsonConvert.DeserializeObject<List<QuizQuestion>>(json);
            return list ?? new List<QuizQuestion>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"퀴즈 JSON 파싱 실패: {ex.Message}");
            return new List<QuizQuestion>();
        }
    }
}

