using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class UIFloatingElement : MonoBehaviour
{
    [SerializeField] private TMP_Text _textDamage;
    [SerializeField, Min(0.05f)] private float _duration = 0.7f;
    [SerializeField, Min(1f)] private float _riseDistance = 100f;

    private RectTransform _rectTransform;
    private Coroutine _animationCoroutine;
    private Color _baseColor = Color.white;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (_textDamage == null)
            _textDamage = GetComponentInChildren<TMP_Text>();

        if (_textDamage != null)
            _baseColor = _textDamage.color;
    }

    public void Play(float damage, Vector2 anchoredPosition, Action<UIFloatingElement> onComplete)
    {
        if (_rectTransform == null || _textDamage == null)
        {
            onComplete?.Invoke(this);
            return;
        }

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _rectTransform.anchoredPosition = anchoredPosition;
        _textDamage.text = FormatNumber(damage);
        _textDamage.color = _baseColor;
        _animationCoroutine = StartCoroutine(Animate(anchoredPosition, onComplete));
    }

    private IEnumerator Animate(Vector2 startPosition, Action<UIFloatingElement> onComplete)
    {
        for (var elapsed = 0f; elapsed < _duration; elapsed += Time.deltaTime)
        {
            var progress = elapsed / _duration;
            _rectTransform.anchoredPosition = startPosition + Vector2.up * (_riseDistance * progress);

            var color = _baseColor;
            color.a *= 1f - progress * progress;
            _textDamage.color = color;
            yield return null;
        }

        _animationCoroutine = null;
        onComplete?.Invoke(this);
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
