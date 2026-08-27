using System.Collections.Generic;
using UnityEngine;

public class UIFloating : MonoBehaviour
{
    [SerializeField] private UIFloatingElement _normalTemplate;
    [SerializeField] private UIFloatingElement _criticalTemplate;

    private readonly List<UIFloatingElement> _normalElements = new();
    private readonly List<UIFloatingElement> _criticalElements = new();
    private readonly Queue<UIFloatingElement> _normalPool = new();
    private readonly Queue<UIFloatingElement> _criticalPool = new();
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

            var isCritical = element == _criticalTemplate || child.name.Contains("Critical");
            if (isCritical)
            {
                _criticalTemplate ??= element;
                _criticalElements.Add(element);
                PoolCriticalElement(element);
            }
            else
            {
                _normalTemplate ??= element;
                _normalElements.Add(element);
                PoolNormalElement(element);
            }
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

    private void OnAttack(BigNumber damage, bool isCritical)
    {
        var monster = GameMain.Instance != null ? GameMain.Instance.Monster : null;
        if (monster == null || _rectTransform == null || _canvas == null)
            return;

        var element = GetElement(isCritical);
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
            PoolElement(element, isCritical);
            return;
        }

        element.gameObject.SetActive(true);
        element.transform.SetAsLastSibling();
        element.Play(
            damage,
            localPosition,
            isCritical ? PoolCriticalElement : PoolNormalElement);
    }

    private void PoolElement(UIFloatingElement element, bool isCritical)
    {
        if (isCritical)
            PoolCriticalElement(element);
        else
            PoolNormalElement(element);
    }

    private void PoolNormalElement(UIFloatingElement element)
    {
        if (element == null)
            return;

        element.gameObject.SetActive(false);
        _normalPool.Enqueue(element);
    }

    private void PoolCriticalElement(UIFloatingElement element)
    {
        if (element == null)
            return;

        element.gameObject.SetActive(false);
        _criticalPool.Enqueue(element);
    }

    private UIFloatingElement GetElement(bool isCritical)
    {
        var pool = isCritical ? _criticalPool : _normalPool;
        var elements = isCritical ? _criticalElements : _normalElements;
        var template = isCritical ? _criticalTemplate : _normalTemplate;
        UIFloatingElement element = null;
        while (pool.Count > 0)
        {
            element = pool.Dequeue();
            if (element != null)
                break;
        }
        if (element == null && template != null)
        {
            element = Instantiate(template, transform);
            element.name = template.name;
            elements.Add(element);
        }

        return element;
    }
}
