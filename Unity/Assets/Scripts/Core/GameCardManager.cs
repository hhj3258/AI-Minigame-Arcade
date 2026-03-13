using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameCardManager : MonoBehaviour
{
    [SerializeField]
    private UIDocument _uiDocument;

    private VisualElement _root;
    private VisualElement _cardContainer;
    private readonly List<VisualElement> _cards = new List<VisualElement>();

    private int _currentIndex;
    private bool _isDragging;
    private float _startY;

    private const float DragThresholdRatio = 0.3f;

    private void Awake()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        if (_uiDocument == null)
        {
            Debug.LogError("UIDocument가 할당되지 않았습니다.", this);
            return;
        }

        _root = _uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("rootVisualElement를 가져오지 못했습니다.", this);
            return;
        }

        _cardContainer = _root.Q<VisualElement>("card-container");
        if (_cardContainer == null)
        {
            Debug.LogError("card-container 요소를 찾지 못했습니다.", this);
            return;
        }

        foreach (VisualElement child in _cardContainer.Children())
        {
            _cards.Add(child);
        }

        _root.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnDestroy()
    {
        if (_root == null)
        {
            return;
        }

        _root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        _root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_cards.Count == 0)
        {
            return;
        }

        _isDragging = true;
        _startY = evt.position.y;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging || _cards.Count == 0)
        {
            return;
        }

        float deltaY = evt.position.y - _startY;
        float height = _root.resolvedStyle.height;
        if (height <= 0f)
        {
            return;
        }

        float baseOffset = -_currentIndex * height;
        float offset = baseOffset + deltaY;

        _cardContainer.style.translate = new Translate(0f, offset);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDragging || _cards.Count == 0)
        {
            return;
        }

        _isDragging = false;

        float deltaY = evt.position.y - _startY;
        float height = _root.resolvedStyle.height;
        if (height <= 0f)
        {
            SnapToCurrent(height);
            return;
        }

        float threshold = height * DragThresholdRatio;
        if (Mathf.Abs(deltaY) >= threshold)
        {
            if (deltaY > 0f)
            {
                _currentIndex = Mathf.Max(0, _currentIndex - 1);
            }
            else
            {
                _currentIndex = Mathf.Min(_cards.Count - 1, _currentIndex + 1);
            }
        }

        SnapToCurrent(height);
    }

    private void SnapToCurrent(float height)
    {
        float offset = -_currentIndex * height;
        _cardContainer.style.translate = new Translate(0f, offset);
    }
}

