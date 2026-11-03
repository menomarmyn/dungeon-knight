using UnityEngine;

// Базовый класс объектов, падающих сверху (сталактиты из Spawner).
public class Hazard : MonoBehaviour
{
    public float lifetime = 8f;
    public bool breakOnGround = true;  // Разбивается о пол.
    public GameObject breakFx;
    public AudioClip breakClip;

    protected virtual void Start()
    {
        Destroy(gameObject, lifetime); // Объект исчезает после заданного времени.
    }

    void Update()
    {
        if (transform.position.y < -10f)
            Destroy(gameObject); // Удаляем объект, упавший за уровень.
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Player"))
        {
            OnHitPlayer(c.gameObject.GetComponent<Health>());
            Break();
            return;
        }
        if (breakOnGround)
            Break();
    }

    void Break()
    {
        if (breakFx != null)
            Instantiate(breakFx, transform.position, Quaternion.identity);
        GameAudio.Play(breakClip, 0.6f);
        Destroy(gameObject);
    }

    // Дочерний класс может изменить реакцию на касание героя.
    protected virtual void OnHitPlayer(Health player)
    {
        player.TakeDamage(1, transform.position);
    }

    // Реакция на попадание пули.
    public virtual void OnShot()
    {
        Break();
    }
}
