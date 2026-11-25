using System.Collections;
using UnityEngine;

// Босс «Король слизней». Наследуется от Enemy.
// Цикл: перелёт -> веер сфер -> пауза -> огненный дождь с предупреждением -> пауза.
// Ниже phase2At HP — вторая фаза: атакует чаще, веер шире, колонн огня больше.
// Все числа атак — в ассете SlimeKing.asset (BossStats).
public class Boss : Enemy
{
    [Header("Attacks")]
    public EnemyProjectile orbPrefab;      // Сфера для веера.
    public EnemyProjectile fireballPrefab; // Огненный шар для дождя.
    public GameObject warningPrefab;       // Красная колонна и «!».
    public AudioClip shotClip;
    public AudioClip warningClip;
    public AudioClip roarClip;

    [Header("Arena")]
    public float arenaLeft = 2f;
    public float arenaRight = 34f;
    public float floorY = 2f;
    public float ceilingY = 14f;

    private int maxHp;
    private Transform hero; // Своё имя: в базовом Enemy уже есть приватное поле player.
    private Animator anim;

    BossStats S => (BossStats)stats;
    bool Phase2 => hp <= maxHp * S.phase2At;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponent<Animator>();
        maxHp = stats.maxHp;
    }

    IEnumerator Start()
    {
        hero = GameObject.FindGameObjectWithTag("Player").transform;
        if (HUD.Instance != null)
            HUD.Instance.ShowBoss(hp, maxHp);

        // Вступление: рык и небольшая пауза, чтобы игрок осмотрелся.
        yield return new WaitForSeconds(S.introDelay);
        GameAudio.Play(roarClip);
        CameraShake.Shake(0.5f, 0.25f);
        yield return new WaitForSeconds(S.introDelay);
        StartCoroutine(Brain());
    }

    IEnumerator Brain()
    {
        while (!dead)
        {
            yield return MoveTo(RandomSpot(), S.moveTime);
            yield return FanAttack();
            yield return new WaitForSeconds(Phase2 ? S.pauseAfterFanPhase2 : S.pauseAfterFan); // Окно, чтобы подойти и стрелять.

            yield return RainAttack();
            yield return new WaitForSeconds(Phase2 ? S.pauseAfterRainPhase2 : S.pauseAfterRain);

            if (Phase2)
            {
                yield return MoveTo(RandomSpot(), S.moveTimePhase2);
                yield return FanAttack();
                yield return new WaitForSeconds(S.pauseAfterFanPhase2);
            }
        }
    }

    Vector2 RandomSpot()
    {
        float x = Random.Range(arenaLeft + S.arenaMargin.x, arenaRight - S.arenaMargin.y);
        float y = floorY + Random.Range(S.hoverHeight.x, S.hoverHeight.y);
        return new Vector2(x, y);
    }

    // Плавный перелёт в точку (SmoothStep).
    IEnumerator MoveTo(Vector2 target, float time)
    {
        Vector2 start = rb.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / time)
        {
            rb.MovePosition(Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)));
            sr.flipX = hero.position.x > transform.position.x;
            yield return null;
        }
        rb.MovePosition(target);
    }

    // Веер сфер в сторону героя (Instantiate по кругу с шагом угла).
    IEnumerator FanAttack()
    {
        anim.SetTrigger("Attack");
        GameAudio.Play(windUpClip, 0.8f);         // Звук замаха: слышно до того, как полетят сферы.
        yield return new WaitForSeconds(S.fanWindUp); // Замах: атаку видно и слышно заранее.

        int count = Phase2 ? S.fanCountPhase2 : S.fanCount;
        float spread = Phase2 ? S.fanSpreadPhase2 : S.fanSpread;
        int waves = Phase2 ? S.fanWavesPhase2 : S.fanWaves;

        for (int w = 0; w < waves; w++)
        {
            Vector2 toPlayer = (Vector2)hero.position + Vector2.up * 0.6f - (Vector2)transform.position;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float a = baseAngle - spread / 2f + spread * i / (count - 1);
                Instantiate(orbPrefab, transform.position, Quaternion.Euler(0f, 0f, a));
            }
            GameAudio.Play(shotClip);
            yield return new WaitForSeconds(S.fanWaveGap);
        }
    }

    // Огненный дождь: сначала красные колонны «!», потом падают шары.
    IEnumerator RainAttack()
    {
        int columns = Phase2 ? S.rainColumnsPhase2 : S.rainColumns;
        float[] xs = new float[columns];
        xs[0] = hero.position.x; // Одна колонна всегда над героем.
        for (int i = 1; i < columns; i++)
            xs[i] = Random.Range(arenaLeft + 1f, arenaRight - 1f);

        GameObject[] warnings = new GameObject[columns];
        for (int i = 0; i < columns; i++)
            warnings[i] = Instantiate(warningPrefab, new Vector3(xs[i], floorY, 0f), Quaternion.identity);
        GameAudio.Play(warningClip);

        yield return new WaitForSeconds(Phase2 ? S.rainWarnTimePhase2 : S.rainWarnTime); // Время, чтобы уйти.

        for (int i = 0; i < columns; i++)
        {
            Destroy(warnings[i]);
            // Поворот -90°: transform.right смотрит вниз, снаряд летит вниз.
            Instantiate(fireballPrefab, new Vector3(xs[i], ceilingY, 0f), Quaternion.Euler(0f, 0f, -90f));
            Instantiate(fireballPrefab, new Vector3(xs[i], ceilingY - 1.5f, 0f), Quaternion.Euler(0f, 0f, -90f));
        }
        GameAudio.Play(shotClip);
    }

    public override void TakeHit(int dmg)
    {
        base.TakeHit(dmg);
        if (HUD.Instance != null)
            HUD.Instance.ShowBoss(Mathf.Max(hp, 0), maxHp);
    }

    // Босс не исчезает сразу: проигрываем смерть и переходим на экран победы.
    protected override void Die()
    {
        dead = true;
        GameScore.Add(stats.scoreValue);
        StopAllCoroutines();
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Убираем все снаряды босса.
        foreach (EnemyProjectile p in FindObjectsByType<EnemyProjectile>())
            Destroy(p.gameObject);

        GameAudio.Play(roarClip);
        CameraShake.Shake(1.2f, 0.3f);
        Vector3 startScale = transform.localScale;
        for (int i = 0; i < 10; i++)
        {
            if (deathFx != null)
                Instantiate(deathFx, transform.position + (Vector3)Random.insideUnitCircle * 1.5f, Quaternion.identity);
            sr.color = i % 2 == 0 ? Color.white : new Color(1f, 0.4f, 0.4f);
            transform.localScale = startScale * (1f - i * 0.08f);
            GameAudio.Play(dieClip, 0.6f);
            yield return new WaitForSeconds(0.15f);
        }
        sr.enabled = false;
        yield return new WaitForSeconds(0.8f);
        GameSession.CompleteLevel(); // Сохраняем прогресс и рекорд, дальше — экран победы.
    }
}
