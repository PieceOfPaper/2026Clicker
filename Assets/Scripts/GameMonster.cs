using System.Collections;
using UnityEngine;

public class GameMonster : GameObjBase
{
    [SerializeField] private Transform _floatingPivot;
    [SerializeField] private string _appearTrigger = "Appear";
    [SerializeField] private string _hitTrigger = "Hit";
    [SerializeField] private string _defeatTrigger = "Defeat";
    [SerializeField, Min(0.02f)] private float _appearanceMotionDuration = 0.35f;
    [SerializeField, Min(0.02f)] private float _fallbackFeedbackDuration = 0.1f;
    [SerializeField, Min(0.02f)] private float _fallbackDefeatDuration = 0.45f;

    private Coroutine _motionCoroutine;
    private Vector3 _defaultScale;

    public bool IsAppearing { get; private set; }
    public bool IsDefeated { get; private set; }

    public Vector3 FloatingPosition => _floatingPivot != null
        ? _floatingPivot.position
        : transform.position;

    protected override void Awake()
    {
        base.Awake();
        _defaultScale = transform.localScale;
    }

    public void Appear()
    {
        StopMotion();
        IsAppearing = true;
        IsDefeated = false;

        var hasAnimatorMotion = TrySetTrigger(_appearTrigger);
        _motionCoroutine = StartCoroutine(AppearRoutine(hasAnimatorMotion));
    }

    public void Hit()
    {
        if (IsAppearing || IsDefeated)
            return;

        if (!TrySetTrigger(_hitTrigger))
            PlayScaleFeedback(new Vector3(1.08f, 0.88f, 1f), _fallbackFeedbackDuration);
    }

    public void Defeat()
    {
        if (IsDefeated)
            return;

        StopMotion();
        StopScaleFeedback();
        IsAppearing = false;
        IsDefeated = true;

        if (!TrySetTrigger(_defeatTrigger))
            _motionCoroutine = StartCoroutine(FallbackDefeatRoutine());
    }

    private IEnumerator AppearRoutine(bool hasAnimatorMotion)
    {
        if (hasAnimatorMotion)
        {
            yield return new WaitForSeconds(_appearanceMotionDuration);
        }
        else
        {
            var startScale = Vector3.Scale(_defaultScale, new Vector3(0.25f, 0.25f, 1f));
            var overshootScale = Vector3.Scale(_defaultScale, new Vector3(1.12f, 1.12f, 1f));
            transform.localScale = startScale;

            var growDuration = _appearanceMotionDuration * 0.75f;
            for (var elapsed = 0f; elapsed < growDuration; elapsed += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(startScale, overshootScale, elapsed / growDuration);
                yield return null;
            }

            var settleDuration = _appearanceMotionDuration - growDuration;
            for (var elapsed = 0f; elapsed < settleDuration; elapsed += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(overshootScale, _defaultScale, elapsed / settleDuration);
                yield return null;
            }

            transform.localScale = _defaultScale;
        }

        IsAppearing = false;
        _motionCoroutine = null;
    }

    private IEnumerator FallbackDefeatRoutine()
    {
        var startScale = transform.localScale;
        var squashScale = Vector3.Scale(_defaultScale, new Vector3(1.2f, 0.75f, 1f));
        var squashDuration = _fallbackDefeatDuration * 0.35f;

        for (var elapsed = 0f; elapsed < squashDuration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, squashScale, elapsed / squashDuration);
            yield return null;
        }

        var disappearDuration = _fallbackDefeatDuration - squashDuration;
        for (var elapsed = 0f; elapsed < disappearDuration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(squashScale, Vector3.zero, elapsed / disappearDuration);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        _motionCoroutine = null;
    }

    private void StopMotion()
    {
        if (_motionCoroutine == null)
            return;

        StopCoroutine(_motionCoroutine);
        _motionCoroutine = null;
    }
}
