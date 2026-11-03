using UnityEngine;

// Фон двигается медленнее камеры — эффект глубины.
[DefaultExecutionOrder(100)]
public class Parallax : MonoBehaviour
{
    [Range(0f, 1f)] public float factor = 0.5f; // 0 — стоит на месте, 1 — едет вместе с камерой.

    private Transform cam;
    private Vector3 startPos;
    private Vector3 camStart;

    void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position;
        camStart = cam.position;
    }

    void LateUpdate()
    {
        Vector3 d = cam.position - camStart;
        transform.position = startPos + new Vector3(d.x * factor, d.y * factor, 0f);
    }
}
