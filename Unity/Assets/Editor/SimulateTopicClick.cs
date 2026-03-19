// using Lean.Gui;
// using UnityEditor;
// using UnityEngine;

// public static class SimulateTopicClick
// {
//     [MenuItem("Tools/Simulate Topic Click (역사)")]
//     public static void Click()
//     {
//         // TopicButton_0의 LeanButton.OnClick 호출
//         GameObject btn = GameObject.Find("TopicButton_0");
//         if (btn == null) { Debug.LogError("TopicButton_0를 찾을 수 없습니다. Play mode에서 실행하세요."); return; }
//         LeanButton lean = btn.GetComponent<LeanButton>();
//         if (lean == null) { Debug.LogError("LeanButton이 없습니다."); return; }
//         lean.OnClick.Invoke();
//         Debug.Log("TopicButton_0 클릭 시뮬레이션 완료");
//     }
// }
