using UnityEditor;

/// <summary>
/// 자주 쓰는 Unity 창 바로가기 모음.
/// </summary>
public static class WindowShortcuts
{
    [MenuItem("Tools/창 바로가기/어드레서블 그룹")]
    public static void OpenAddressables()
        => EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");

    [MenuItem("Tools/창 바로가기/MCP 서버 창")]
    public static void OpenMcp()
        => EditorApplication.ExecuteMenuItem("Window/MCP For Unity/Toggle MCP Window");

    [MenuItem("Tools/창 바로가기/디바이스 시뮬레이터")]
    public static void OpenDeviceSimulator()
        => EditorApplication.ExecuteMenuItem("Window/General/Device Simulator");
}
