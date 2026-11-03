using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Интерфейс на Canvas: сердца, счётчик кристаллов, сообщения, полоска HP босса.
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    public Image[] hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public GameObject crystalGroup;
    public TextMeshProUGUI crystalText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI titleText;
    public GameObject bossGroup;
    public RectTransform bossFill;
    public string levelTitle = "LEVEL 1";
    public bool showCrystals = true; // На уровне босса счётчик не нужен.

    private Coroutine messageRoutine;

    void Awake()
    {
        Instance = this;
        if (bossGroup != null)
            bossGroup.SetActive(false);
        if (messageText != null)
            messageText.text = "";
        if (crystalGroup != null)
            crystalGroup.SetActive(showCrystals);
    }

    void Start()
    {
        StartCoroutine(ShowTitle());
    }

    public void SetHearts(int hp, int maxHp)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].gameObject.SetActive(i < maxHp);
            hearts[i].sprite = i < hp ? heartFull : heartEmpty;
        }
    }

    public void SetCrystals(int have, int need)
    {
        if (crystalText == null)
            return;
        crystalText.text = need > 0 ? have + "/" + need : have.ToString();
        crystalText.color = need > 0 && have >= need ? new Color(0.65f, 1f, 0.55f) : Color.white;
    }

    public void ShowBoss(int hp, int maxHp)
    {
        bossGroup.SetActive(true);
        if (crystalGroup != null)
            crystalGroup.SetActive(false);
        // Полоска: меняем правую границу (anchorMax.x) от 0 до 1.
        bossFill.anchorMax = new Vector2((float)hp / maxHp, 1f);
    }

    public void ShowMessage(string text, float seconds)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.text = text;
        yield return new WaitForSeconds(seconds);
        messageText.text = "";
    }

    // Название уровня появляется в начале и плавно исчезает.
    IEnumerator ShowTitle()
    {
        if (titleText == null)
            yield break;
        titleText.text = levelTitle;
        Color c = titleText.color;
        yield return new WaitForSeconds(1.8f);
        for (float t = 1f; t > 0f; t -= Time.deltaTime * 1.5f)
        {
            titleText.color = new Color(c.r, c.g, c.b, t);
            yield return null;
        }
        titleText.text = "";
    }
}
