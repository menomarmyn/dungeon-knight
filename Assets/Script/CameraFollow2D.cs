using UnityEngine;

// Камера сбоку следует за героем и не выходит за границы уровня.
[DefaultExecutionOrder(50)]
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1.5f);
    public float smooth = 6f;

    // Границы уровня в мировых координатах.
    public float levelMinX = 0f;
    public float levelMaxX = 100f;
    public float minY = 5f;
    public float maxY = 20f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Половина ширины кадра зависит от экрана телефона (aspect).
        float halfW = cam.orthographicSize * cam.aspect;
        float minX = levelMinX + halfW;
        float maxX = levelMaxX - halfW;

        float x = target.position.x + offset.x;
        x = minX > maxX ? (levelMinX + levelMaxX) / 2f : Mathf.Clamp(x, minX, maxX);
        float y = Mathf.Clamp(target.position.y + offset.y, minY, Mathf.Max(minY, maxY));

        Vector3 wanted = new Vector3(x, y, transform.position.z);
        Vector3 pos = Vector3.Lerp(transform.position - CameraShake.Offset, wanted, smooth * Time.deltaTime);
        CameraShake.Tick();
        transform.position = pos + CameraShake.Offset;
    }
}

// Тряска камеры при уроне и рыке босса.
public static class CameraShake
{
    static float until;
    static float power;
    public static Vector3 Offset { get; private set; }

    public static void Shake(float duration, float magnitude)
    {
        until = Time.time + duration;
        power = magnitude;
    }

    public static void Tick()
    {
        Offset = Time.time < until ? (Vector3)(Random.insideUnitCircle * power) : Vector3.zero;
    }
}
