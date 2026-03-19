using System;
using Newtonsoft.Json;

[Serializable]
public class QuizQuestion
{
    [JsonProperty("topic")]
    public string Topic;

    [JsonProperty("question")]
    public string Question;

    [JsonProperty("choices")]
    public string[] Choices;

    [JsonProperty("answerIndex")]
    public int AnswerIndex;

    [JsonProperty("explanation")]
    public string Explanation;
}

