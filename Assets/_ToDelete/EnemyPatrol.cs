using UnityEngine;

// Враг ходит между двумя точками.
// Анимация ходьбы задаётся отдельно.
public class EnemyPatrol : MonoBehaviour
{
    public float leftX = 2.8f;
    public float rightX = 6.2f;
    public float speed = 2f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int dir = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        float x = rb.position.x + dir * speed * Time.fixedDeltaTime;

        if (x >= rightX)
        {
            x = rightX;
            dir = -1;
        }
        if (x <= leftX)
        {
            x = leftX;
            dir = 1;
        }

        // MovePosition перемещает тело с учётом физики.
        rb.MovePosition(new Vector2(x, rb.position.y));

        // Отражаем спрайт, когда враг идёт влево.
        sr.flipX = dir < 0;
    }
}
