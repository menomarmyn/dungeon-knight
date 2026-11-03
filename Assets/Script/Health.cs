using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Здоровье героя, временная защита (мигание) и перезапуск уровня.
public class Health : MonoBehaviour
{
    public int hp = 3;
    public int maxHp = 3;
    public float invincibleTime = 1.2f;
    public AudioClip hurtClip;
    public AudioClip healClip;
    public GameObject deathFx;

    private bool invincible;
    private bool dead;
    private SpriteRenderer sr;
    private Player2D player;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GetComponent<Player2D>();
    }

    void Start()
    {
        ShowHp();
    }

    // Старый вариант (камни из Spawner вызывают его без точки удара).
    public void TakeDamage(int dmg)
    {
        TakeDamage(dmg, transform.position + Vector3.down);
    }

    public void TakeDamage(int dmg, Vector2 from)
    {
        if (invincible || dead)
            return; // Во время защиты урон не проходит.

        hp -= dmg;
        Debug.Log("Damage " + dmg + " from " + from + ", HP: " + hp);
        ShowHp();
        GameAudio.Play(hurtClip);
        CameraShake.Shake(0.15f, 0.15f);

        if (hp <= 0)
        {
            GameOver();
            return;
        }

        if (player != null)
            player.Knockback(from);
        StartCoroutine(Invincibility());
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(hp + amount, maxHp);
        ShowHp();
        GameAudio.Play(healClip);
    }

    // Корутина ждёт, не останавливая игру.
    IEnumerator Invincibility()
    {
        invincible = true;
        float end = Time.time + invincibleTime;
        while (Time.time < end)
        {
            sr.enabled = !sr.enabled; // Включаем и выключаем спрайт для мигания.
            yield return new WaitForSeconds(0.1f);
        }
        sr.enabled = true;
        invincible = false;
    }

    public void GameOver()
    {
        if (dead)
            return;
        dead = true;
        hp = 0;
        ShowHp();
        StartCoroutine(DieAndRestart());
    }

    IEnumerator DieAndRestart()
    {
        if (player != null)
            player.Freeze();
        if (deathFx != null)
            Instantiate(deathFx, transform.position + Vector3.up * 0.6f, Quaternion.identity);
        sr.enabled = false;
        GameAudio.Play(hurtClip);
        yield return new WaitForSeconds(0.8f);
        // Загружаем текущую сцену заново.
        SceneFader.Load(SceneManager.GetActiveScene().name);
    }

    void ShowHp()
    {
        if (HUD.Instance != null)
            HUD.Instance.SetHearts(hp, maxHp);
    }
}
