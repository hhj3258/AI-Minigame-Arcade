using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 퀴즈 UI Toolkit 테스트용 탭 전환 컨트롤러
/// </summary>
public class QuizUITabController : MonoBehaviour
{
    private static readonly string[] PanelNames = { "panel-topic", "panel-loading", "panel-gameplay", "panel-result" };
    private static readonly string[] TabNames   = { "tab-topic",   "tab-loading",   "tab-gameplay",   "tab-result" };

    private VisualElement _root;
    private VisualElement[] _panels;
    private Button[] _tabs;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("UIDocument 컴포넌트를 찾지 못했습니다.", this);
            return;
        }

        _root = doc.rootVisualElement;

        _panels = new VisualElement[PanelNames.Length];
        _tabs   = new Button[TabNames.Length];

        for (int i = 0; i < PanelNames.Length; i++)
        {
            _panels[i] = _root.Q<VisualElement>(PanelNames[i]);
            _tabs[i]   = _root.Q<Button>(TabNames[i]);

            int index = i;
            if (_tabs[i] != null)
            {
                _tabs[i].clicked += () => SwitchTo(index);
            }
        }

        SwitchTo(0);
    }

    private void SwitchTo(int index)
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            bool active = (i == index);

            if (_panels[i] != null)
            {
                if (active) _panels[i].RemoveFromClassList("hidden");
                else        _panels[i].AddToClassList("hidden");
            }

            if (_tabs[i] != null)
            {
                if (active) _tabs[i].AddToClassList("active");
                else        _tabs[i].RemoveFromClassList("active");
            }
        }
    }
}
