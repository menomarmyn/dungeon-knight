using UnityEngine;

// Кнопки главного меню и экрана победы (подключены к Button.onClick).
public class MenuButtons : MonoBehaviour
{
    public string firstLevel = "Level1";
    public string menuScene = "MainMenu";
    public AudioClip clickClip;
    public AudioClip startClip; // Звук при открытии сцены (например, фанфары победы).
    public TMPro.TextMeshProUGUI scoreText; // На экране победы — итоговый счёт.

    void Start()
    {
        Time.timeScale = 1f;
        GameAudio.Play(startClip);
        if (scoreText != null)
            scoreText.text = "SCORE " + GameScore.Score;
    }

    public void Play()
    {
        GameAudio.Play(clickClip);
        GameScore.ResetAll();
        SceneFader.Load(firstLevel);
    }

    public void BackToMenu()
    {
        GameAudio.Play(clickClip);
        SceneFader.Load(menuScene);
    }

    public void Quit()
    {
        GameAudio.Play(clickClip);
        Debug.Log("Quit");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
