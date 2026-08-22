using System.Collections.Generic;
using UnityEngine;

public class UIFloating : MonoBehaviour
{
    private readonly List<UIFloatingElement> _cachedElements = new();
    private readonly Queue<UIFloatingElement> _elementPool = new();
    private RectTransform _rectTransform;
    private Canvas _canvas;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();

        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var element = child.GetComponent<UIFloatingElement>();
            if (element == null)
                continue;

            _cachedElements.Add(element);
            PoolElement(element);
        }
    }

    private void Start()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnAttackCallback += OnAttack;
    }

    private void OnDestroy()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnAttackCallback -= OnAttack;
    }

    private void OnAttack(float damage)
    {
        var monster = GameMain.Instance != null ? GameMain.Instance.Monster : null;
        if (monster == null || _rectTransform == null || _canvas == null)
            return;

        var element = GetElement();
        if (element == null)
            return;

        var worldCamera = Camera.main;
        var screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, monster.FloatingPosition);
        var canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPosition,
                canvasCamera,
                out var localPosition))
        {
            PoolElement(element);
            return;
        }

        element.gameObject.SetActive(true);
        element.transform.SetAsLastSibling();
        element.Play(damage, localPosition, PoolElement);
    }

    private void PoolElement(UIFloatingElement element)
    {
        if (element == null)
            return;

        element.gameObject.SetActive(false);
        _elementPool.Enqueue(element);
    }
    
    private UIFloatingElement GetElement()
    {
        UIFloatingElement element = null;
        while (_elementPool.Count > 0)
        {
            element = _elementPool.Dequeue();
            if (element != null)
                break;
        }
        if (element == null && _cachedElements.Count > 0)
        {
            element = Instantiate(_cachedElements[0], transform);
            _cachedElements.Add(element);
        }

        return element;
    }
}
