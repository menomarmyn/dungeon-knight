using UnityEngine;

// Платформу двигает Animation Clip (Animator на родителе, режим Animate Physics).
// Этот скрипт только считает скорость платформы, чтобы герой ехал вместе с ней.
public class MovingPlatform : MonoBehaviour
{
    public Vector2 Velocity { get; private set; }

    private Vector2 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    void FixedUpdate()
    {
        Vector2 pos = transform.position;
        Velocity = (pos - lastPos) / Time.fixedDeltaTime;
        lastPos = pos;
    }
}
