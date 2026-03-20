using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UIDocument 첫 번째 자식을 position:absolute로 전환한 뒤
/// Screen.safeArea에 맞게 left/top/right/bottom을 적용합니다.
/// SurvivorGame(원래 absolute), QuizGame(원래 relative) 모두 동일하게 동작합니다.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SafeAreaApplier : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Rect       _lastSafeArea;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void Update()
    {
        var safeArea = Screen.safeArea;
        if (safeArea == _lastSafeArea) return;

        if (ApplySafeArea(safeArea))
            _lastSafeArea = safeArea;
    }

    private bool ApplySafeArea(Rect safeArea)
    {
        var root = _uiDocument?.rootVisualElement;
        if (root == null || root.childCount == 0) return false;

        float sw = Screen.width;
        float sh = Screen.height;

        float leftPct   = safeArea.xMin / sw * 100f;
        float rightPct  = (sw - safeArea.xMax) / sw * 100f;
        float topPct    = (sh - safeArea.yMax) / sh * 100f;
        float bottomPct = safeArea.yMin / sh * 100f;

        var container = root[0];

        // position:absolute + width/height auto → left/top/right/bottom이 경계를 결정
        // USS의 width:100%, height:100%가 있으면 top 적용 시 하단 overflow 발생하므로 auto로 재정의
        container.style.position = Position.Absolute;
        container.style.width    = StyleKeyword.Auto;
        container.style.height   = StyleKeyword.Auto;
        container.style.left     = Length.Percent(leftPct);
        container.style.right    = Length.Percent(rightPct);
        container.style.top      = Length.Percent(topPct);
        container.style.bottom   = Length.Percent(bottomPct);

        return true;
    }
}
