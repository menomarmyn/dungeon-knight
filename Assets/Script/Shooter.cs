using UnityEngine;
using UnityEngine.InputSystem;

// Создаёт пули с задержкой между выстрелами (cooldown). Кнопка Fire из Input System.
public class Shooter : MonoBehaviour
{
    public Bullet bulletPrefab; // Префаб пули.
    public float cooldown = 0.25f;
    public AudioClip shootClip;
    public Vector2 muzzleOffset = new Vector2(0.7f, 0.62f);

    private float nextShot; // Время следующего доступного выстрела.
    private SpriteRenderer sr;
    private InputAction fireAction;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        fireAction = GetComponent<PlayerInput>().actions["Fire"];
    }

    void Update()
    {
        // Держим кнопку — стреляем очередью, но не чаще cooldown.
        if (fireAction.IsPressed() && !PauseMenu.IsPaused)
            Shoot();
    }

    public void Shoot()
    {
        if (Time.time < nextShot)
            return; // Выстрел пока недоступен.
        nextShot = Time.time + cooldown;

        // При взгляде влево разворачиваем пулю.
        Quaternion turn = sr.flipX ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
        Vector3 muzzle = transform.position + new Vector3(sr.flipX ? -muzzleOffset.x : muzzleOffset.x, muzzleOffset.y, 0f);

        Instantiate(bulletPrefab, muzzle, turn);
        GameAudio.Play(shootClip, 0.7f);
    }
}
