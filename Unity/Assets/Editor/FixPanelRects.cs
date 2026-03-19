using UnityEditor;
using UnityEngine;

/// <summary>
/// QuizGame.prefab 내 4개 패널의 RectTransform을 fullscreen stretch로 수정한다.
/// </summary>
public static class FixPanelRects
{
    [MenuItem("Tools/Fix Panel Rects")]
    public static void Fix()
    {
        // QuizTopicSelect, Loading, Gameplay, Result 개별 프리팹 수정
        string[] prefabPaths = {
            "Assets/UI/Prefabs/Quiz/QuizTopicSelect.prefab",
            "Assets/UI/Prefabs/Quiz/QuizLoading.prefab",
            "Assets/UI/Prefabs/Quiz/QuizGameplay.prefab",
            "Assets/UI/Prefabs/Quiz/QuizResult.prefab",
        };

        foreach (string path in prefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            SetFullscreen(root.GetComponent<RectTransform>(), path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // QuizGame.prefab 내부 패널 인스턴스도 fullscreen으로
        string quizGamePath = "Assets/UI/Prefabs/Quiz/QuizGame.prefab";
        GameObject quizGameRoot = PrefabUtility.LoadPrefabContents(quizGamePath);

        string[] panelNames = { "QuizTopicSelect", "QuizLoading", "QuizGameplay", "QuizResult" };
        foreach (string panelName in panelNames)
        {
            Transform panel = quizGameRoot.transform.Find(panelName);
            if (panel != null)
            {
                SetFullscreen(panel.GetComponent<RectTransform>(), panelName);
            }
            else
            {
                Debug.LogWarning($"{panelName}을 QuizGame.prefab에서 찾지 못했습니다.");
            }
        }

        // QuizGame 루트도 fullscreen
        SetFullscreen(quizGameRoot.GetComponent<RectTransform>(), "QuizGame root");

        PrefabUtility.SaveAsPrefabAsset(quizGameRoot, quizGamePath);
        PrefabUtility.UnloadPrefabContents(quizGameRoot);

        AssetDatabase.SaveAssets();
        Debug.Log("Fix Panel Rects 완료.");
    }

    private static void SetFullscreen(RectTransform rt, string name)
    {
        if (rt == null)
        {
            Debug.LogWarning($"RectTransform 없음: {name}");
            return;
        }
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
        Debug.Log($"Fullscreen 설정: {name}");
    }
}
