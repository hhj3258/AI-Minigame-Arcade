// using System.Linq;
// using Lean.Gui;
// using TMPro;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// 퀴즈 게임 UI 프리팹을 Assets/TestUI 아래에 생성하는 에디터 유틸리티.
// /// Tools / Build Quiz Prefabs 메뉴에서 실행한다.
// /// </summary>
// public static class QuizPrefabBuilder
// {
//     private const string OutDir   = "Assets/TestUI";
//     private const string FontPath = "Assets/Fonts/MalgunGothic SDF.asset";

//     // ── 디자인 토큰 ─────────────────────────────────────────
//     private static readonly Color CPrimary  = H(0x5b, 0x6e, 0xf5);
//     private static readonly Color CText     = H(0x1e, 0x1e, 0x2e);
//     private static readonly Color CTextSec  = H(0x7c, 0x7f, 0x9e);
//     private static readonly Color CBg       = H(0xf0, 0xf4, 0xff);
//     private static readonly Color CBtnBg    = H(0xef, 0xf1, 0xff);
//     private static readonly Color CWhite    = Color.white;

//     private static Color H(byte r, byte g, byte b) => new Color32(r, g, b, 255);

//     private static TMP_FontAsset _font;

//     // ── 진입점 ─────────────────────────────────────────────
//     [MenuItem("Tools/Build Quiz Prefabs")]
//     public static void Build()
//     {
//         if (!AssetDatabase.IsValidFolder(OutDir))
//             AssetDatabase.CreateFolder("Assets", "TestUI");

//         _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
//         if (_font == null)
//             Debug.LogWarning($"[QPB] 폰트 로드 실패: {FontPath}");

//         // Atom 먼저 생성 → QuizGame에서 Nested Prefab으로 사용
//         var choiceBtnAsset  = BuildChoiceButton();
//         var loadingDotAsset = BuildLoadingDot();
//         BuildQuizGame(choiceBtnAsset, loadingDotAsset);

//         AssetDatabase.SaveAssets();
//         AssetDatabase.Refresh();
//         Debug.Log($"[QPB] ✅ 완료 → {OutDir}");
//     }

//     // ══════════════════════ ATOMS ══════════════════════════

//     private static GameObject BuildChoiceButton()
//     {
//         var root = GO("ChoiceButton");
//         SetSize(root, 960, 110);

//         var lb = root.AddComponent<LeanButton>();
//         var cb = root.AddComponent<ChoiceButton>();

//         // Background (풀스트레치)
//         var bgGO  = Child("Background", root);
//         SetStretch(bgGO);
//         var bgImg = bgGO.AddComponent<Image>();
//         bgImg.color = CBtnBg;

//         // IndexLabel (좌 18%)
//         var idxGO  = Child("IndexLabel", root);
//         SetAnchors(idxGO, new Vector2(0, 0), new Vector2(0.18f, 1));
//         var idxTmp = Tmp(idxGO, "①", 48, CText);

//         // ChoiceText (우 82%)
//         var txtGO  = Child("ChoiceText", root);
//         SetAnchors(txtGO, new Vector2(0.18f, 0), Vector2.one, oMin: new Vector2(8, 0));
//         var txtTmp = Tmp(txtGO, "선택지 텍스트", 42, CText, align: TextAlignmentOptions.MidlineLeft);

//         // 직렬화 필드 연결
//         Prop(cb, "_leanButton", lb);
//         Prop(cb, "_background", bgImg);
//         Prop(cb, "_indexLabel", idxTmp);
//         Prop(cb, "_choiceText", txtTmp);

//         return Save(root, "ChoiceButton");
//     }

//     private static GameObject BuildLoadingDot()
//     {
//         var root = GO("LoadingDot");
//         SetSize(root, 24, 24);

//         root.AddComponent<Image>().color = CPrimary;

//         var pulse = root.AddComponent<LeanPulse>();
//         Prop(pulse, "remainingPulses", -1);
//         Prop(pulse, "timeInterval",    0.4f);

//         return Save(root, "LoadingDot");
//     }

//     // ════════════════════ QUIZ GAME ROOT ════════════════════

//     private static void BuildQuizGame(GameObject choiceBtnAsset, GameObject loadingDotAsset)
//     {
//         var root = GO("QuizGame");
//         SetStretch(root);
//         root.AddComponent<Image>().color = CBg;
//         var qg = root.AddComponent<QuizGame>();

//         // ── Panel 1: TopicSelect (시작 시 표시) ───────────────
//         var (topicPanel, topicToggle) = MakePanel("QuizTopicSelect", root, startOn: true);

//         var titleGO = Child("TitleText", topicPanel);
//         SetAnchors(titleGO, new Vector2(0.05f, 0.67f), new Vector2(0.95f, 0.88f));
//         Tmp(titleGO, "주제를 선택하세요", 72, CText, FontStyles.Bold);

//         var btnCont = Child("TopicButtonsContainer", topicPanel);
//         SetAnchors(btnCont, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.65f));
//         var vlg = btnCont.AddComponent<VerticalLayoutGroup>();
//         vlg.spacing = 16;
//         vlg.childControlHeight    = false; vlg.childControlWidth    = true;
//         vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;

//         string[] topics = { "세계 역사", "과학 상식", "영화 / 드라마", "스포츠", "K-POP" };
//         var topicLBs = new LeanButton[topics.Length];
//         for (int i = 0; i < topics.Length; i++)
//         {
//             var btnGO = Child($"TopicButton_{i}", btnCont);
//             btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 108);
//             btnGO.AddComponent<Image>().color = CBtnBg;
//             topicLBs[i] = btnGO.AddComponent<LeanButton>();

//             var lbl = Child("Label", btnGO);
//             SetStretch(lbl);
//             Tmp(lbl, topics[i], 52, CText);
//         }

//         // ── Panel 2: Loading ────────────────────────────────
//         var (loadingPanel, loadingToggle) = MakePanel("QuizLoading", root, startOn: false);

//         var loadTextGO = Child("LoadingText", loadingPanel);
//         SetAnchors(loadTextGO, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.64f));
//         Tmp(loadTextGO, "문제를 생성하고 있습니다", 52, CTextSec);

//         var dotsCont = Child("DotsContainer", loadingPanel);
//         SetAnchors(dotsCont, new Vector2(0.35f, 0.38f), new Vector2(0.65f, 0.50f));
//         var hlg = dotsCont.AddComponent<HorizontalLayoutGroup>();
//         hlg.spacing = 16;
//         hlg.childAlignment       = TextAnchor.MiddleCenter;
//         hlg.childControlHeight   = false; hlg.childControlWidth   = false;
//         hlg.childForceExpandHeight = false; hlg.childForceExpandWidth = false;

//         float[] dotDelays  = { 0f, 0.13f, 0.27f };
//         var     dotPulses  = new LeanPulse[3];
//         for (int i = 0; i < 3; i++)
//         {
//             var dotGO = loadingDotAsset != null
//                 ? (GameObject)PrefabUtility.InstantiatePrefab(loadingDotAsset, dotsCont.transform)
//                 : GO($"Dot_{i}", dotsCont);
//             dotGO.name = $"Dot_{i}";

//             var p = dotGO.GetComponent<LeanPulse>();
//             if (p != null)
//             {
//                 // 양수 remainingTime = 해당 시간 후 첫 펄스 → 딜레이 오프셋 구현
//                 Prop(p, "remainingTime", dotDelays[i]);
//                 dotPulses[i] = p;
//             }
//         }

//         // ── Panel 3: Gameplay ────────────────────────────────
//         var (gameplayPanel, gameplayToggle) = MakePanel("QuizGameplay", root, startOn: false);

//         var badgeGO = Child("TopicBadge", gameplayPanel);
//         SetAnchors(badgeGO, new Vector2(0.05f, 0.88f), new Vector2(0.55f, 0.95f));
//         var badgeTmp = Tmp(badgeGO, "주제", 44, CTextSec, align: TextAlignmentOptions.MidlineLeft);

//         var counterGO = Child("QuestionCounter", gameplayPanel);
//         SetAnchors(counterGO, new Vector2(0.55f, 0.88f), new Vector2(0.95f, 0.95f));
//         var counterTmp = Tmp(counterGO, "1 / 5", 44, CTextSec, FontStyles.Bold, TextAlignmentOptions.MidlineRight);

//         var questionGO = Child("QuestionText", gameplayPanel);
//         SetAnchors(questionGO, new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.87f));
//         var questionTmp = Tmp(questionGO, "질문 텍스트가 여기에 표시됩니다.", 52, CText);

//         // 선택지 컨테이너
//         var choicesCont = Child("ChoicesContainer", gameplayPanel);
//         SetAnchors(choicesCont, new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.63f));
//         var cvlg = choicesCont.AddComponent<VerticalLayoutGroup>();
//         cvlg.spacing = 12;
//         cvlg.childControlHeight    = false; cvlg.childControlWidth    = true;
//         cvlg.childForceExpandHeight = false; cvlg.childForceExpandWidth = true;

//         var choiceBtns = new ChoiceButton[4];
//         for (int i = 0; i < 4; i++)
//         {
//             var cbGO = choiceBtnAsset != null
//                 ? (GameObject)PrefabUtility.InstantiatePrefab(choiceBtnAsset, choicesCont.transform)
//                 : GO($"Choice_{i}", choicesCont);
//             cbGO.name = $"Choice_{i}";
//             cbGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 110);
//             choiceBtns[i] = cbGO.GetComponent<ChoiceButton>();
//         }

//         // 상태바 (타이머 + 점수)
//         var statusBar = Child("StatusBar", gameplayPanel);
//         SetAnchors(statusBar, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.28f));
//         var shlg = statusBar.AddComponent<HorizontalLayoutGroup>();
//         shlg.childControlWidth    = true; shlg.childForceExpandWidth = true;
//         shlg.childAlignment       = TextAnchor.MiddleCenter;

//         var timerGO  = Child("TimerLabel", statusBar);
//         var timerTmp = Tmp(timerGO, "30.0s", 52, CText, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

//         var scoreGO  = Child("ScoreLabel", statusBar);
//         var scoreTmp = Tmp(scoreGO, "0 / 5", 52, CText, FontStyles.Bold, TextAlignmentOptions.MidlineRight);

//         // 해설 패널 (Gameplay 안쪽 하단)
//         var explPanel = Child("ExplanationPanel", gameplayPanel);
//         SetAnchors(explPanel, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.19f));
//         explPanel.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);
//         var (_, explToggle) = AddPanelToggle(explPanel, startOn: false);

//         var explTextGO = Child("ExplanationText", explPanel);
//         SetStretch(explTextGO, 16, 16, 8, 8);
//         var explTmp = Tmp(explTextGO, "해설 텍스트", 40, CTextSec);

//         // ── Panel 4: Result ──────────────────────────────────
//         var (resultPanel, resultToggle) = MakePanel("QuizResult", root, startOn: false);

//         var resultTitleGO = Child("ResultTitle", resultPanel);
//         SetAnchors(resultTitleGO, new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.78f));
//         var resultTitleTmp = Tmp(resultTitleGO, "🎉 클리어!", 80, CText, FontStyles.Bold);

//         var resultScoreGO = Child("ResultScore", resultPanel);
//         SetAnchors(resultScoreGO, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.60f));
//         var resultScoreTmp = Tmp(resultScoreGO, "3 / 5 정답", 64, CPrimary, FontStyles.Bold);

//         var commentGO = Child("CommentText", resultPanel);
//         SetAnchors(commentGO, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.47f));
//         var commentTmp = Tmp(commentGO, "AI 총평이 여기에 표시됩니다.", 44, CTextSec);

//         var restartGO = Child("RestartButton", resultPanel);
//         SetAnchors(restartGO, new Vector2(0.2f, 0.10f), new Vector2(0.8f, 0.23f));
//         restartGO.AddComponent<Image>().color = CPrimary;
//         var restartLB  = restartGO.AddComponent<LeanButton>();
//         var rlblGO     = Child("ButtonText", restartGO);
//         SetStretch(rlblGO);
//         Tmp(rlblGO, "다시 하기", 52, CWhite, FontStyles.Bold);

//         // ── QuizGame 직렬화 필드 일괄 연결 ──────────────────
//         Prop(qg, "_topicSelectToggle",  topicToggle);
//         Prop(qg, "_loadingToggle",      loadingToggle);
//         Prop(qg, "_gameplayToggle",     gameplayToggle);
//         Prop(qg, "_resultToggle",       resultToggle);
//         Prop(qg, "_explanationToggle",  explToggle);
//         Prop(qg, "_topicBadge",         badgeTmp);
//         Prop(qg, "_questionCounter",    counterTmp);
//         Prop(qg, "_questionText",       questionTmp);
//         Prop(qg, "_timerLabel",         timerTmp);
//         Prop(qg, "_scoreLabel",         scoreTmp);
//         Prop(qg, "_explanationText",    explTmp);
//         Prop(qg, "_resultTitle",        resultTitleTmp);
//         Prop(qg, "_resultScore",        resultScoreTmp);
//         Prop(qg, "_commentText",        commentTmp);
//         Prop(qg, "_restartButton",      restartLB);

//         PropArray(qg, "_topicButtons",  topicLBs.Cast<Object>().ToArray());
//         PropArray(qg, "_choiceButtons", choiceBtns.Cast<Object>().ToArray());
//         PropArray(qg, "_loadingDots",   dotPulses.Cast<Object>().ToArray());

//         Save(root, "QuizGame");
//     }

//     // ════════════════════ HELPERS ════════════════════════

//     private static GameObject GO(string name, GameObject parent = null)
//     {
//         var go = new GameObject(name);
//         if (parent != null) go.transform.SetParent(parent.transform, false);
//         var rt = go.AddComponent<RectTransform>();
//         rt.localScale = Vector3.one;
//         return go;
//     }

//     private static GameObject Child(string name, GameObject parent)
//     {
//         return GO(name, parent);
//     }

//     private static void SetSize(GameObject go, float w, float h)
//     {
//         var rt = go.GetComponent<RectTransform>();
//         rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
//         rt.sizeDelta = new Vector2(w, h);
//     }

//     private static void SetStretch(GameObject go,
//         float padL = 0, float padR = 0, float padB = 0, float padT = 0)
//     {
//         var rt = go.GetComponent<RectTransform>();
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.offsetMin = new Vector2(padL, padB);
//         rt.offsetMax = new Vector2(-padR, -padT);
//     }

//     private static void SetAnchors(GameObject go,
//         Vector2 aMin, Vector2 aMax,
//         Vector2 oMin = default, Vector2 oMax = default)
//     {
//         var rt = go.GetComponent<RectTransform>();
//         rt.anchorMin = aMin;
//         rt.anchorMax = aMax;
//         rt.offsetMin = oMin;
//         rt.offsetMax = oMax;
//     }

//     /// <summary>패널 배경 + CanvasGroup + LeanToggle 한 번에 추가</summary>
//     private static (GameObject panel, LeanToggle toggle) MakePanel(
//         string name, GameObject parent, bool startOn)
//     {
//         var go = Child(name, parent);
//         SetStretch(go);
//         go.AddComponent<Image>().color = CBg;
//         var (_, lt) = AddPanelToggle(go, startOn);
//         return (go, lt);
//     }

//     /// <summary>CanvasGroup + LeanToggle 추가 (Image 없음 버전)</summary>
//     private static (CanvasGroup cg, LeanToggle lt) AddPanelToggle(GameObject go, bool startOn)
//     {
//         var cg = go.AddComponent<CanvasGroup>();
//         cg.alpha = startOn ? 1f : 0f;
//         cg.interactable    = startOn;
//         cg.blocksRaycasts  = startOn;

//         var lt = go.AddComponent<LeanToggle>();
//         lt.On = startOn;
//         return (cg, lt);
//     }

//     private static TextMeshProUGUI Tmp(GameObject go, string text, float size, Color color,
//         FontStyles style = FontStyles.Normal,
//         TextAlignmentOptions align = TextAlignmentOptions.Center)
//     {
//         var t = go.AddComponent<TextMeshProUGUI>();
//         t.text              = text;
//         t.fontSize          = size;
//         t.color             = color;
//         t.fontStyle         = style;
//         t.alignment         = align;
//         t.enableWordWrapping = true;
//         if (_font != null) t.font = _font;
//         return t;
//     }

//     // ── SerializedObject 헬퍼 ──────────────────────────────

//     private static void Prop(Object comp, string field, Object value)
//     {
//         var so = new SerializedObject(comp);
//         var p  = so.FindProperty(field);
//         if (p == null) { Debug.LogWarning($"[QPB] 필드 없음: {field}"); return; }
//         p.objectReferenceValue = value;
//         so.ApplyModifiedProperties();
//     }

//     private static void Prop(Object comp, string field, int value)
//     {
//         var so = new SerializedObject(comp);
//         var p  = so.FindProperty(field);
//         if (p == null) { Debug.LogWarning($"[QPB] 필드 없음: {field}"); return; }
//         p.intValue = value;
//         so.ApplyModifiedProperties();
//     }

//     private static void Prop(Object comp, string field, float value)
//     {
//         var so = new SerializedObject(comp);
//         var p  = so.FindProperty(field);
//         if (p == null) { Debug.LogWarning($"[QPB] 필드 없음: {field}"); return; }
//         p.floatValue = value;
//         so.ApplyModifiedProperties();
//     }

//     private static void PropArray(Object comp, string field, Object[] values)
//     {
//         var so = new SerializedObject(comp);
//         var p  = so.FindProperty(field);
//         if (p == null) { Debug.LogWarning($"[QPB] 배열 필드 없음: {field}"); return; }
//         p.arraySize = values.Length;
//         for (int i = 0; i < values.Length; i++)
//             p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
//         so.ApplyModifiedProperties();
//     }

//     private static GameObject Save(GameObject root, string prefabName)
//     {
//         string path   = $"{OutDir}/{prefabName}.prefab";
//         var    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
//         Object.DestroyImmediate(root);
//         Debug.Log($"[QPB] 저장: {path}");
//         return prefab;
//     }
// }
