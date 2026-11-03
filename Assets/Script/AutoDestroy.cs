using UnityEngine;

// Эффекты (дым, искры, предупреждения) удаляют себя сами.
public class AutoDestroy : MonoBehaviour
{
    public float time = 0.5f;

    void Start()
    {
        Destroy(gameObject, time);
    }
}
