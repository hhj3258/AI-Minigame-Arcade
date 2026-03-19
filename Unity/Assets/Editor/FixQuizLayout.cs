using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 퀴즈 UI 레이아웃 전체 수정 — 배경색, 버튼 크기, 여백 등 디자인 토큰 적용
/// </summary>
public static class FixQuizLayout
{
    // 디자인 토큰
    private static readonly Color ColBackground    = new Color32(0xf0, 0xf4, 0xff, 0xff); // #f0f4ff
    private static readonly Color ColCardBg        = new Color32(0xff, 0xff, 0xff, 0xff); // #ffffff
    private static readonly Color ColPrimary       = new Color32(0x5b, 0x6e, 0xf5, 0xff); // #5b6ef5
    private static readonly Color ColText          = new Color32(0x1e, 0x1e, 0x2e, 0xff); // #1e1e2e
    private static readonly Color ColTextSecondary = new Color32(0x7c, 0x7f, 0x9e, 0xff); // #7c7f9e
    private static readonly Color ColButtonDefaultBg = new Color32(0xef, 0xf1, 0xff, 0xff); // #eff1ff

    [MenuItem("Tools/Fix Quiz Layout")]
    public static void Fix()
    {
        FixScene();
        FixTopicSelectPrefab();
        FixLoadingPrefab();
        FixGameplayPrefab();
        FixResultPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fix Quiz Layout 완료.");
    }

    // ─── 씬 ───────────────────────────────────────────────────────────────
    private static void FixScene()
    {
        // 카메라 배경색
        Camera cam = Camera.main;
        if (cam != null) cam.backgroundColor = ColBackground;

        // SafeAreaContainer 배경 Image
        GameObject sac = GameObject.Find("UIRoot/SafeAreaContainer");
        if (sac != null)
        {
            Image img = sac.GetComponent<Image>();
            if (img != null) img.color = ColBackground;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("씬 배경색 설정 완료.");
    }

    // ─── TopicSelect ───────────────────────────────────────────────────────
    private static void FixTopicSelectPrefab()
    {
        const string path = "Assets/UI/Prefabs/Quiz/QuizTopicSelect.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        // 루트 배경
        Image rootImg = root.GetComponent<Image>();
        if (rootImg != null) rootImg.color = ColCardBg;

        // 루트 VerticalLayoutGroup
        VerticalLayoutGroup rootVlg = root.GetComponent<VerticalLayoutGroup>();
        if (rootVlg != null)
        {
            rootVlg.padding = new RectOffset(60, 60, 100, 80);
            rootVlg.spacing = 50;
            rootVlg.childForceExpandHeight = false;
            rootVlg.childForceExpandWidth = true;
            rootVlg.childControlHeight = true;
            rootVlg.childControlWidth = true;
        }

        // TitleText
        Transform titleText = root.transform.Find("TitleText");
        if (titleText != null)
        {
            TextMeshProUGUI tmp = titleText.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = ColText;

            LayoutElement le = titleText.GetComponent<LayoutElement>();
            if (le == null) le = titleText.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 160;
        }

        // TopicButtonsContainer
        Transform container = root.transform.Find("TopicButtonsContainer");
        if (container != null)
        {
            // 루트 VLG가 childControlHeight=true이므로 container 자체에도 LayoutElement 필요
            LayoutElement containerLe = container.GetComponent<LayoutElement>();
            if (containerLe == null) containerLe = container.gameObject.AddComponent<LayoutElement>();
            containerLe.flexibleHeight = 1; // 남은 공간 모두 차지

            VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 24;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
            }

            // 각 버튼
            for (int i = 0; i < container.childCount; i++)
            {
                Transform btn = container.GetChild(i);

                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null) btnImg.color = ColButtonDefaultBg;

                LayoutElement le = btn.GetComponent<LayoutElement>();
                if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 160;

                // 버튼 자식 Text 색상
                TextMeshProUGUI btnTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTmp != null) btnTmp.color = ColText;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("QuizTopicSelect.prefab 수정 완료.");
    }

    // ─── Loading ───────────────────────────────────────────────────────────
    private static void FixLoadingPrefab()
    {
        const string path = "Assets/UI/Prefabs/Quiz/QuizLoading.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        Image rootImg = root.GetComponent<Image>();
        if (rootImg != null) rootImg.color = ColCardBg;

        // LoadingText 색상
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts) t.color = ColText;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("QuizLoading.prefab 수정 완료.");
    }

    // ─── Gameplay ─────────────────────────────────────────────────────────
    private static void FixGameplayPrefab()
    {
        const string path = "Assets/UI/Prefabs/Quiz/QuizGameplay.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        Image rootImg = root.GetComponent<Image>();
        if (rootImg != null) rootImg.color = ColCardBg;

        // 루트 VerticalLayoutGroup
        VerticalLayoutGroup rootVlg = root.GetComponent<VerticalLayoutGroup>();
        if (rootVlg != null)
        {
            rootVlg.padding = new RectOffset(40, 40, 80, 40);
            rootVlg.spacing = 30;
            rootVlg.childForceExpandHeight = false;
            rootVlg.childForceExpandWidth = true;
            rootVlg.childControlHeight = false;
            rootVlg.childControlWidth = true;
        }

        // TopicBadge
        SetLayoutElement(root, "TopicBadge", preferredHeight: 70);
        SetTMPColor(root, "TopicBadge", ColTextSecondary);

        // QuestionCounter
        SetLayoutElement(root, "QuestionCounter", preferredHeight: 70);
        SetTMPColor(root, "QuestionCounter", ColTextSecondary);

        // QuestionText
        SetLayoutElement(root, "QuestionText", preferredHeight: 200, flexibleHeight: 1);
        SetTMPColor(root, "QuestionText", ColText);

        // ChoicesContainer
        Transform choicesContainer = root.transform.Find("ChoicesContainer");
        if (choicesContainer != null)
        {
            VerticalLayoutGroup vlg = choicesContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 20;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
            }
            LayoutElement cle = choicesContainer.GetComponent<LayoutElement>();
            if (cle == null) cle = choicesContainer.gameObject.AddComponent<LayoutElement>();
            cle.preferredHeight = 4 * 160 + 3 * 20; // 4버튼 + 3간격 = 700
        }

        // StatusBar
        SetLayoutElement(root, "StatusBar", preferredHeight: 80);
        Transform statusBar = root.transform.Find("StatusBar");
        if (statusBar != null)
        {
            foreach (TextMeshProUGUI t in statusBar.GetComponentsInChildren<TextMeshProUGUI>())
                t.color = ColText;
        }

        // ExplanationPanel 배경색
        Transform expPanel = root.transform.Find("ExplanationPanel");
        if (expPanel != null)
        {
            Image expImg = expPanel.GetComponent<Image>();
            if (expImg != null) expImg.color = new Color32(0xef, 0xf1, 0xff, 0xff);

            LayoutElement le = expPanel.GetComponent<LayoutElement>();
            if (le == null) le = expPanel.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 200;

            foreach (TextMeshProUGUI t in expPanel.GetComponentsInChildren<TextMeshProUGUI>())
                t.color = ColText;
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("QuizGameplay.prefab 수정 완료.");
    }

    // ─── Result ────────────────────────────────────────────────────────────
    private static void FixResultPrefab()
    {
        const string path = "Assets/UI/Prefabs/Quiz/QuizResult.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        Image rootImg = root.GetComponent<Image>();
        if (rootImg != null) rootImg.color = ColCardBg;

        VerticalLayoutGroup rootVlg = root.GetComponent<VerticalLayoutGroup>();
        if (rootVlg != null)
        {
            rootVlg.padding = new RectOffset(60, 60, 200, 100);
            rootVlg.spacing = 40;
            rootVlg.childForceExpandHeight = false;
            rootVlg.childForceExpandWidth = true;
            rootVlg.childControlHeight = false;
            rootVlg.childControlWidth = true;
        }

        SetLayoutElement(root, "ResultTitle",  preferredHeight: 140);
        SetTMPColor(root, "ResultTitle", ColText);

        SetLayoutElement(root, "ResultScore",  preferredHeight: 100);
        SetTMPColor(root, "ResultScore", ColText);

        SetLayoutElement(root, "CommentText",  preferredHeight: 160, flexibleHeight: 1);
        SetTMPColor(root, "CommentText", ColTextSecondary);

        Transform restartBtn = root.transform.Find("RestartButton");
        if (restartBtn != null)
        {
            Image btnImg = restartBtn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = ColPrimary;

            LayoutElement le = restartBtn.GetComponent<LayoutElement>();
            if (le == null) le = restartBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 160;

            TextMeshProUGUI btnText = restartBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.color = Color.white;
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("QuizResult.prefab 수정 완료.");
    }

    // ─── 헬퍼 ─────────────────────────────────────────────────────────────
    private static void SetLayoutElement(GameObject root, string childName,
        float preferredHeight = -1, float flexibleHeight = -1)
    {
        Transform t = root.transform.Find(childName);
        if (t == null) return;
        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
        if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
        if (flexibleHeight >= 0)  le.flexibleHeight  = flexibleHeight;
    }

    private static void SetTMPColor(GameObject root, string childName, Color color)
    {
        Transform t = root.transform.Find(childName);
        if (t == null) return;
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = color;
    }
}
