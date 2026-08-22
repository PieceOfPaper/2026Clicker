using System.Collections;
using TMPro;
using UnityEngine;

public class GameMonster : GameObjBase
{
    [SerializeField] private Transform _floatingPivot;
    [SerializeField] private string _hitTrigger = "Hit";
    [SerializeField, Min(0.02f)] private float _fallbackFeedbackDuration = 0.1f;

    protected override void Awake()
    {
        base.Awake();

        if (_floatingPivot == null)
            _floatingPivot = transform;
    }

    public void Hit(float damage)
    {
        if (!TrySetTrigger(_hitTrigger))
            PlayScaleFeedback(new Vector3(1.08f, 0.88f, 1f), _fallbackFeedbackDuration);

        ShowFloatingDamage(damage);
    }

    private void ShowFloatingDamage(float damage)
    {
        var damageObject = new GameObject("FloatingDamage", typeof(TextMeshPro));
        damageObject.transform.position = _floatingPivot.position + Vector3.up * 0.5f;

        var label = damageObject.GetComponent<TextMeshPro>();
        label.text = FormatNumber(damage);
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 6f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.85f, 0.1f, 1f);
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(80, 35, 5, 255);
        label.sortingOrder = 100;

        if (GameMain.Instance != null)
            GameMain.Instance.StartCoroutine(AnimateFloatingDamage(damageObject.transform, label));
        else
            StartCoroutine(AnimateFloatingDamage(damageObject.transform, label));
    }

    private static IEnumerator AnimateFloatingDamage(Transform target, TMP_Text label)
    {
        const float duration = 0.7f;
        const float distance = 1.25f;
        var start = target.position;

        for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            var progress = elapsed / duration;
            target.position = start + Vector3.up * (distance * progress);

            var color = label.color;
            color.a = 1f - progress * progress;
            label.color = color;
            yield return null;
        }

        if (target != null)
            Destroy(target.gameObject);
    }

    private static string FormatNumber(float value)
    {
        if (value >= 1_000_000f)
            return $"{value / 1_000_000f:0.#}M!";

        if (value >= 1_000f)
            return $"{value / 1_000f:0.#}K!";

        return $"{value:0}!";
    }
}
