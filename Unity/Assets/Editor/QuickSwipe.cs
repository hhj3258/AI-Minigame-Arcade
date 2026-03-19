using UnityEditor;
using UnityEngine;
public static class QuickSwipe
{
    [MenuItem("Tools/Quick Swipe Survivor")]
    public static void Go() => Object.FindFirstObjectByType<CardSwipeController>()?.SwipeToCard(1);
}
