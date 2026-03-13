using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LoadingIndicator : MonoBehaviour
{
    [SerializeField]
    private UIDocument _uiDocument;

    private readonly List<Label> _dots = new List<Label>();
    private IVisualElementScheduledItem _scheduleItem;
    private int _step;

    private void Awake()
    {
        if (_uiDocument == null)
        {
            Debug.LogError("UIDocumentê°€ ? ë‹¹?˜ì? ?Šì•˜?µë‹ˆ??", this);
            return;
        }

        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("rootVisualElementë¥?ê°€?¸ì˜¤ì§€ ëª»í–ˆ?µë‹ˆ??", this);
            return;
        }

        VisualElement container = root.Q<VisualElement>("loading-indicator");
        if (container == null)
        {
            Debug.LogError("loading-indicator ?”ì†Œë¥?ì°¾ì? ëª»í–ˆ?µë‹ˆ??", this);
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            Label dot = container.Q<Label>($"dot-{i}");
            if (dot != null)
            {
                _dots.Add(dot);
            }
        }

        if (_dots.Count == 0)
        {
            Debug.LogWarning("ë¡œë”© ?¸ë””ì¼€?´í„°????Label??ì°¾ì? ëª»í–ˆ?µë‹ˆ??", this);
        }

        _scheduleItem = container.schedule.Execute(UpdateDots).Every(400);
    }

    private void OnDestroy()
    {
        if (_scheduleItem != null)
        {
            _scheduleItem.Pause();
            _scheduleItem = null;
        }
    }

    private void UpdateDots()
    {
        _step++;
        int activeCount = (_step % 3) + 1;

        for (int i = 0; i < _dots.Count; i++)
        {
            if (i < activeCount)
            {
                _dots[i].text = ".";
            }
            else
            {
                _dots[i].text = string.Empty;
            }
        }
    }
}

