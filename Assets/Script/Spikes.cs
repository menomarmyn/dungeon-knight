using UnityEngine;

// Статичная опасность: шипы ранят героя, пока он их касается.
public class Spikes : MonoBehaviour
{
    public ItemValues items;   // Урон шипов — items.spikeDamage.

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        Health h = other.GetComponent<Health>();
        if (h != null)
            h.TakeDamage(items.spikeDamage, (Vector2)other.transform.position + Vector2.down); // Подбрасывает вверх.
    }
}
