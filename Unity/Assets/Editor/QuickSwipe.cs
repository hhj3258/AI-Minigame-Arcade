using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 플레이 중 원하는 게임 카드로 즉시 이동.
/// 카드 추가 시 아래에 MenuItem 항목 추가.
/// </summary>
public static class QuickSwipe
{
    [MenuItem("Tools/빠른 이동/0 퀴즈 게임")]
    public static void GoQuiz() => Swipe(0);

    [MenuItem("Tools/빠른 이동/1 서바이버 게임")]
    public static void GoSurvivor() => Swipe(1);

    private static void Swipe(int index)
    {
        var controller = Object.FindFirstObjectByType<CardSwipeController>();
        if (controller == null)
        {
            Debug.LogWarning("CardSwipeController를 찾을 수 없습니다. Play mode인지 확인하세요.");
            return;
        }
        controller.SwipeToCard(index);
    }
}
