using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class UIFloatingElement : MonoBehaviour
{
    private static readonly string[] s_suffixes =
    {
        string.Empty, "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
    };

    [SerializeField] private RectTransform _pivot;
    [SerializeField] private TMP_Text _textDamage;
    [SerializeField, Min(0.05f)] private float _duration = 0.7f;
    [SerializeField, Min(1f)] private float _riseDistance = 100f;

    private RectTransform _rectTransform;
    private Coroutine _animationCoroutine;
    private Color _baseColor = Color.white;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (_textDamage != null)
            _baseColor = _textDamage.color;
    }

    public void Play(BigNumber damage, Vector2 anchoredPosition, Action<UIFloatingElement> onComplete)
    {
        if (_rectTransform == null || _pivot == null || _textDamage == null)
        {
            onComplete?.Invoke(this);
            return;
        }

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _rectTransform.anchoredPosition = anchoredPosition;
        _pivot.anchoredPosition = Vector2.zero;
        _textDamage.text = FormatNumber(damage);
        _textDamage.color = _baseColor;
        _animationCoroutine = StartCoroutine(Animate(onComplete));
    }

    private IEnumerator Animate(Action<UIFloatingElement> onComplete)
    {
        for (var elapsed = 0f; elapsed < _duration; elapsed += Time.deltaTime)
        {
            var progress = elapsed / _duration;
            _pivot.anchoredPosition = Vector2.up * (_riseDistance * progress);

            var color = _baseColor;
            color.a *= 1f - progress * progress;
            _textDamage.color = color;
            yield return null;
        }

        _animationCoroutine = null;
        onComplete?.Invoke(this);
    }

    private static string FormatNumber(BigNumber value)
    {
        var absolute = BigNumber.Abs(value);
        if (absolute < 1_000)
            return value.ToDouble().ToString("0.#", CultureInfo.InvariantCulture) + "!";

        var suffixIndex = value.Exponent / 3;
        if (suffixIndex > 0 && suffixIndex < s_suffixes.Length)
        {
            var scaled = value.Mantissa * Math.Pow(10d, value.Exponent - suffixIndex * 3);
            return scaled.ToString("0.#", CultureInfo.InvariantCulture) + s_suffixes[suffixIndex] + "!";
        }

        return value.Mantissa.ToString("0.##", CultureInfo.InvariantCulture) +
               "e" + value.Exponent.ToString(CultureInfo.InvariantCulture) + "!";
    }
}
