#if DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Development Build 전용 개발 도구 로더.
/// MonoBehaviour 불필요 — GameManager.Awake()에서 Initialize()를 직접 호출한다.
/// </summary>
public static class DevToolsLoader
{
    private const string ConsoleAddress = "IngameDebugConsole";

    public static void Initialize()
    {
        Addressables.LoadAssetAsync<GameObject>(ConsoleAddress).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[DevToolsLoader] {ConsoleAddress} 로드 실패: {handle.OperationException?.Message}");
                return;
            }
            Object.Instantiate(handle.Result);
            Debug.Log("[DevToolsLoader] IngameDebugConsole 생성 완료.");
        };
    }
}
#endif
