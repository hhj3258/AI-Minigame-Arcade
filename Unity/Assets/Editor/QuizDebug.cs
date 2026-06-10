using UnityEditor;
using UnityEngine;

public static class QuizDebug
{
    [MenuItem("Tools/퀴즈 디버그/결과 화면 (통과 3점)")]
    static void JumpPass() => Jump(3);

    [MenuItem("Tools/퀴즈 디버그/결과 화면 (실패 0점)")]
    static void JumpFail() => Jump(0);

    static void Jump(int correct)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[QuizDebug] Play mode에서만 사용할 수 있습니다.");
            return;
        }

        var quiz = Object.FindFirstObjectByType<QuizGame>();
        if (quiz == null)
        {
            Debug.LogWarning("[QuizDebug] QuizGame을 찾을 수 없습니다. 퀴즈 카드가 활성화되어 있는지 확인하세요.");
            return;
        }

        quiz.DebugJumpToResult(correct);
        Debug.Log($"[QuizDebug] 결과 화면으로 이동 ({correct}/3)");
    }
}
