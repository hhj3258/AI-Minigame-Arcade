using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseQuizClient : MonoBehaviour
{
    [SerializeField]
    private SupabaseSettings _settings;

    private void Awake()
    {
        if (_settings == null)
        {
            Debug.LogError("SupabaseSettings가 할당되지 않았습니다.", this);
        }
    }

    public async UniTask<List<QuizQuestion>> GenerateQuestionsAsync(string topic, int count)
    {
        if (_settings == null)
        {
            Debug.LogError("SupabaseSettings가 null입니다.", this);
            return null;
        }

        string url = $"{_settings.ProjectUrl}/functions/v1/generate-quiz";
        string body = JsonConvert.SerializeObject(new { topic, count });

        string response = await PostAsync(url, body);
        if (response == null)
        {
            return null;
        }

        try
        {
            List<QuizQuestion> questions = JsonConvert.DeserializeObject<List<QuizQuestion>>(response);
            return questions;
        }
        catch (Exception ex)
        {
            Debug.LogError($"generate-quiz 응답 파싱 실패: {ex.Message}\n{response}");
            return null;
        }
    }

    public async UniTask<string> GenerateCommentAsync(string topic, int correctCount, int totalCount)
    {
        if (_settings == null)
        {
            Debug.LogError("SupabaseSettings가 null입니다.", this);
            return null;
        }

        string url = $"{_settings.ProjectUrl}/functions/v1/generate-comment";
        string body = JsonConvert.SerializeObject(new { topic, correctCount, totalCount });

        string response = await PostAsync(url, body);
        if (response == null)
        {
            return null;
        }

        try
        {
            var parsed = JsonConvert.DeserializeObject<CommentResponse>(response);
            return parsed?.Comment;
        }
        catch (Exception ex)
        {
            Debug.LogError($"generate-comment 응답 파싱 실패: {ex.Message}\n{response}");
            return null;
        }
    }

    private async UniTask<string> PostAsync(string url, string body)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = _settings.TimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_settings.AnonKey}");

            try
            {
                await request.SendWebRequest().ToUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Supabase 요청 중 예외 발생: {ex.Message}", this);
                return null;
            }

#if UNITY_2020_1_OR_NEWER
            bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
            bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

            if (!isSuccess)
            {
                Debug.LogError($"Supabase Edge Function 요청 실패 ({request.responseCode}): {request.downloadHandler.text}", this);
                return null;
            }

            if (request.GetResponseHeader("X-Gemini-Fallback") == "true")
                Debug.LogWarning("[Supabase] Gemini API 한도 초과 — DB 저장 데이터로 대체 응답합니다.", this);

            return request.downloadHandler.text;
        }
    }

    [Serializable]
    private class CommentResponse
    {
        [JsonProperty("comment")]
        public string Comment;
    }
}
