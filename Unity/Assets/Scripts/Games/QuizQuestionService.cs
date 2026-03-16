using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class QuizQuestionService
{
    private readonly SupabaseQuizClient _supabaseClient;
    private readonly QuizSettings _quizSettings;
    // 세션 내 중복 호출 방지용 인메모리 캐시
    private readonly Dictionary<string, List<QuizQuestion>> _memoryCache = new Dictionary<string, List<QuizQuestion>>();

    public QuizQuestionService(SupabaseQuizClient supabaseClient, QuizSettings quizSettings)
    {
        _supabaseClient = supabaseClient;
        _quizSettings = quizSettings;
    }

    public async UniTask<List<QuizQuestion>> GetQuestionsAsync(string topic, int questionCount)
    {
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogError("토픽이 비어 있습니다.");
            return null;
        }

        // 인메모리 캐시 확인
        if (_memoryCache.TryGetValue(topic, out List<QuizQuestion> cached) && cached.Count >= questionCount)
        {
            return cached.OrderBy(_ => Guid.NewGuid()).Take(questionCount).ToList();
        }

        if (_supabaseClient == null)
        {
            Debug.LogError("SupabaseQuizClient가 null입니다.");
            return null;
        }

        List<QuizQuestion> questions = await _supabaseClient.GenerateQuestionsAsync(topic, questionCount);
        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("Supabase 퀴즈 생성 실패.");
            return null;
        }

        _memoryCache[topic] = questions;
        return questions.OrderBy(_ => Guid.NewGuid()).Take(questionCount).ToList();
    }

    public async UniTask<string> GetCommentAsync(int correctCount, int totalCount, string topic)
    {
        if (_supabaseClient == null)
        {
            Debug.LogError("SupabaseQuizClient가 null입니다.");
            return null;
        }

        return await _supabaseClient.GenerateCommentAsync(topic, correctCount, totalCount);
    }
}
