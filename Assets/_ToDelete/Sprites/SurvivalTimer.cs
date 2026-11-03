using UnityEngine;
using TMPro;

// Открывает ворота, если герой продержался нужное время.
public class SurvivalTimer : MonoBehaviour
{
    public float goal = 60f;
    public Animator gateAnimator; // Аниматор ворот.
    public Spawner spawner;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI messageText;

    private float t;
    private bool won;

    void Update()
    {
        t += Time.deltaTime; // добавляем время

        if (timerText != null)
            timerText.text = "Время: " + Mathf.FloorToInt(Mathf.Min(t, goal)) + " / " + goal;

        if (!won && t >= goal) // чекает время выжмвания
        {
            won = true;
            Debug.Log("YOU WIN!");
            gateAnimator.SetBool("Open", true); 
            spawner.CancelInvoke();
            if (messageText != null)
                messageText.text = "Выжил! Ворота открыты — беги к выходу";
        }
    }
}
