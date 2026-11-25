using UnityEngine;

// Кнопки главного меню и экрана победы (подключены к Button.onClick).
public class MenuButtons : MonoBehaviour
{
    public string firstLevel = "Level1";
    public string menuScene = "MainMenu";
    public AudioClip clickClip;
    public AudioClip startClip; // Звук при открытии сцены (например, фанфары победы).
    public TMPro.TextMeshProUGUI scoreText; // На экране победы — итоговый счёт.
    public GameObject settingsPanel;        // Окно Settings: ползунки Music и SFX.

    void Start()
    {
        Time.timeScale = 1f;
        GameAudio.Play(startClip, 1f, false); // Мелодию не сдвигаем по pitch.
        if (scoreText != null)
            scoreText.text = "SCORE " + GameScore.Score;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        GameAudio.PlayUI(clickClip);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        GameAudio.PlayUI(clickClip);
        settingsPanel.SetActive(false); // VolumeSlider.OnDisable сохраняет громкость.
    }

    public void Play()
    {
        GameAudio.PlayUI(clickClip);
        GameScore.ResetAll();
        SceneFader.Load(firstLevel);
    }

    public void BackToMenu()
    {
        GameAudio.PlayUI(clickClip);
        SceneFader.Load(menuScene);
    }

    public void Quit()
    {
        GameAudio.PlayUI(clickClip);
        Debug.Log("Quit");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
