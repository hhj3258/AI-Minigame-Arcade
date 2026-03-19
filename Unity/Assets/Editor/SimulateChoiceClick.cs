// using Lean.Gui;
// using UnityEditor;
// using UnityEngine;

// public static class SimulateChoiceClick
// {
//     [MenuItem("Tools/Simulate Choice Click/Choice 0")]
//     public static void Click0() => ClickChoice(0);
//     [MenuItem("Tools/Simulate Choice Click/Choice 1")]
//     public static void Click1() => ClickChoice(1);
//     [MenuItem("Tools/Simulate Choice Click/Choice 2")]
//     public static void Click2() => ClickChoice(2);
//     [MenuItem("Tools/Simulate Choice Click/Choice 3")]
//     public static void Click3() => ClickChoice(3);

//     private static void ClickChoice(int index)
//     {
//         string name = $"ChoiceButton_{index}";
//         GameObject btn = GameObject.Find(name);
//         if (btn == null)
//         {
//             Debug.LogError($"{name}을 찾을 수 없습니다. Play mode에서 실행하세요.");
//             return;
//         }
//         LeanButton lean = btn.GetComponent<LeanButton>();
//         if (lean == null)
//         {
//             Debug.LogError($"LeanButton 없음: {name}");
//             return;
//         }
//         lean.OnClick.Invoke();
//         Debug.Log($"{name} 클릭 시뮬레이션 완료");
//     }
// }
