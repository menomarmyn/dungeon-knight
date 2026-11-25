using UnityEngine;
using UnityEngine.UI;

// Кнопки главного меню и экрана победы (подключены к Button.onClick).
// Неделя 11: выбор уровня (замки из save.json), рекорд, сброс прогресса с подтверждением.
public class MenuButtons : MonoBehaviour
{
    public AudioClip clickClip;
    public AudioClip startClip;                 // Звук при открытии сцены (например, фанфары победы).
    public TMPro.TextMeshProUGUI scoreText;     // На экране победы — итоговый счёт.
    public TMPro.TextMeshProUGUI bestText;      // Рекорд из save.json.
    public GameObject settingsPanel;            // Окно Settings: ползунки Music и SFX, сброс прогресса.

    [Header("Level select")]
    public GameObject levelSelectPanel;
    public Button[] levelButtons;               // По кнопке на каждый LevelData из LevelCatalog.
    public TMPro.TextMeshProUGUI[] levelLabels;

    [Header("Reset progress")]
    public GameObject confirmPanel;             // «RESET ALL PROGRESS?» YES / NO

    void Start()
    {
        Time.timeScale = 1f;
        GameAudio.Play(startClip, 1f, false); // Мелодию не сдвигаем по pitch.

        // Экран победы: финальный счёт идёт в рекорд и сразу на диск.
        if (scoreText != null)
        {
            SaveSystem.ReportScore(GameScore.Score);
            SaveSystem.Save();
            scoreText.text = "SCORE " + GameScore.Score;
        }
        ShowBest();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    void ShowBest()
    {
        if (bestText != null)
            bestText.text = "BEST " + SaveSystem.Data.bestScore;
    }

    // --- выбор уровня
    public void Play()
    {
        GameAudio.PlayUI(clickClip);
        RefreshLevels();
        levelSelectPanel.SetActive(true);
    }

    public void CloseLevelSelect()
    {
        GameAudio.PlayUI(clickClip);
        levelSelectPanel.SetActive(false);
    }

    // Закрытые уровни — серые и не нажимаются; пройденные — зелёные.
    void RefreshLevels()
    {
        LevelData[] levels = LevelCatalog.Instance.levels;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool exists = i < levels.Length;
            levelButtons[i].gameObject.SetActive(exists);
            if (!exists)
                continue;
            int number = i + 1;
            bool open = SaveSystem.IsUnlocked(number);
            levelButtons[i].interactable = open;
            levelLabels[i].text = open ? levels[i].title : "LEVEL " + number + " - LOCKED";
            levelLabels[i].color = !open ? new Color(0.55f, 0.55f, 0.6f)
                : SaveSystem.IsCompleted(number) ? new Color(0.6f, 1f, 0.5f) : Color.white;
        }
    }

    public void SelectLevel(int index)
    {
        if (!SaveSystem.IsUnlocked(index + 1))
            return;
        GameAudio.PlayUI(clickClip);
        GameSession.StartLevel(index);
    }

    // --- настройки
    public void OpenSettings()
    {
        GameAudio.PlayUI(clickClip);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        GameAudio.PlayUI(clickClip);
        settingsPanel.SetActive(false); // VolumeSlider.OnDisable -> save.json.
    }

    // --- сброс прогресса: сначала спрашиваем
    public void AskReset()
    {
        GameAudio.PlayUI(clickClip);
        confirmPanel.SetActive(true);
    }

    public void ConfirmReset()
    {
        GameAudio.PlayUI(clickClip);
        SaveSystem.ResetProgress();
        confirmPanel.SetActive(false);
        ShowBest();
        Debug.Log("[Save] Progress reset");
    }

    public void CancelReset()
    {
        GameAudio.PlayUI(clickClip);
        confirmPanel.SetActive(false);
    }

    public void BackToMenu()
    {
        GameAudio.PlayUI(clickClip);
        SceneFader.Load(GameSession.MenuScene);
    }

    public void Quit()
    {
        GameAudio.PlayUI(clickClip);
        SaveSystem.Save();
        Debug.Log("Quit");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
