using UnityEngine;

// Не даёт кнопкам залезть под «чёлку» iPhone: подгоняем RectTransform под Screen.safeArea.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private Rect last;

    void Update()
    {
        Rect safe = Screen.safeArea;
        if (safe == last || Screen.width == 0)
            return;
        last = safe;

        RectTransform rt = (RectTransform)transform;
        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        rt.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
        rt.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
    }
}
