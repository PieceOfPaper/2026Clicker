using UnityEngine;

public class GameCharacter : GameObjBase
{
    [SerializeField] private string _attackTrigger = "Attack";
    [SerializeField, Min(0.02f)] private float _fallbackFeedbackDuration = 0.12f;

    public void Attack()
    {
        if (!TrySetTrigger(_attackTrigger))
            PlayScaleFeedback(new Vector3(1.12f, 0.9f, 1f), _fallbackFeedbackDuration);
    }
}
