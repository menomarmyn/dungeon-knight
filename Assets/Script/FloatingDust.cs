using UnityEngine;

// Атмосфера подземелья: пылинки медленно плывут вверх (или конфетти падает вниз на экране победы).
public class FloatingDust : MonoBehaviour
{
    public Sprite sprite;
    public int count = 40;
    public Vector2 area = new Vector2(30f, 16f);
    public Vector2 velocity = new Vector2(0.2f, 0.4f);
    public Color[] colors = { new Color(1f, 0.85f, 0.6f, 0.35f) };
    public float scale = 1f;
    public bool spin;
    public bool followCamera = true;

    private Transform[] motes;
    private Vector2[] speeds;
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
        motes = new Transform[count];
        speeds = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            GameObject g = new GameObject("Mote");
            g.transform.SetParent(transform);
            SpriteRenderer r = g.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = colors[Random.Range(0, colors.Length)];
            r.sortingOrder = 40;
            g.transform.localScale = Vector3.one * scale * Random.Range(0.6f, 1.2f);
            g.transform.position = Center() + new Vector3(Random.Range(-area.x, area.x) / 2f, Random.Range(-area.y, area.y) / 2f, 0f);
            motes[i] = g.transform;
            speeds[i] = new Vector2(velocity.x * Random.Range(-1f, 1f), velocity.y * Random.Range(0.5f, 1.5f));
        }
    }

    Vector3 Center()
    {
        return followCamera ? new Vector3(cam.position.x, cam.position.y, 0f) : transform.position;
    }

    void Update()
    {
        Vector3 c = Center();
        for (int i = 0; i < motes.Length; i++)
        {
            Transform m = motes[i];
            m.position += (Vector3)speeds[i] * Time.deltaTime;
            if (spin)
                m.Rotate(0f, 0f, 180f * Time.deltaTime);
            Vector3 d = m.position - c;
            // Вышла за край — переносим на другую сторону.
            if (d.y > area.y / 2f) m.position -= new Vector3(0f, area.y, 0f);
            if (d.y < -area.y / 2f) m.position += new Vector3(0f, area.y, 0f);
            if (d.x > area.x / 2f) m.position -= new Vector3(area.x, 0f, 0f);
            if (d.x < -area.x / 2f) m.position += new Vector3(area.x, 0f, 0f);
        }
    }
}
