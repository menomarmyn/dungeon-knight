using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Пауза: кнопка «II» в HUD (или Esc / P на клавиатуре) открывает панель и останавливает время.
// Time.timeScale = 0 — физика, анимации и движение в Update (через deltaTime) замирают.
public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public string menuScene = "MainMenu";
    public AudioClip clickClip;

    public static bool IsPaused { get; private set; }

    void Start()
    {
        SetPaused(false);
    }

    void Update()
    {
        // Клавиатура для отладки на компьютере.
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
            SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        pausePanel.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    // Кнопки (подключены к Button.onClick).
    public void Pause()
    {
        GameAudio.Play(clickClip);
        SetPaused(true);
    }

    public void Resume()
    {
        GameAudio.Play(clickClip);
        SetPaused(false);
    }

    public void Restart()
    {
        GameAudio.Play(clickClip);
        pausePanel.SetActive(false);
        GameScore.ResetToLevelStart();
        SceneFader.Load(SceneManager.GetActiveScene().name); // Время вернётся в 1 перед загрузкой.
    }

    public void ToMenu()
    {
        GameAudio.Play(clickClip);
        pausePanel.SetActive(false);
        SceneFader.Load(menuScene);
    }

    void OnDestroy()
    {
        // Если сцена выгружается во время паузы — не оставляем игру замороженной.
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
