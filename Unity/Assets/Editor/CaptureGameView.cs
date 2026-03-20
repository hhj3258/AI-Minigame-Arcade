using System;
using UnityEditor;
using UnityEngine;

public static class CaptureGameView
{
    [MenuItem("Tools/Game View 캡처")]
    public static void Capture()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = $"Assets/Screenshots/gameview_{timestamp}.png";
        ScreenCapture.CaptureScreenshot(path, 1);
        Debug.Log($"Game View 캡처 저장: {path}");
    }
}
