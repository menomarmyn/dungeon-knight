using System.Collections;
using UnityEngine;

// Базовый класс врагов (наследование): здоровье, урон при касании, вспышка и смерть.
// SlimeEnemy, BatEnemy и Boss наследуются от него.
public class Enemy : MonoBehaviour
{
    public int hp = 2;
    public int contactDamage = 1;
    public int scoreValue = 50; // Очки за победу над врагом.
    public GameObject deathFx;
    public AudioClip hitClip;
    public AudioClip dieClip;

    protected SpriteRenderer sr;
    protected Rigidbody2D rb;
    protected bool dead;

    protected virtual void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
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

    // Короткая красная вспышка при попадании.
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
}
