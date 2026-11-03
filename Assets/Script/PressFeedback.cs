using UnityEngine;
using UnityEngine.EventSystems;

// Экранная кнопка чуть уменьшается при нажатии, чтобы было видно касание.
public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressedScale = 0.88f;

    public void OnPointerDown(PointerEventData e)
    {
        transform.localScale = Vector3.one * pressedScale;
    }

    public void OnPointerUp(PointerEventData e)
    {
        transform.localScale = Vector3.one;
    }
}
