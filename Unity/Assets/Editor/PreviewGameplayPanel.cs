using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임플레이 패널 미리보기: TopicSelect OFF, Gameplay ON 으로 전환하여 레이아웃 확인
/// </summary>
public static class PreviewGameplayPanel
{
    [MenuItem("Tools/패널 미리보기/게임플레이")]
    public static void ShowGameplay()
    {
        SetPanel("QuizTopicSelect", false);
        SetPanel("QuizLoading",     false);
        SetPanel("QuizGameplay",    true);
        SetPanel("QuizResult",      false);

        // 더미 텍스트 채우기
        SetTMP("QuizGameplay/TopicBadge",      "과학");
        SetTMP("QuizGameplay/QuestionCounter", "1 / 5");
        SetTMP("QuizGameplay/QuestionText",    "물의 화학식은 무엇일까요?");
        SetTMP("QuizGameplay/StatusBar/TimerLabel", "20.0s");
        SetTMP("QuizGameplay/StatusBar/ScoreLabel", "0 / 5");

        // 선택지 더미 텍스트
        string[] choices = { "CO2", "H2O", "O2", "NaCl" };
        for (int i = 0; i < 4; i++)
        {
            GameObject btn = FindGO($"QuizGameplay/ChoicesContainer/ChoiceButton_{i}");
            if (btn != null)
            {
                Transform choiceText = btn.transform.Find("ChoiceText");
                if (choiceText != null)
                {
                    TextMeshProUGUI tmp = choiceText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = choices[i];
                }
                Transform indexLabel = btn.transform.Find("IndexLabel");
                if (indexLabel != null)
                {
                    TextMeshProUGUI tmp = indexLabel.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = new[] { "①", "②", "③", "④" }[i];
                }
            }
        }

        Debug.Log("게임플레이 패널 미리보기 활성화 완료.");
    }

    [MenuItem("Tools/패널 미리보기/주제 선택")]
    public static void ShowTopicSelect()
    {
        SetPanel("QuizTopicSelect", true);
        SetPanel("QuizLoading",     false);
        SetPanel("QuizGameplay",    false);
        SetPanel("QuizResult",      false);
        Debug.Log("주제 선택 패널 미리보기 활성화 완료.");
    }

    private static void SetPanel(string name, bool visible)
    {
        GameObject go = FindGO(name);
        if (go == null) return;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
    }

    private static void SetTMP(string path, string text)
    {
        GameObject go = FindGO(path);
        if (go == null) return;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    private static GameObject FindGO(string name)
    {
        // 마지막 파트 이름으로 씬 전체 검색
        string lastName = name.Contains("/") ? name.Substring(name.LastIndexOf('/') + 1) : name;
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go.name == lastName && go.scene.IsValid())
                return go;
        }
        Debug.LogWarning($"FindGO: '{name}'를 찾지 못했습니다.");
        return null;
    }
}
