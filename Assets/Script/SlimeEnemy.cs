using UnityEngine;

// Слизень ходит между двумя точками (патруль). Анимация ходьбы — в Animator.
public class SlimeEnemy : Enemy
{
    public float leftX = 2f;
    public float rightX = 6f;
    public float speed = 2f;

    private int dir = 1;

    void FixedUpdate()
    {
        if (dead)
            return;

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

        // MovePosition перемещает кинематическое тело с учётом физики.
        rb.MovePosition(new Vector2(x, rb.position.y));

        // Спрайт нарисован смотрящим вправо — отражаем, когда идём влево.
        sr.flipX = dir < 0;
    }
}
