using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Музыка всей игры: один объект на все сцены (DontDestroyOnLoad), два источника и кроссфейд 1 с.
// Если тот же трек уже играет (Restart, Level1 -> Level2), музыка не начинается заново.
// Здесь же: микшер GameMix, сохранённая громкость при запуске и snapshot паузы.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    // Микшер лежит в Assets/Midterm/Resources/GameMix.mixer и грузится по имени.
    static AudioMixer mixer;
    public static AudioMixer Mixer
    {
        get
        {
            if (mixer == null)
            {
                mixer = Resources.Load<AudioMixer>("GameMix");
                if (mixer == null)
                    Debug.LogWarning("Нет микшера Resources/GameMix — звук играет мимо групп.");
            }
            return mixer;
        }
    }

    // Группа микшера по имени: "Tracks", "SFX", "UI".
    public static AudioMixerGroup Group(string name)
    {
        if (Mixer == null)
            return null;
        foreach (AudioMixerGroup g in Mixer.FindMatchingGroups(name))
            if (g.name == name)
                return g;
        return null;
    }

    public float fadeTime = 1f;

    AudioSource a, b, current;
    AudioMixerSnapshot normalSnapshot, pausedSnapshot;

    // Создаётся сам при первом вызове, поэтому его не нужно ставить в каждую сцену.
    static MusicManager Ensure()
    {
        if (Instance == null)
            new GameObject("MusicManager").AddComponent<MusicManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; } // Копия — уничтожаем.
        Instance = this;
        DontDestroyOnLoad(gameObject); // Работает только у корневого объекта.

        a = MakeSource();
        b = MakeSource();
        current = a;

        if (Mixer != null)
        {
            // Пауза ставит Time.timeScale = 0, поэтому переходы микшера идут по реальному времени.
            Mixer.updateMode = AudioMixerUpdateMode.UnscaledTime;
            normalSnapshot = Mixer.FindSnapshot("Normal");
            pausedSnapshot = Mixer.FindSnapshot("Paused");
        }
    }

    void Start()
    {
        // SetFloat работает только со Start: применяем сохранённую громкость один раз при запуске игры.
        AudioPrefs.ApplyAll();
    }

    // Телефон: приложение свернули или закрыли — громкость уже записана на диск.
    void OnApplicationPause(bool paused)
    {
        if (paused)
            AudioPrefs.Save();
    }

    AudioSource MakeSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.outputAudioMixerGroup = Group("Tracks"); // Tracks — дочерняя группа Music.
        return s;
    }

    public static void Play(AudioClip clip, float level = 0.35f)
    {
        Ensure().PlayTrack(clip, level);
    }

    void PlayTrack(AudioClip clip, float level)
    {
        if (current.clip == clip && current.isPlaying)
            return; // Тот же трек продолжает играть.

        AudioSource from = current;
        AudioSource to = current == a ? b : a;
        to.clip = clip;
        to.volume = 0f;
        to.Play();
        StopAllCoroutines();
        StartCoroutine(Crossfade(from, to, level));
        current = to;
    }

    // Старый трек затихает, новый становится громче — одновременно, за fadeTime секунд.
    IEnumerator Crossfade(AudioSource from, AudioSource to, float level)
    {
        float fromStart = from.volume;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
        {
            from.volume = fromStart * (1f - t);
            to.volume = level * t;
            yield return null;
        }
        from.Stop();
        to.volume = level;
    }

    // Вызывает PauseMenu: в паузе музыка тише и глуше (snapshot Paused: Tracks −6 dB, Lowpass 800 Hz).
    public static void SetPaused(bool paused, float time = 0.3f)
    {
        MusicManager m = Ensure();
        AudioMixerSnapshot s = paused ? m.pausedSnapshot : m.normalSnapshot;
        if (s != null)
            s.TransitionTo(time);
    }
}
