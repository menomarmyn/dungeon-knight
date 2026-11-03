using UnityEngine;

// Статичная опасность: шипы ранят героя, пока он их касается.
public class Spikes : MonoBehaviour
{
    public int damage = 1;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        Health h = other.GetComponent<Health>();
        if (h != null)
            h.TakeDamage(damage, (Vector2)other.transform.position + Vector2.down); // Подбрасывает вверх.
    }
}
