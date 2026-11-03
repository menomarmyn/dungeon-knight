using UnityEngine;

// Звук: фоновая музыка сцены и короткие эффекты (2D, без затухания по расстоянию).
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.35f; // Музыка тише эффектов.
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    private AudioSource sfxSource;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (music != null)
        {
            AudioSource musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    // Статический метод: GameAudio.Play(clip) можно вызвать из любого скрипта.
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (Instance != null && clip != null)
            Instance.sfxSource.PlayOneShot(clip, volume * Instance.sfxVolume);
    }
}
