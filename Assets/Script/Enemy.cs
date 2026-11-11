using System.Collections;
using UnityEngine;

// Базовый класс врагов (наследование): здоровье, урон при касании, вспышка, смерть
// и общее «зрение» (дистанция + угол + Raycast, стены закрывают обзор).
// SlimeEnemy (патруль), BatEnemy (таран), GoblinArcher (стрелок) и Boss наследуются от него.
public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public int hp = 2;
    public int contactDamage = 1;
    public int scoreValue = 50; // Очки за победу над врагом.
    public GameObject deathFx;
    public AudioClip hitClip;
    public AudioClip dieClip;

    [Header("Sight")]
    public float viewDistance = 6f;            // Дальше этого врага не видит.
    [Range(0f, 360f)] public float viewAngle = 120f; // Ширина конуса зрения (360 = видит во все стороны).
    public LayerMask wallMask;                 // Слой стен и пола: они закрывают обзор.
    public Vector2 eyeOffset = new Vector2(0f, 0.2f);

    [Header("Telegraph")]
    public GameObject alertIcon;               // Значок «!» над головой во время замаха.
    public AudioClip windUpClip;               // Звук замаха.
    public Color windUpColor = new Color(1f, 0.25f, 0.25f);

    protected SpriteRenderer sr;
    protected Rigidbody2D rb;
    protected bool dead;
    private Transform player;

    protected virtual void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (alertIcon != null)
            alertIcon.SetActive(false);
    }

    // Игрока ищем один раз и запоминаем (не GameObject.Find каждый кадр).
    protected Transform Player
    {
        get
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                    player = p.transform;
            }
            return player;
        }
    }

    protected Vector2 Eye => (Vector2)transform.position + eyeOffset;
    protected Vector2 PlayerCenter => (Vector2)Player.position + Vector2.up * 0.6f;

    // Видит ли враг игрока: 1) близко, 2) в конусе зрения, 3) луч до игрока не упирается в стену.
    protected bool CanSeePlayer(Vector2 facing)
    {
        if (Player == null)
            return false;

        Vector2 toPlayer = PlayerCenter - Eye;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance)
            return false;
        if (viewAngle < 360f && Vector2.Angle(facing, toPlayer) > viewAngle / 2f)
            return false;

        RaycastHit2D wall = Physics2D.Raycast(Eye, toPlayer / dist, dist, wallMask);
        bool visible = wall.collider == null;

        // В окне Scene: жёлтый луч — видит, красный — стена мешает.
        Debug.DrawLine(Eye, visible ? PlayerCenter : wall.point, visible ? Color.yellow : Color.red);
        return visible;
    }

    // Честность: не атакуем, если враг за краем экрана.
    protected bool OnScreen()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return true;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        return v.x > 0.02f && v.x < 0.98f && v.y > 0.02f && v.y < 0.98f;
    }

    // Замах перед атакой: мигаем красным и показываем «!».
    protected void BeginWindUp()
    {
        if (alertIcon != null)
            alertIcon.SetActive(true);
        GameAudio.Play(windUpClip, 0.6f);
    }

    protected void UpdateWindUp(float timeLeft)
    {
        sr.color = Mathf.Repeat(timeLeft, 0.14f) < 0.07f ? windUpColor : Color.white;
    }

    protected void EndWindUp()
    {
        if (alertIcon != null)
            alertIcon.SetActive(false);
        sr.color = Color.white;
    }

    // Вызывается пулей героя.
    public virtual void TakeHit(int dmg)
    {
        if (dead)
            return;
        hp -= dmg;
        GameAudio.Play(hitClip, 0.8f);
        StartCoroutine(Flash());
        if (hp <= 0)
            Die();
    }

    // Короткая вспышка при попадании.
    IEnumerator Flash()
    {
        sr.color = new Color(1f, 0.35f, 0.35f);
        yield return new WaitForSeconds(0.08f);
        sr.color = Color.white;
    }

    protected virtual void Die()
    {
        dead = true;
        GameScore.Add(scoreValue);
        if (deathFx != null)
            Instantiate(deathFx, transform.position, Quaternion.identity);
        GameAudio.Play(dieClip);
        Destroy(gameObject);
    }

    // Касание врага ранит героя (у врага триггер-коллайдер).
    void OnTriggerStay2D(Collider2D other)
    {
        if (dead || !other.CompareTag("Player"))
            return;
        Health h = other.GetComponent<Health>();
        if (h != null)
            h.TakeDamage(contactDamage, transform.position);
    }

    // Конус зрения в окне Scene (когда объект выделен).
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
        Vector3 eye = transform.position + (Vector3)eyeOffset;
        Gizmos.DrawWireSphere(eye, viewDistance);
    }
}
