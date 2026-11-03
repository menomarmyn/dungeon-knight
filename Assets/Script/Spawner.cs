using UnityEngine;

// Создаёт опасные объекты над уровнем.
public class Spawner : MonoBehaviour
{
    // Здесь можно указать камни, бомбы и сердца.
    // Повтор объекта увеличивает шанс его появления.
    public Hazard[] prefabs;
    public float firstDelay = 1f;
    public float interval = 2f;
    public float halfWidth = 9f; // Ширина зоны появления объектов.

    void Start()
    {
        // Первый вызов и интервал между повторами.
        InvokeRepeating(nameof(Spawn), firstDelay, interval);
    } 

    void Spawn()
    {
        Hazard pick = prefabs[Random.Range(0, prefabs.Length)];
        float x = transform.position.x + Random.Range(-halfWidth, halfWidth);
        Instantiate(pick, new Vector3(x, transform.position.y, 0f), Quaternion.identity);
    } // Создатель работает с любым наследником Hazard.
}
