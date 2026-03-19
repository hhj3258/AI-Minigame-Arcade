using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UIToolkit PointerEvent 기반 가상 조이스틱.
/// joystick-area VisualElement에 이벤트 등록 후 Direction 값을 제공합니다.
/// </summary>
public class VirtualJoystick
{
    public Vector2 Direction { get; private set; }

    private readonly VisualElement _area;
    private readonly VisualElement _handle;

    private const float MaxRadius = 58f; // USS joystick-area 200px / 2 - handle/2 ≈ 58px

    private bool    _isDragging;
    private Vector2 _centerPos;

    public VirtualJoystick(VisualElement area, VisualElement handle)
    {
        if (area == null) throw new ArgumentNullException(nameof(area));
        if (handle == null) throw new ArgumentNullException(nameof(handle));

        _area   = area;
        _handle = handle;

        _area.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _area.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _area.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _area.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
    }

    public void Unregister()
    {
        _area.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        _area.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        _area.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        _area.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
    }

    private void OnPointerDown(PointerDownEvent e)
    {
        _isDragging = true;
        _area.CapturePointer(e.pointerId);

        // 조이스틱 영역 중심 계산
        Rect worldBound = _area.worldBound;
        _centerPos = new Vector2(worldBound.center.x, worldBound.center.y);

        UpdateHandle(e.position);
        e.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (!_isDragging) return;
        UpdateHandle(e.position);
        e.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        if (!_isDragging) return;
        Reset();
        _area.ReleasePointer(e.pointerId);
        e.StopPropagation();
    }

    private void OnPointerCancel(PointerCancelEvent e)
    {
        if (_isDragging) Reset();
    }

    private void UpdateHandle(Vector3 pointerPos)
    {
        Vector2 delta = new Vector2(pointerPos.x - _centerPos.x, pointerPos.y - _centerPos.y);
        float   dist  = delta.magnitude;

        if (dist > MaxRadius)
            delta = delta.normalized * MaxRadius;

        // UIToolkit Y축은 아래가 +, Unity 게임월드는 위가 +이므로 Y 반전
        Direction = new Vector2(delta.x / MaxRadius, -delta.y / MaxRadius);

        // 핸들 이동 (translate)
        _handle.style.translate = new StyleTranslate(new Translate(delta.x, delta.y));
    }

    private void Reset()
    {
        _isDragging = false;
        Direction   = Vector2.zero;
        _handle.style.translate = new StyleTranslate(new Translate(0, 0));
    }
}
