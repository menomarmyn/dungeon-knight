using UnityEngine;

// Летучая мышь летает влево-вправо и покачивается вверх-вниз (синус).
public class BatEnemy : Enemy
{
    public float leftX = 2f;
    public float rightX = 6f;
    public float speed = 2.5f;
    public float waveHeight = 0.6f;

    private int dir = 1;
    private float baseY;

    void Start()
    {
        baseY = rb.position.y;
    }

    void FixedUpdate()
    {
        if (dead)
            return;

        float x = rb.position.x + dir * speed * Time.fixedDeltaTime;
        if (x >= rightX) { x = rightX; dir = -1; }
        if (x <= leftX) { x = leftX; dir = 1; }

        float y = baseY + Mathf.Sin(Time.time * 3f) * waveHeight;
        rb.MovePosition(new Vector2(x, y));
        sr.flipX = dir < 0;
    }
}
