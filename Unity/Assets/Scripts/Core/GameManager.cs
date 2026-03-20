using Cysharp.Threading.Tasks;
using UnityEngine;

public enum GameState
{
    Initializing,
    Ready,
    Playing,
}

/// <summary>
/// 앱 전체 생명주기 총괄 매니저.
/// - AppSettings를 통한 기기·그래픽 설정 적용
/// - CardSwipeController 초기화 순서 명시적 제어
/// - GameState로 앱 전반 상태 관리
/// 씬에는 GameManager.prefab 인스턴스를 배치한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private AppSettings         _appSettings;
    [SerializeField] private CardSwipeController _cardSwipeController;

    public GameState State { get; private set; } = GameState.Initializing;

    private void Awake()
    {
        Instance = this;

        if (_appSettings == null)
        {
            Debug.LogError("[GameManager] AppSettings가 없습니다.");
            return;
        }
        _appSettings.Apply();

#if DEVELOPMENT_BUILD
        DevToolsLoader.Initialize();
#endif
    }

    private async void Start()
    {
        if (_cardSwipeController == null)
        {
            Debug.LogError("[GameManager] CardSwipeController가 없습니다.");
            return;
        }

        await _cardSwipeController.InitializeAsync();
        State = GameState.Ready;
    }

    public void SetState(GameState state) => State = state;
}
