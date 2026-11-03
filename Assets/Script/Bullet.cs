using UnityEngine;

// Пуля героя. Наследуется от Projectile.
public class Bullet : Projectile
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // Попали во врага (слизень, мышь или босс) — GetComponent находит базовый класс Enemy.
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeHit(damage);
            Impact();
            return;
        }

        // Падающий камень можно сбить выстрелом.
        Hazard hazard = other.GetComponent<Hazard>();
        if (hazard != null)
        {
            hazard.OnShot();
            Impact();
            return;
        }

        // Пуля исчезает при столкновении со стеной или полом.
        if (IsSolid(other))
            Impact();
    }
}
