using UnityEngine;

// Базовый класс предметов, которые подбирает герой (триггер).
public abstract class Pickup : MonoBehaviour
{
    public AudioClip clip;
    public GameObject fx;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Наследник решает, можно ли подобрать предмет.
        if (!Collect(other.gameObject))
            return;

        GameAudio.Play(clip);
        if (fx != null)
            Instantiate(fx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    protected abstract bool Collect(GameObject player);
}
