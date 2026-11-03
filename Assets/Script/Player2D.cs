using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

// Герой: бег, прыжок, сбор кристаллов. Управление через Input System (PlayerInput).
// Джойстик на экране — это виртуальный геймпад, поэтому он работает через те же Actions, что и клавиатура.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
public class Player2D : MonoBehaviour
{
    public float speed = 7f;
    public float jumpForce = 15f;
    public float coyoteTime = 0.1f;      // Можно прыгнуть чуть позже, чем сошёл с края.
    public float jumpBuffer = 0.12f;     // Нажатие прыжка чуть раньше приземления тоже считается.
    public LayerMask groundMask;         // Слой Ground: тайлы, уступы, движущиеся платформы.
    public float deathY = -4f;           // Ниже этой высоты — падение в яму.
    public AudioClip jumpClip;

    public int Crystals { get; private set; }

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Collider2D col;
    private Health health;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    private bool onGround;
    private float lastGroundTime = -10f;
    private float lastJumpPress = -10f;
    private float knockbackUntil;
    private MovingPlatform platform;     // Платформа под ногами (если есть).

    public bool FacingLeft => sr.flipX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        health = GetComponent<Health>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        // Берём действия из Actions asset, который назначен в PlayerInput.
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];

        // Привязываем к игроку все устройства, включая виртуальный геймпад экранного джойстика.
        foreach (InputDevice device in InputSystem.devices)
            PairDevice(device);
        InputSystem.onDeviceChange += OnDeviceChange;

        if (HUD.Instance != null)
            HUD.Instance.SetCrystals(Crystals, ExitPortal.Required);
    }

    void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added)
            PairDevice(device);
    }

    void PairDevice(InputDevice device)
    {
        if (playerInput.user.valid && !System.Linq.Enumerable.Contains(playerInput.user.pairedDevices, device))
            InputUser.PerformPairingWithDevice(device, playerInput.user);
    }

    void Update()
    {
        if (PauseMenu.IsPaused)
            return; // На паузе не читаем управление.

        float x = moveAction.ReadValue<Vector2>().x;

        // Запоминаем момент нажатия прыжка (буфер прыжка).
        if (jumpAction.WasPressedThisFrame())
            lastJumpPress = Time.time;

        bool canJump = Time.time - lastGroundTime <= coyoteTime;
        if (Time.time - lastJumpPress <= jumpBuffer && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            lastJumpPress = -10f;
            lastGroundTime = -10f;
            onGround = false;
            GameAudio.Play(jumpClip);
        }

        // Отпустил кнопку раньше — прыжок ниже (управляемая высота прыжка).
        if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        // Передаём параметры аниматору.
        anim.SetFloat("Speed", Mathf.Abs(x));
        anim.SetBool("Grounded", onGround);
        anim.SetFloat("VelY", rb.linearVelocity.y);

        // Поворачиваем героя по направлению движения (flipX).
        if (x > 0.1f)
            sr.flipX = false;
        else if (x < -0.1f)
            sr.flipX = true;

        // Падение в яму = смерть и перезапуск уровня.
        if (transform.position.y < deathY)
        {
            Debug.Log("Fell into a pit at x=" + transform.position.x.ToString("0.0"));
            health.GameOver();
        }
    }

    void FixedUpdate()
    {
        CheckGround();

        if (Time.time < knockbackUntil)
            return; // Во время отбрасывания не управляем скоростью.

        float x = moveAction.ReadValue<Vector2>().x;
        Vector2 v = new Vector2(x * speed, rb.linearVelocity.y);

        // Стоим на движущейся платформе — едем вместе с ней.
        if (platform != null)
        {
            v.x += platform.Velocity.x;
            if (platform.Velocity.y < 0f && v.y <= 0.01f)
                v.y = platform.Velocity.y;
        }
        rb.linearVelocity = v;
    }

    // Проверяем землю небольшим прямоугольником под ногами.
    void CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 feet = new Vector2(b.center.x, b.min.y);
        Collider2D hit = Physics2D.OverlapBox(feet, new Vector2(b.size.x * 0.9f, 0.12f), 0f, groundMask);

        onGround = hit != null && rb.linearVelocity.y <= 0.1f;
        platform = onGround ? hit.GetComponent<MovingPlatform>() : null;
        if (onGround)
            lastGroundTime = Time.time;
    }

    // Отбрасывание при получении урона (вызывает Health).
    public void Knockback(Vector2 from)
    {
        float dir = transform.position.x < from.x ? -1f : 1f;
        rb.linearVelocity = new Vector2(dir * 6f, 8f);
        knockbackUntil = Time.time + 0.2f;
    }

    public void AddCrystal()
    {
        Crystals++;
        if (HUD.Instance != null)
            HUD.Instance.SetCrystals(Crystals, ExitPortal.Required);
    }

    // Отключаем управление (смерть, переход на другой уровень).
    public void Freeze()
    {
        enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        Shooter s = GetComponent<Shooter>();
        if (s != null)
            s.enabled = false;
    }
}
