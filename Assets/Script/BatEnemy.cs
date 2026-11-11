using UnityEngine;

// CHARGER — летучая мышь. Конечный автомат (FSM):
// Patrol → (увидел игрока на экране) → WindUp (дрожит, мигает, «!») → Dash (таран по прямой) → Recover → Patrol.
// Летает между waypoints, таран останавливает стена.
public class BatEnemy : Enemy
{
    public enum State { Patrol, WindUp, Dash, Recover }

    [Header("Patrol (waypoints)")]
    public Vector2[] waypoints;
    public float patrolSpeed = 2.2f;
    public float waveHeight = 0.4f;        // Покачивание вверх-вниз.

    [Header("Dash")]
    public float windUp = 0.7f;            // Замах перед тараном.
    public float dashSpeed = 11f;
    public float dashTime = 0.45f;
    public float recoverTime = 1.4f;       // Отдых после тарана (кулдаун).
    public float returnSpeed = 2.5f;

    [Header("Debug")]
    public State state = State.Patrol;

    private int waypoint;
    private float timer;
    private Vector2 dashDir;
    private Vector2 windUpPos;

    void FixedUpdate()
    {
        if (dead || PauseMenu.IsPaused)
            return;

        switch (state)
        {
            case State.Patrol:
                Patrol();
                // Мышь видит во все стороны (viewAngle = 360), но стены закрывают обзор.
                if (OnScreen() && CanSeePlayer(Vector2.right))
                {
                    state = State.WindUp;
                    timer = windUp;
                    windUpPos = rb.position;
                    BeginWindUp();
                }
                break;

            case State.WindUp:
                timer -= Time.fixedDeltaTime;
                UpdateWindUp(timer);
                // Дрожит на месте — видно, что сейчас атакует.
                rb.MovePosition(windUpPos + Random.insideUnitCircle * 0.06f);
                sr.flipX = Player.position.x < transform.position.x;
                if (timer <= 0f)
                {
                    EndWindUp();
                    dashDir = (PlayerCenter - rb.position).normalized; // Направление фиксируем в конце замаха.
                    state = State.Dash;
                    timer = dashTime;
                }
                break;

            case State.Dash:
                timer -= Time.fixedDeltaTime;
                float step = dashSpeed * Time.fixedDeltaTime;
                bool wall = Physics2D.Raycast(rb.position, dashDir, step + 0.35f, wallMask).collider != null;
                if (wall || timer <= 0f)
                {
                    state = State.Recover;
                    timer = recoverTime;
                    break;
                }
                rb.MovePosition(rb.position + dashDir * step);
                break;

            case State.Recover:
                timer -= Time.fixedDeltaTime;
                // Медленно возвращается к маршруту.
                Vector2 home = waypoints != null && waypoints.Length > 0 ? waypoints[waypoint] : rb.position;
                rb.MovePosition(Vector2.MoveTowards(rb.position, home, returnSpeed * Time.fixedDeltaTime));
                if (timer <= 0f)
                    state = State.Patrol;
                break;
        }
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;
        Vector2 target = waypoints[waypoint];
        Vector2 next = Vector2.MoveTowards(rb.position, target, patrolSpeed * Time.fixedDeltaTime);
        next.y += Mathf.Sin(Time.time * 3f) * waveHeight * Time.fixedDeltaTime * 3f;
        rb.MovePosition(next);
        sr.flipX = target.x < rb.position.x;
        if (Vector2.Distance(rb.position, target) < 0.2f)
            waypoint = (waypoint + 1) % waypoints.Length;
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
