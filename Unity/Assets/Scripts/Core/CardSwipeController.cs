using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// UIDocument 기반 카드 스와이프 컨트롤러.
/// 각 카드의 rootVisualElement를 translate로 애니메이션합니다.
/// SurvivorGame 등 카메라가 있는 카드는 Camera 활성화/비활성화로 처리합니다.
/// </summary>
public class CardSwipeController : MonoBehaviour
{
    [Serializable]
    public class GameCard
    {
        public UIDocument      uiDocument;
        public MonoBehaviour   controller;  // IMinigame 구현체
        public Camera          gameCamera;  // nullable
        public SurvivorGame    survivorGame; // nullable (카드 진입/이탈 추가 처리용)
    }

    [SerializeField] private List<GameCard> _cards;
    [SerializeField] [Range(0.1f, 0.5f)] private float _swipeThresholdRatio = 0.3f;
    [SerializeField] private float _transitionDuration = 0.3f;

    // ── 카드 인디케이터 VisualElement 이름 ────────────────
    // 현재는 별도 UIDocument 없이 카드 내부에 없음 — 추후 확장 가능

    private int  _currentIndex;
    private bool _isTransitioning;

    private Vector2 _touchStart;
    private float   _touchStartTime;
    private bool    _tracking;

    // Start()는 GameManager.Start()에서 InitializeAsync()를 명시적으로 호출하므로 제거.

    public async UniTask InitializeAsync()
    {
        if (_cards == null || _cards.Count == 0) return;

        // 카드 초기 배치: 0번 ON, 나머지 아래(100%)
        for (int i = 0; i < _cards.Count; i++)
        {
            var root = GetCardRoot(i);
            if (root == null) continue;

            SetTranslateY(root, i == 0 ? 0f : 100f);

            if (_cards[i].uiDocument != null)
                _cards[i].uiDocument.enabled = true;

            if (_cards[i].gameCamera != null)
                _cards[i].gameCamera.enabled = (i == 0);
        }

        // 카드 0번 자산 로드 후 시작
        var minigame0 = _cards[0].controller as IMinigame;
        await (minigame0?.InitializeAsync() ?? UniTask.CompletedTask);
        minigame0?.OnGameStart();
        _cards[0].survivorGame?.ActivateCard();
    }

    private void Update()
    {
        if (_isTransitioning) return;
        TrackInput();
    }

    // ── 입력 감지 ─────────────────────────────────────────
    private void TrackInput()
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                _touchStart     = touch.position.ReadValue();
                _touchStartTime = Time.time;
                _tracking       = true;
            }
            else if (touch.press.wasReleasedThisFrame && _tracking)
            {
                _tracking = false;
                HandleSwipeDelta(touch.position.ReadValue().y - _touchStart.y);
            }

            // 터치가 활성 중이면 마우스 블록 진입 차단
            if (touch.press.isPressed || touch.press.wasReleasedThisFrame)
                return;
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _touchStart     = Mouse.current.position.ReadValue();
                _touchStartTime = Time.time;
                _tracking       = true;
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && _tracking)
            {
                _tracking = false;
                HandleSwipeDelta(Mouse.current.position.ReadValue().y - _touchStart.y);
            }
        }
    }

    private void HandleSwipeDelta(float deltaY)
    {
        float threshold = Screen.height * _swipeThresholdRatio;
        float elapsed   = Time.time - _touchStartTime;
        bool  isFast    = elapsed < 0.25f && Mathf.Abs(deltaY) > 80f;

        if (deltaY > threshold || (isFast && deltaY > 0))
            SwipeToCard(_currentIndex + 1);
        else if (deltaY < -threshold || (isFast && deltaY < 0))
            SwipeToCard(_currentIndex - 1);
    }

    public void SwipeToCard(int targetIndex)
    {
        if (_isTransitioning) return;
        if (targetIndex < 0 || targetIndex >= _cards.Count) return;
        if (targetIndex == _currentIndex) return;

        TransitionAsync(_currentIndex, targetIndex).Forget();
    }

    // ── 카드 전환 ─────────────────────────────────────────
    private async UniTaskVoid TransitionAsync(int fromIndex, int toIndex)
    {
        _isTransitioning = true;

        bool goingDown = toIndex > fromIndex; // 위로 스와이프 → 다음 카드 (아래서 올라옴)

        var fromRoot = GetCardRoot(fromIndex);
        var toRoot   = GetCardRoot(toIndex);

        if (fromRoot == null || toRoot == null)
        {
            _isTransitioning = false;
            return;
        }

        // 나가는 카드 비활성화
        if (_cards[fromIndex].survivorGame != null)
            _cards[fromIndex].survivorGame.DeactivateCard();

        // 들어오는 카드 자산 로드 (최초 진입 시에만 실제 로드, 이후 즉시 반환)
        var toMinigame = _cards[toIndex].controller as IMinigame;
        await (toMinigame?.InitializeAsync() ?? UniTask.CompletedTask);

        // 들어오는 카드 초기 위치 설정 (화면 밖)
        SetTranslateY(toRoot, goingDown ? 100f : -100f);

        // CSS transition (SurvivorGame.uss에 정의된 transition 활용)
        // USS transition이 없는 경우 UniTask 루프로 직접 lerp
        float elapsed = 0f;

        float fromStart = 0f;
        float fromEnd   = goingDown ? -100f : 100f;
        float toStart   = goingDown ?  100f : -100f;
        float toEnd     = 0f;

        while (elapsed < _transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / _transitionDuration);

            SetTranslateY(fromRoot, Mathf.Lerp(fromStart, fromEnd, t));
            SetTranslateY(toRoot,   Mathf.Lerp(toStart,  toEnd,   t));

            await UniTask.Yield();
        }

        SetTranslateY(fromRoot, fromEnd);
        SetTranslateY(toRoot,   0f);

        _currentIndex = toIndex;

        // 카메라 전환
        if (_cards[fromIndex].gameCamera != null)
            _cards[fromIndex].gameCamera.enabled = false;
        if (_cards[toIndex].gameCamera != null)
            _cards[toIndex].gameCamera.enabled = true;

        // 들어오는 카드 OnGameStart
        toMinigame?.OnGameStart();

        _cards[toIndex].survivorGame?.ActivateCard();

        _isTransitioning = false;
    }

    // ── 유틸리티 ──────────────────────────────────────────
    private VisualElement GetCardRoot(int index)
    {
        if (index < 0 || index >= _cards.Count) return null;
        return _cards[index].uiDocument?.rootVisualElement;
    }

    private static void SetTranslateY(VisualElement element, float percent)
    {
        element.style.translate = new StyleTranslate(
            new Translate(Length.Percent(0), Length.Percent(percent)));
    }
}
