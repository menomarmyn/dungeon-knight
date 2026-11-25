using UnityEngine;

// PATROLLER — слизень. Конечный автомат (FSM):
// Patrol → (увидел игрока) → Chase → (близко) → WindUp → Lunge → Cooldown → Chase / Patrol.
// Ходит по точкам-waypoints, видит лучом (Raycast) в конусе перед собой, стены закрывают обзор.
public class SlimeEnemy : Enemy
{
    public enum State { Patrol, Chase, WindUp, Lunge, Cooldown }

    [Header("Patrol (waypoints)")]
    public Vector2[] waypoints;            // Точки маршрута (задаёт уровень).

    // Все числа (скорости, замах, кулдаун) — в ассете Slime.asset.
    SlimeStats S => (SlimeStats)stats;

    [Header("Debug")]
    public State state = State.Patrol;

    private int waypoint;
    private float timer;
    private float lastSeen = -10f;
    private int dir = 1;                   // 1 — вправо, -1 — влево.

    Vector2 Facing => new Vector2(dir, 0f);

    void FixedUpdate()
    {
        if (dead || PauseMenu.IsPaused)
            return;

        bool sees = CanSeePlayer(Facing);
        if (sees)
            lastSeen = Time.time;

        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (sees && OnScreen())
                    state = State.Chase;
                break;

            case State.Chase:
                FacePlayer();
                if (Time.time - lastSeen > S.loseSightTime)
                {
                    state = State.Patrol; // Потерял игрока — возвращается к маршруту.
                    break;
                }
                if (Mathf.Abs(Player.position.x - transform.position.x) <= S.attackRange && sees)
                {
                    state = State.WindUp;
                    timer = S.windUp;
                    BeginWindUp();
                    break;
                }
                Move(dir * S.chaseSpeed);
                break;

            case State.WindUp:
                timer -= Time.fixedDeltaTime;
                UpdateWindUp(timer);
                if (timer <= 0f)
                {
                    EndWindUp();
                    state = State.Lunge;
                    timer = S.lungeTime;
                }
                break;

            case State.Lunge:
                timer -= Time.fixedDeltaTime;
                if (!Move(dir * S.lungeSpeed) || timer <= 0f)
                {
                    state = State.Cooldown;
                    timer = S.cooldown;
                }
                break;

            case State.Cooldown:
                timer -= Time.fixedDeltaTime;
                if (timer <= 0f)
                    state = Time.time - lastSeen < S.loseSightTime ? State.Chase : State.Patrol;
                break;
        }

        sr.flipX = dir < 0; // Спрайт нарисован смотрящим вправо.
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;
        if (timer > 0f)
        {
            timer -= Time.fixedDeltaTime; // Ждём на точке.
            return;
        }
        Vector2 target = waypoints[waypoint];
        float dx = target.x - rb.position.x;
        if (Mathf.Abs(dx) < 0.05f || !Move(Mathf.Sign(dx) * S.patrolSpeed))
        {
            waypoint = (waypoint + 1) % waypoints.Length; // Следующая точка по кругу.
            timer = S.waitAtPoint;
        }
        else
        {
            dir = dx > 0 ? 1 : -1;
        }
    }

    void FacePlayer()
    {
        dir = Player.position.x > transform.position.x ? 1 : -1;
    }

    // Шаг по горизонтали. false — впереди стена или обрыв, дальше не идём.
    bool Move(float speed)
    {
        float step = speed * Time.fixedDeltaTime;
        int s = speed > 0 ? 1 : -1;
        Vector2 pos = rb.position;

        bool wallAhead = Physics2D.Raycast(pos, new Vector2(s, 0f), 0.55f, wallMask).collider != null;
        bool groundAhead = Physics2D.Raycast(pos + new Vector2(s * 0.5f, 0f), Vector2.down, 0.9f, wallMask).collider != null;
        if (wallAhead || !groundAhead)
            return false;

        rb.MovePosition(pos + new Vector2(step, 0f));
        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        if (waypoints != null)
            foreach (Vector2 p in waypoints)
                Gizmos.DrawWireCube(p, Vector3.one * 0.3f);
    }
}
