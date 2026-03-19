using UnityEditor;
using UnityEngine;

public static class CaptureGameView
{
    [MenuItem("Tools/Capture Game View")]
    public static void Capture()
    {
        string path = "Assets/Screenshots/gameview_capture.png";
        ScreenCapture.CaptureScreenshot(path, 1);
        Debug.Log($"Game View 캡처 저장: {path}");
    }
}
