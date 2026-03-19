using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LeanGUIPrefabExtractor
{
    private const string ExamplesPath = "Assets/Plugins/LeanGUI/LeanGUI/Examples";
    private const string OutputPath = "Assets/UI/Prefabs/LeanGUI";

    [MenuItem("Tools/LeanGUI/모든 예제 프리팹 추출")]
    public static void ExtractAllPrefabs()
    {
        // 현재 씬 저장 확인
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 원래 씬 경로 기록
        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        // 출력 폴더 생성
        if (!AssetDatabase.IsValidFolder(OutputPath))
            AssetDatabase.CreateFolder("Assets/UI/Prefabs", "LeanGUI");

        // 예제 씬 파일 검색
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { ExamplesPath });
        int total = guids.Length;
        int prefabCount = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                EditorUtility.DisplayProgressBar(
                    "LeanGUI 프리팹 추출 중",
                    $"[{i + 1}/{total}] {sceneName}",
                    (float)i / total
                );

                // 씬 오픈
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // 씬 이름에서 토픽명 추출: "01 Button" → "Button"
                string topic = Regex.Replace(sceneName, @"^\d+\s+", "");

                // 루트 Canvas 검색
                Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                bool foundCanvas = false;

                foreach (Canvas canvas in canvases)
                {
                    // 루트 Canvas만 대상 (부모가 없는 것)
                    if (canvas.transform.parent != null)
                        continue;

                    foundCanvas = true;

                    foreach (Transform child in canvas.transform)
                    {
                        // 파일명에 사용할 수 없는 문자 제거
                        string safeName = string.Join("_", child.name.Split(Path.GetInvalidFileNameChars()));
                        string prefabFileName = $"{topic}_{safeName}.prefab";
                        string prefabPath = $"{OutputPath}/{prefabFileName}";

                        PrefabUtility.SaveAsPrefabAsset(child.gameObject, prefabPath);
                        prefabCount++;
                        Debug.Log($"[LeanGUI 추출] 생성: {prefabPath}");
                    }
                }

                if (!foundCanvas)
                    Debug.LogWarning($"[LeanGUI 추출] Canvas 없음, 스킵: {sceneName}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            // 원래 씬 복구
            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "LeanGUI 프리팹 추출 완료",
            $"총 {prefabCount}개 프리팹이 생성되었습니다.\n경로: {OutputPath}",
            "확인"
        );
    }
}
