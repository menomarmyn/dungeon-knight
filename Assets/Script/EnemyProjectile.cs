using UnityEngine;

// Снаряд босса (огненный шар или сфера). Наследуется от Projectile.
public class EnemyProjectile : Projectile
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Health h = other.GetComponent<Health>();
            if (h != null)
                h.TakeDamage(damage, transform.position);
            Impact();
            return;
        }

        if (IsSolid(other) && other.GetComponentInParent<Enemy>() == null)
            Impact();
    }
}
