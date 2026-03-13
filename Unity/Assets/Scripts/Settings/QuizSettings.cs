using UnityEngine;

[CreateAssetMenu(fileName = "QuizSettings", menuName = "AIMiniArcade/QuizSettings")]
public class QuizSettings : ScriptableObject
{
    public int QuestionCount = 5;
    public float TimeLimitSeconds = 20f;
    public string[] Topics = { "역사", "과학", "상식", "문화", "스포츠" };
}

