using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public interface IClaudeApiClient
{
    UniTask<string> SendMessageAsync(string systemPrompt, List<ChatMessage> messages);
}

[Serializable]
internal class ClaudeConfig
{
    [JsonProperty("apiKey")]
    public string ApiKey;
}

[Serializable]
internal class ClaudeMessageContent
{
    [JsonProperty("type")]
    public string Type = "text";

    [JsonProperty("text")]
    public string Text;
}

[Serializable]
internal class ClaudeMessage
{
    [JsonProperty("role")]
    public string Role;

    [JsonProperty("content")]
    public List<ClaudeMessageContent> Content;
}

[Serializable]
internal class ClaudeRequest
{
    [JsonProperty("model")]
    public string Model;

    [JsonProperty("max_tokens")]
    public int MaxTokens;

    [JsonProperty("system")]
    public string System;

    [JsonProperty("messages")]
    public List<ClaudeMessage> Messages;
}

[Serializable]
internal class ClaudeContentItem
{
    [JsonProperty("type")]
    public string Type;

    [JsonProperty("text")]
    public string Text;
}

[Serializable]
internal class ClaudeResponse
{
    [JsonProperty("content")]
    public List<ClaudeContentItem> Content;
}

public class ClaudeApiClient : MonoBehaviour, IClaudeApiClient
{
    [SerializeField]
    private ApiSettings _apiSettings;

    private string _apiKey;

    private void Awake()
    {
        if (_apiSettings == null)
        {
            Debug.LogError("ApiSettings가 할당되지 않았습니다.", this);
            return;
        }
    }

    public async UniTask<string> SendMessageAsync(string systemPrompt, List<ChatMessage> messages)
    {
        if (_apiSettings == null)
        {
            Debug.LogError("ApiSettings가 null입니다.", this);
            return null;
        }

        if (messages == null || messages.Count == 0)
        {
            Debug.LogError("messages 리스트가 비어 있습니다.", this);
            return null;
        }

        if (string.IsNullOrEmpty(systemPrompt))
        {
            Debug.LogError("systemPrompt가 비어 있습니다.", this);
            return null;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = await LoadApiKeyAsync();
            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogError("Claude API Key를 로드하지 못했습니다.", this);
                return null;
            }
        }

        ClaudeRequest requestBody = BuildRequest(systemPrompt, messages);
        string json = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(_apiSettings.Endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = _apiSettings.TimeoutSeconds;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", _apiKey);
            request.SetRequestHeader("anthropic-version", "2023-06-01");

            try
            {
                await request.SendWebRequest().ToUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Claude API 요청 중 예외 발생: {ex.Message}", this);
                return null;
            }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Claude API 요청 실패: {request.error}", this);
                return null;
            }

            string responseText = request.downloadHandler.text;
            return ParseResponseText(responseText);
        }
    }

    private ClaudeRequest BuildRequest(string systemPrompt, List<ChatMessage> messages)
    {
        List<ClaudeMessage> apiMessages = new List<ClaudeMessage>();

        foreach (ChatMessage chat in messages)
        {
            if (chat == null)
            {
                continue;
            }

            ClaudeMessage message = new ClaudeMessage
            {
                Role = chat.Role,
                Content = new List<ClaudeMessageContent>
                {
                    new ClaudeMessageContent
                    {
                        Text = chat.Content
                    }
                }
            };

            apiMessages.Add(message);
        }

        ClaudeRequest request = new ClaudeRequest
        {
            Model = _apiSettings.Model,
            MaxTokens = _apiSettings.MaxTokens,
            System = systemPrompt,
            Messages = apiMessages
        };

        return request;
    }

    private async UniTask<string> LoadApiKeyAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "claude_config.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"claude_config.json 파일을 찾을 수 없습니다: {path}", this);
            return null;
        }

        try
        {
            string json = await UniTask.Run(() => File.ReadAllText(path, Encoding.UTF8));
            ClaudeConfig config = JsonConvert.DeserializeObject<ClaudeConfig>(json);
            if (config == null || string.IsNullOrEmpty(config.ApiKey))
            {
                Debug.LogError("claude_config.json에서 apiKey를 읽지 못했습니다.", this);
                return null;
            }

            return config.ApiKey;
        }
        catch (Exception ex)
        {
            Debug.LogError($"claude_config.json 로드 중 예외 발생: {ex.Message}", this);
            return null;
        }
    }

    private string ParseResponseText(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogError("응답 본문이 비어 있습니다.", this);
            return null;
        }

        try
        {
            ClaudeResponse response = JsonConvert.DeserializeObject<ClaudeResponse>(responseText);
            if (response == null || response.Content == null || response.Content.Count == 0)
            {
                Debug.LogError("Claude 응답에서 content를 찾지 못했습니다.", this);
                return null;
            }

            ClaudeContentItem first = response.Content[0];
            if (first == null || string.IsNullOrEmpty(first.Text))
            {
                Debug.LogError("Claude 응답의 첫 content에 text가 없습니다.", this);
                return null;
            }

            return first.Text;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Claude 응답 파싱 중 예외 발생: {ex.Message}", this);
            return null;
        }
    }
}

