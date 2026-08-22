using UnityEngine;

public class GameMonster : GameObjBase
{
    [SerializeField] private Transform _floatingPivot;
    [SerializeField] private string _hitTrigger = "Hit";
    [SerializeField, Min(0.02f)] private float _fallbackFeedbackDuration = 0.1f;

    public Vector3 FloatingPosition => _floatingPivot != null
        ? _floatingPivot.position
        : transform.position;

    public void Hit()
    {
        if (!TrySetTrigger(_hitTrigger))
            PlayScaleFeedback(new Vector3(1.08f, 0.88f, 1f), _fallbackFeedbackDuration);
    }
}
