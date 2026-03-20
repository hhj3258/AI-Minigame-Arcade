using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android APK 빌드 자동화 스크립트.
/// Tools/Build Android APK 메뉴로 실행.
/// </summary>
public static class AndroidBuildScript
{
    private const string BuildProfilePath = "Assets/Settings/Build Profiles/Android™.asset";
    private const string OutputDir        = "Builds";
    private const string ApkName          = "AIMiniArcade.apk";

    [MenuItem("Tools/Build Android APK")]
    public static void BuildAndroid()
    {
        // Build Profile 활성화
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(BuildProfilePath);
        if (profile == null)
        {
            Debug.LogError($"[AndroidBuild] Build Profile을 찾을 수 없음: {BuildProfilePath}");
            return;
        }
        BuildProfile.SetActiveBuildProfile(profile);

        // 출력 경로 설정
        Directory.CreateDirectory(OutputDir);
        string outputPath = Path.Combine(OutputDir, ApkName);

        // 씬 목록
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[AndroidBuild] Build Settings에 활성화된 씬이 없습니다.");
            return;
        }

        Debug.Log($"[AndroidBuild] 빌드 시작 → {outputPath}");
        Debug.Log($"[AndroidBuild] 씬: {string.Join(", ", scenes)}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes             = scenes,
            locationPathName   = outputPath,
            target             = BuildTarget.Android,
            options            = BuildOptions.Development,
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AndroidBuild] ✓ 빌드 성공! 파일: {Path.GetFullPath(outputPath)} ({summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"[AndroidBuild] ✗ 빌드 실패 — 오류 {summary.totalErrors}개, 경고 {summary.totalWarnings}개");
        }
    }
}
