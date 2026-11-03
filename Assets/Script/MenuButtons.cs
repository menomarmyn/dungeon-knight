using UnityEngine;

// Кнопки главного меню и экрана победы (подключены к Button.onClick).
public class MenuButtons : MonoBehaviour
{
    public string firstLevel = "Level1";
    public string menuScene = "MainMenu";
    public AudioClip clickClip;
    public AudioClip startClip; // Звук при открытии сцены (например, фанфары победы).

    void Start()
    {
        GameAudio.Play(startClip);
    }

    public void Play()
    {
        GameAudio.Play(clickClip);
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
