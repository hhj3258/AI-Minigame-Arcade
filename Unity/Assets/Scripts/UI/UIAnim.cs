using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UIToolkit 공통 애니메이션 유틸.
/// 버튼 프레스 애니메이션 등 여러 게임에서 재사용할 수 있는 UI 인터랙션 패턴을 제공한다.
/// </summary>
public static class UIAnim
{
    // ── 프레스 애니메이션 스타일 캐시 ────────────────────────
    private static readonly StyleScale PressedScale =
        new(new Scale(new Vector3(0.93f, 0.93f, 1f)));
    private static readonly StyleScale OvershootScale =
        new(new Scale(new Vector3(1.12f, 1.12f, 1f)));
    private static readonly StyleList<TimeValue> ZeroTransition =
        new(new List<TimeValue> { new(0) });

    /// <summary>
    /// VisualElement에 버튼 프레스 애니메이션을 등록한다.
    /// PointerDown : 즉시 0.93 축소 (CSS ease-out 트랜지션)
    /// PointerUp   : 즉시 1.12 오버슈트 → 1프레임 후 CSS ease-out으로 1.0 복귀 (스프링 효과)
    /// PointerLeave: 눌린 상태일 때만 동일 처리 (onReleaseDone 미실행)
    /// </summary>
    /// <param name="el">애니메이션을 적용할 VisualElement</param>
    /// <param name="onReleaseDone">스케일 복귀 트랜지션 완료 후 실행할 콜백 (nullable)</param>
    public static void RegisterPressAnim(VisualElement el, Action onReleaseDone = null)
    {
        bool fireOnEnd = false;
        bool isPressed = false;

        el.RegisterCallback<PointerDownEvent>(_ =>
        {
            isPressed = true;
            fireOnEnd = false;
            el.style.scale = PressedScale;
        }, TrickleDown.TrickleDown);

        el.RegisterCallback<PointerUpEvent>(_ =>
        {
            if (!isPressed) return;
            isPressed = false;
            fireOnEnd = onReleaseDone != null;
            el.style.transitionDuration = ZeroTransition;
            el.style.scale = OvershootScale;
            el.schedule.Execute(() =>
            {
                el.style.transitionDuration = StyleKeyword.Null;
                el.style.scale = new StyleScale(StyleKeyword.Null);
            }).StartingIn(1);
        });

        el.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            if (!isPressed) return;
            isPressed = false;
            fireOnEnd = false;
            el.style.transitionDuration = ZeroTransition;
            el.style.scale = OvershootScale;
            el.schedule.Execute(() =>
            {
                el.style.transitionDuration = StyleKeyword.Null;
                el.style.scale = new StyleScale(StyleKeyword.Null);
            }).StartingIn(1);
        });

        if (onReleaseDone != null)
        {
            el.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (!fireOnEnd) return;
                foreach (var p in evt.stylePropertyNames)
                {
                    if (p.ToString() != "scale") continue;
                    fireOnEnd = false;
                    onReleaseDone();
                    break;
                }
            });
        }
    }
}
