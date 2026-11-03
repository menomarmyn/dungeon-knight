using UnityEngine;

// Базовый класс для всех снарядов (наследование): пуля героя и снаряды босса.
// На префабе нужны Rigidbody2D без гравитации и триггер-коллайдер.
public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 3f;
    public int damage = 1;
    public GameObject hitFx;

    protected virtual void Start()
    {
        // Летим туда, куда смотрит снаряд (ось X объекта).
        GetComponent<Rigidbody2D>().linearVelocity = transform.right * speed;
        Destroy(gameObject, lifetime);
    }

    // Каждый наследник сам решает, в кого он попадает.
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
    }

    // Общая часть: эффект попадания и удаление снаряда.
    protected void Impact()
    {
        if (hitFx != null)
            Instantiate(hitFx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    // Стены и пол — это не триггеры. Односторонние уступы пропускаем.
    protected static bool IsSolid(Collider2D other)
    {
        return !other.isTrigger && !other.usedByEffector && !other.CompareTag("Player");
    }
}
