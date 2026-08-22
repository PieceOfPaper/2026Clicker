using UnityEngine;
using UnityEngine.EventSystems;

public class UITouchArea : MonoBehaviour, IPointerDownHandler
{
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (GameMain.Instance != null)
            GameMain.Instance.Touch();
    }
}
