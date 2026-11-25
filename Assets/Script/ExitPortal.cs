using UnityEngine;

// Портал-выход. Открывается, когда собрано нужное число кристаллов (LevelData.requiredCrystals).
public class ExitPortal : MonoBehaviour
{
    public AudioClip openClip;
    public AudioClip lockedClip;
    public AudioClip enterClip;

    public static int Required { get; private set; } // Для HUD: сколько нужно на этом уровне.

    private int requiredCrystals;
    private Animator anim;
    private Player2D player;
    private bool open;
    private bool used;
    private float nextLockedMessage;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // Вызывает LevelLoader сразу после создания портала.
    public void Setup(int required)
    {
        requiredCrystals = required;
        Required = required;
    }

    void Start()
    {
        player = FindAnyObjectByType<Player2D>();
    }

    void Update()
    {
        if (!open && player != null && player.Crystals >= requiredCrystals)
        {
            open = true;
            anim.SetBool("Open", true);
            GameAudio.Play(openClip);
            if (HUD.Instance != null)
                HUD.Instance.ShowMessage("THE PORTAL IS OPEN!", 2f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player"))
            return;

        if (!open)
        {
            if (Time.time > nextLockedMessage && HUD.Instance != null)
            {
                HUD.Instance.ShowMessage("COLLECT " + requiredCrystals + " CRYSTALS!", 2f);
                GameAudio.Play(lockedClip);
                nextLockedMessage = Time.time + 2f;
            }
            return;
        }

        // Уровень пройден: сохраняем прогресс (save.json) и грузим следующий уровень.
        used = true;
        GameAudio.Play(enterClip);
        other.GetComponent<Player2D>().Freeze();
        GameSession.CompleteLevel();
    }
}
