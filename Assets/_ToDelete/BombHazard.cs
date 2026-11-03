using UnityEngine;

// Бомба взрывается по таймеру или после попадания.
public class BombHazard : Hazard
{
    public GameObject explosionPrefab;
    public float fuse = 2f;
    public float radius = 1.6f;

    private bool exploded;

    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Explode), fuse); //таймер взрыва.
    }

    // Бомба наносит герою два очка урона.
    protected override void OnHitPlayer(Health player)
    {
        player.TakeDamage(2);
        Explode();
    }

    // Выстрел вызывает взрыв.
    public override void OnShot()
    {
        Explode();
    }

    void Explode()
    {
        if (exploded)
            return; // Не допускаем повторный взрыв.
        exploded = true;

        // Наносим урон всем объектам со здоровьем рядом.
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Health h = hit.GetComponent<Health>();
            if (h != null)
                h.TakeDamage(2);
        }

        // Создаём эффект взрыва и удаляем его через секунду.
        if (explosionPrefab != null)
            Destroy(Instantiate(explosionPrefab, transform.position, Quaternion.identity), 1f);

        Destroy(gameObject);
    }
}
