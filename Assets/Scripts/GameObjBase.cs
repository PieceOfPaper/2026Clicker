using System.Collections;
using UnityEngine;

public class GameObjBase : MonoBehaviour
{
    [SerializeField] protected Animator _animator;

    private Coroutine _feedbackCoroutine;

    protected virtual void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    protected bool TrySetTrigger(string triggerName)
    {
        if (_animator == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        foreach (var parameter in _animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Trigger || parameter.name != triggerName)
                continue;

            _animator.ResetTrigger(triggerName);
            _animator.SetTrigger(triggerName);
            return true;
        }

        return false;
    }

    protected void PlayScaleFeedback(Vector3 targetScaleMultiplier, float duration)
    {
        StopScaleFeedback();

        _feedbackCoroutine = StartCoroutine(ScaleFeedback(targetScaleMultiplier, duration));
    }

    protected void StopScaleFeedback()
    {
        if (_feedbackCoroutine == null)
            return;

        StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = null;
    }

    private IEnumerator ScaleFeedback(Vector3 targetScaleMultiplier, float duration)
    {
        var originalScale = transform.localScale;
        var targetScale = Vector3.Scale(originalScale, targetScaleMultiplier);
        var halfDuration = Mathf.Max(0.01f, duration * 0.5f);

        for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / halfDuration);
            yield return null;
        }

        for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / halfDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        _feedbackCoroutine = null;
    }
}
