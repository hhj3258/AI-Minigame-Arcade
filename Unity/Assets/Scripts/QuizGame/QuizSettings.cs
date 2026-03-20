using UnityEngine;

[System.Serializable]
public struct TopicEntry
{
    public string Emoji;
    public string Name;
}

[CreateAssetMenu(fileName = "QuizSettings", menuName = "AIMiniArcade/QuizSettings")]
public class QuizSettings : ScriptableObject
{
    public int QuestionCount = 5;
    public float TimeLimitSeconds = 20f;
    public TopicEntry[] Topics =
    {
        new TopicEntry { Emoji = "🌍", Name = "세계 역사" },
        new TopicEntry { Emoji = "🔬", Name = "과학 상식" },
        new TopicEntry { Emoji = "🎬", Name = "영화 / 드라마" },
        new TopicEntry { Emoji = "⚽",  Name = "스포츠" },
        new TopicEntry { Emoji = "🎵", Name = "K-POP" },
    };
}
