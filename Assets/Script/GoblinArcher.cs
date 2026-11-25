using UnityEngine;

// SHOOTER — гоблин-лучник. Конечный автомат (FSM):
// Watch (смотрит, иногда оборачивается) → (увидел) → Aim (красный прицел 0.6 с) → Shoot (стрела) → Cooldown → Watch / Aim.
// Стоит на месте, видит лучом в конусе перед собой, стены закрывают обзор.
public class GoblinArcher : Enemy
{
    public enum State { Watch, Aim, Shoot, Cooldown }

    [Header("Watch")]
    public bool startFacingRight = false;

    [Header("Aim & Shoot")]
    public EnemyProjectile arrowPrefab;    // Скорость и урон стрелы — в Arrow.asset.
    public AudioClip shootClip;
    public Vector2 bowOffset = new Vector2(0.45f, 0.1f);

    [Header("Aim line (telegraph)")]
    public SpriteRenderer aimLine;         // Тонкая красная линия от лука к цели.
    public float pixelUnits = 0.25f;       // Длина спрайта линии при scale = 1.

    [Header("Debug")]
    public State state = State.Watch;

    private int dir;
    private float timer;
    private float turnTimer;
    private Vector2 target;
    private Animator anim;

    // Время прицеливания, перезарядка, разворот — в ассете GoblinArcher.asset.
    ArcherStats S => (ArcherStats)stats;

    Vector2 Facing => new Vector2(dir, 0f);
    Vector2 Bow => (Vector2)transform.position + new Vector2(bowOffset.x * dir, bowOffset.y);

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponent<Animator>();
        dir = startFacingRight ? 1 : -1;
        if (aimLine != null)
            aimLine.gameObject.SetActive(false);
    }

    // Куда смотрит в начале (ставит LevelLoader).
    public void SetFacing(bool right)
    {
        startFacingRight = right;
        dir = right ? 1 : -1;
    }

    void FixedUpdate()
    {
        if (dead || PauseMenu.IsPaused)
            return;

        bool sees = CanSeePlayer(Facing);

        switch (state)
        {
            case State.Watch:
                turnTimer += Time.fixedDeltaTime;
                if (turnTimer >= S.turnAroundTime)
                {
                    turnTimer = 0f;
                    dir = -dir; // Оглядывается — сзади к нему тоже не подкрасться надолго.
                }
                if (sees && OnScreen())
                    StartAim();
                break;

            case State.Aim:
                timer -= Time.fixedDeltaTime;
                if (!sees && timer > S.lockTime)
                {
                    StopAim(); // Игрок спрятался за стеной — отменяем выстрел.
                    state = State.Watch;
                    break;
                }
                if (timer > S.lockTime)
                {
                    target = PlayerCenter;            // Прицел следит за игроком...
                    dir = target.x > transform.position.x ? 1 : -1;
                }
                DrawAimLine(timer);                   // ...а в конце замирает.
                if (timer <= 0f)
                    state = State.Shoot;
                break;

            case State.Shoot:
                StopAim();
                Vector2 d = (target - Bow).normalized;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                Instantiate(arrowPrefab, Bow, Quaternion.Euler(0f, 0f, angle));
                GameAudio.Play(shootClip, 0.7f);
                state = State.Cooldown;
                timer = S.cooldown;
                break;

            case State.Cooldown:
                timer -= Time.fixedDeltaTime;
                if (timer <= 0f)
                {
                    if (sees && OnScreen())
                        StartAim();
                    else
                        state = State.Watch;
                }
                break;
        }

        sr.flipX = dir < 0;
    }

    void StartAim()
    {
        state = State.Aim;
        timer = S.aimTime;
        target = PlayerCenter;
        BeginWindUp();
        if (anim != null)
            anim.SetBool("Aiming", true);
        if (aimLine != null)
            aimLine.gameObject.SetActive(true);
    }

    void StopAim()
    {
        EndWindUp();
        if (anim != null)
            anim.SetBool("Aiming", false);
        if (aimLine != null)
            aimLine.gameObject.SetActive(false);
    }

    // Красная линия прицела: ставим в середину между луком и целью, поворачиваем и растягиваем.
    void DrawAimLine(float timeLeft)
    {
        if (aimLine == null)
            return;
        Vector2 from = Bow;
        Vector2 diff = target - from;
        Transform t = aimLine.transform;
        t.position = from + diff / 2f;
        t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        t.localScale = new Vector3(diff.magnitude / pixelUnits, 0.35f, 1f);
        // Мигает быстрее, когда прицел зафиксирован.
        float blink = timeLeft <= S.lockTime ? 0.06f : 0.14f;
        Color c = aimLine.color;
        c.a = Mathf.Repeat(timeLeft, blink) < blink / 2f ? 0.9f : 0.35f;
        aimLine.color = c;
    }
}
