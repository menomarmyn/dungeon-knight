using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Переходы между сценами с затемнением. Внутри — SceneManager.LoadScene.
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    public Image black;
    public float fadeTime = 0.4f;

    private bool loading;

    void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        // Сцена открылась — плавно убираем чёрный экран.
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / fadeTime)
        {
            SetAlpha(t);
            yield return null;
        }
        SetAlpha(0f);
    }

    public static void Load(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.FadeAndLoad(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        if (loading)
            yield break;
        loading = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
        {
            SetAlpha(t);
            yield return null;
        }
        SetAlpha(1f);
        SceneManager.LoadScene(sceneName);
    }

    void SetAlpha(float a)
    {
        if (black != null)
            black.color = new Color(0.05f, 0.04f, 0.09f, a);
    }
}
