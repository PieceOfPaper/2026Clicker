using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ResponsiveBattleLayout : MonoBehaviour
{
    [Serializable]
    private struct LayoutProfile
    {
        public Vector3 CameraPosition;
        public float OrthographicSize;
        public Rect BattleViewport;
        public Vector2 CombatFocus;
        public Vector2 CameraOffset;
        public Vector3 CharacterPosition;
        public Vector3 MonsterPosition;
    }

    [Header("World")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _characterPivot;
    [SerializeField] private Transform _monsterPivot;

    [Header("Responsive UI")]
    [SerializeField] private RectTransform _safeArea;
    [SerializeField] private RectTransform _currencyPanel;
    [SerializeField] private RectTransform _battleHud;
    [SerializeField] private RectTransform _upgradePanel;
    [SerializeField] private RectTransform _skillPanel;
    [SerializeField] private RectTransform _menuPanel;

    [Header("Profiles")]
    [SerializeField] private LayoutProfile _landscape = new()
    {
        CameraPosition = new Vector3(1.25f, 0f, -10f),
        OrthographicSize = 6.7f,
        BattleViewport = new Rect(0.29f, 0.16f, 0.71f, 0.84f),
        CombatFocus = new Vector2(-0.95f, -1f),
        CameraOffset = new Vector2(0f, 3f),
        CharacterPosition = new Vector3(-2.8f, -3.31f, 0f),
        MonsterPosition = new Vector3(2.2f, -1.56f, 0f),
    };

    [SerializeField] private LayoutProfile _portrait = new()
    {
        CameraPosition = new Vector3(0f, -3f, -10f),
        OrthographicSize = 8.4f,
        BattleViewport = new Rect(0f, 0.34f, 1f, 0.59f),
        CombatFocus = new Vector2(0f, -1.31f),
        CameraOffset = Vector2.zero,
        CharacterPosition = new Vector3(-2.15f, -3.31f, 0f),
        MonsterPosition = new Vector3(1.65f, -1.56f, 0f),
    };

    private Vector2Int _lastScreenSize;
    private Vector2 _lastCanvasSize;
    private Rect _lastSafeArea;

    private void Awake()
    {
        Refresh(true);
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        var safeArea = Screen.safeArea;
        var canvasSize = GetCanvasSize();
        if (!force && screenSize == _lastScreenSize && canvasSize == _lastCanvasSize && safeArea == _lastSafeArea)
            return;

        _lastScreenSize = screenSize;
        _lastCanvasSize = canvasSize;
        _lastSafeArea = safeArea;
        ApplySafeArea(safeArea, screenSize);

        var isLandscape = canvasSize.x >= canvasSize.y;
        ApplyWorldProfile(isLandscape ? _landscape : _portrait);
        ApplyUiProfile(isLandscape);
    }

    private Vector2 GetCanvasSize()
    {
        var canvasRect = _safeArea != null ? _safeArea.parent as RectTransform : null;
        if (canvasRect != null && canvasRect.rect.width > 0f && canvasRect.rect.height > 0f)
            return canvasRect.rect.size;

        if (_worldCamera != null && _worldCamera.pixelHeight > 0)
            return new Vector2(_worldCamera.pixelWidth, _worldCamera.pixelHeight);

        return new Vector2(Screen.width, Screen.height);
    }

    private void ApplySafeArea(Rect safeArea, Vector2Int screenSize)
    {
        if (_safeArea == null || screenSize.x <= 0 || screenSize.y <= 0)
            return;

        _safeArea.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
        _safeArea.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        _safeArea.offsetMin = Vector2.zero;
        _safeArea.offsetMax = Vector2.zero;
    }

    private void ApplyWorldProfile(LayoutProfile profile)
    {
        if (_worldCamera != null)
        {
            _worldCamera.orthographicSize = profile.OrthographicSize;

            var canvasSize = GetCanvasSize();
            var aspect = canvasSize.y > 0f ? canvasSize.x / canvasSize.y : _worldCamera.aspect;
            var worldHeight = profile.OrthographicSize * 2f;
            var worldWidth = worldHeight * aspect;
            var viewportCenter = profile.BattleViewport.center;
            var viewportOffset = viewportCenter - new Vector2(0.5f, 0.5f);
            var cameraPosition = new Vector3(
                profile.CombatFocus.x - viewportOffset.x * worldWidth + profile.CameraOffset.x,
                profile.CombatFocus.y - viewportOffset.y * worldHeight + profile.CameraOffset.y,
                profile.CameraPosition.z);
            _worldCamera.transform.position = cameraPosition;
        }

        if (_characterPivot != null)
            _characterPivot.position = profile.CharacterPosition;
        if (_monsterPivot != null)
            _monsterPivot.position = profile.MonsterPosition;
    }

    private void ApplyUiProfile(bool landscape)
    {
        if (landscape)
        {
            SetRect(_currencyPanel, new Vector2(0f, 0.88f), new Vector2(0.29f, 1f), Vector2.zero, Vector2.zero);
            SetRect(_upgradePanel, new Vector2(0f, 0.08f), new Vector2(0.29f, 0.88f), Vector2.zero, Vector2.zero);
            SetRect(_battleHud, new Vector2(0.38f, 0.78f), new Vector2(0.88f, 0.98f), Vector2.zero, Vector2.zero);
            SetRect(_skillPanel, new Vector2(0.45f, 0.02f), new Vector2(0.82f, 0.16f), Vector2.zero, Vector2.zero);
            SetRect(_menuPanel, new Vector2(0.88f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }
        else
        {
            SetRect(_currencyPanel, new Vector2(0f, 0.93f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);
            SetRect(_menuPanel, new Vector2(0.76f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            SetRect(_battleHud, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            SetRect(_skillPanel, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.43f), Vector2.zero, Vector2.zero);
            SetRect(_upgradePanel, new Vector2(0f, 0f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);
        }

        ConfigureCurrencyPanel(landscape);
        ConfigureUpgradePanel(landscape);
        ConfigureSkillPanel(landscape);
        Canvas.ForceUpdateCanvases();
        if (_currencyPanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_currencyPanel);
        if (_upgradePanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_upgradePanel);
        if (_skillPanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_skillPanel);
    }

    private void ConfigureCurrencyPanel(bool landscape)
    {
        if (_currencyPanel == null)
            return;

        var layout = _currencyPanel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = landscape
                ? new RectOffset(18, 18, 10, 10)
                : new RectOffset(14, 14, 8, 8);
            layout.spacing = landscape ? 12f : 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        for (var i = 0; i < _currencyPanel.childCount; i++)
        {
            var child = _currencyPanel.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            child.localScale = Vector3.one;
            var element = child.GetComponent<LayoutElement>();
            if (element == null)
                element = child.gameObject.AddComponent<LayoutElement>();

            element.minWidth = landscape ? 150f : 180f;
            element.preferredWidth = i == 0
                ? (landscape ? 300f : 360f)
                : (landscape ? 190f : 240f);
            element.flexibleWidth = i == 0 ? 1.2f : 0.8f;
            element.minHeight = landscape ? 56f : 64f;
            element.preferredHeight = landscape ? 84f : 82f;
            element.flexibleHeight = 1f;
        }
    }

    private void ConfigureUpgradePanel(bool landscape)
    {
        if (_upgradePanel == null)
            return;

        var layout = _upgradePanel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = landscape
                ? new RectOffset(16, 16, 14, 14)
                : new RectOffset(18, 18, 12, 12);
            layout.spacing = landscape ? 10f : 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        for (var i = 0; i < _upgradePanel.childCount; i++)
        {
            var child = _upgradePanel.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            child.localScale = Vector3.one;
            var element = child.GetComponent<LayoutElement>();
            if (element == null)
                element = child.gameObject.AddComponent<LayoutElement>();

            var isPurchaseUnits = i == 0;
            element.minHeight = isPurchaseUnits ? 52f : (landscape ? 118f : 96f);
            element.preferredHeight = isPurchaseUnits ? 58f : (landscape ? 150f : 112f);
            element.flexibleHeight = isPurchaseUnits ? 0f : 1f;
            element.flexibleWidth = 1f;
        }
    }

    private void ConfigureSkillPanel(bool landscape)
    {
        if (_skillPanel == null)
            return;

        var layout = _skillPanel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = landscape
                ? new RectOffset(8, 8, 6, 6)
                : new RectOffset(10, 10, 6, 6);
            layout.spacing = landscape ? 18f : 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        for (var i = 0; i < _skillPanel.childCount; i++)
        {
            var button = _skillPanel.GetChild(i) as RectTransform;
            if (button == null)
                continue;

            button.localScale = Vector3.one;
            var element = button.GetComponent<LayoutElement>();
            if (element == null)
                element = button.gameObject.AddComponent<LayoutElement>();

            element.minWidth = landscape ? 100f : 120f;
            element.preferredWidth = landscape ? 150f : 180f;
            element.flexibleWidth = 1f;
            element.minHeight = landscape ? 72f : 82f;
            element.preferredHeight = landscape ? 108f : 116f;
            element.flexibleHeight = 1f;

            var label = button.Find("Label") as RectTransform;
            if (label == null)
                continue;

            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.pivot = new Vector2(0.5f, 0.5f);
            label.offsetMin = landscape ? new Vector2(10f, 8f) : new Vector2(8f, 8f);
            label.offsetMax = landscape ? new Vector2(-10f, -8f) : new Vector2(-8f, -8f);
            label.localScale = Vector3.one;

            var text = label.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = landscape ? 22f : 24f;
                text.margin = Vector4.zero;
            }
        }
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
