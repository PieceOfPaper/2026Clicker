using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sample
{
    [DisallowMultipleComponent]
    public sealed class TapDamageDemo : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private float damage = 12800f;
        [SerializeField] private float duration = 0.75f;
        [SerializeField] private float floatDistance = 160f;
        [SerializeField] private Color damageColor = new Color(1f, 0.88f, 0.12f, 1f);

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform bossTarget;

        private void Awake()
        {
            CacheReferences();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CacheReferences();

            if (canvasRect == null || bossTarget == null)
            {
                Debug.LogWarning("TapDamageDemo could not find the Canvas or BattleArea/TitanDummy.", this);
                return;
            }

            StartCoroutine(ShowDamage());
        }

        private void CacheReferences()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvas != null)
            {
                canvasRect = canvas.transform as RectTransform;
            }

            if (bossTarget == null)
            {
                bossTarget = transform.Find("BattleArea/TitanDummy") as RectTransform;
            }
        }

        private IEnumerator ShowDamage()
        {
            var damageObject = new GameObject(
                "FloatingDamage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            var damageRect = damageObject.GetComponent<RectTransform>();
            damageRect.SetParent(canvasRect, false);
            damageRect.anchorMin = new Vector2(0.5f, 0.5f);
            damageRect.anchorMax = new Vector2(0.5f, 0.5f);
            damageRect.pivot = new Vector2(0.5f, 0.5f);
            damageRect.sizeDelta = new Vector2(420f, 110f);
            damageRect.SetAsLastSibling();

            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            var bossScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, bossTarget.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                bossScreenPosition,
                uiCamera,
                out var bossLocalPosition);

            var startPosition = bossLocalPosition + new Vector2(Random.Range(-90f, 90f), 70f);
            damageRect.anchoredPosition = startPosition;

            var label = damageObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = FormatDamage(damage);
            label.fontSize = 56f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = damageColor;
            label.raycastTarget = false;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(79, 35, 8, 255);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                damageRect.anchoredPosition = startPosition + Vector2.up * (floatDistance * progress);

                var color = damageColor;
                color.a = 1f - Mathf.Pow(progress, 2f);
                label.color = color;

                yield return null;
            }

            Destroy(damageObject);
        }

        private static string FormatDamage(float value)
        {
            if (value >= 1000000f)
            {
                return $"{value / 1000000f:0.#}M!";
            }

            if (value >= 1000f)
            {
                return $"{value / 1000f:0.#}K!";
            }

            return $"{value:0}!";
        }
    }
}
