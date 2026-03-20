using UnityEngine;

/// <summary>
/// 기기별·그래픽 설정 전담 ScriptableObject.
/// GameManager.Awake()에서 Apply()를 호출해 앱 시작 시 적용된다.
/// </summary>
[CreateAssetMenu(fileName = "AppSettings", menuName = "Settings/AppSettings")]
public class AppSettings : ScriptableObject
{
    [Header("프레임")]
    [SerializeField] private int _targetFrameRate = 60;

    [Header("화면")]
    [SerializeField] private bool _neverSleep = true;
    [SerializeField] private bool _multiTouch = true;

    public void Apply()
    {
        Application.targetFrameRate = _targetFrameRate;
        Screen.sleepTimeout = _neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        Input.multiTouchEnabled = _multiTouch;
    }
}
