using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Короткие звуки игры. Любой скрипт вызывает GameAudio.Play(clip) — так было и раньше.
// Неделя 12:
//  - все источники идут в группы микшера GameMix (SFX или UI);
//  - у повторяющихся звуков случайный pitch 0.92–1.08 и 2–3 версии клипа (без повтора подряд);
//  - музыку сцены включает MusicManager (один на всю игру, с кроссфейдом).
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    public AudioClip music;                          // Трек этой сцены (меню / уровень / босс).
    [Range(0f, 1f)] public float musicVolume = 0.35f; // Громкость трека под эффектами.
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    [System.Serializable]
    public class Variant
    {
        public AudioClip main;    // Клип, который передают в Play().
        public AudioClip[] alts;  // Его другие версии.
    }
    public Variant[] variants;    // Заполняет сборщик: sfx_hit -> sfx_hit_2, sfx_hit_3 ...

    const int Voices = 8;         // Пул источников: несколько звуков сразу, у каждого свой pitch.
    AudioSource[] sfx;
    AudioSource ui;
    int next;
    readonly Dictionary<AudioClip, AudioClip[]> bank = new Dictionary<AudioClip, AudioClip[]>();
    readonly Dictionary<AudioClip, AudioClip> lastPlayed = new Dictionary<AudioClip, AudioClip>();

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        AudioMixerGroup sfxGroup = MusicManager.Group("SFX");
        AudioMixerGroup uiGroup = MusicManager.Group("UI");

        // Источники создаются один раз при старте сцены, а не на каждый выстрел.
        sfx = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
            sfx[i] = MakeSource(sfxGroup);
        ui = MakeSource(uiGroup != null ? uiGroup : sfxGroup);

        if (variants != null)
            foreach (Variant v in variants)
                if (v.main != null && v.alts != null && v.alts.Length > 0)
                {
                    var all = new List<AudioClip> { v.main };
                    all.AddRange(v.alts);
                    bank[v.main] = all.ToArray();
                }

        if (music != null)
            MusicManager.Play(music, musicVolume);
    }

    AudioSource MakeSource(AudioMixerGroup group)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;           // 2D-игра: одна громкость везде.
        s.outputAudioMixerGroup = group;
        return s;
    }

    // Эффект в группу SFX. vary = false — без случайного pitch (например, мелодия победы).
    public static void Play(AudioClip clip, float volume = 1f, bool vary = true)
    {
        if (Instance == null || clip == null)
            return;
        Instance.PlaySfx(clip, volume, vary);
    }

    // Клик интерфейса в группу UI.
    public static void PlayUI(AudioClip clip)
    {
        if (Instance != null && clip != null)
            Instance.ui.PlayOneShot(clip, Instance.sfxVolume);
    }

    void PlaySfx(AudioClip clip, float volume, bool vary)
    {
        AudioClip pick = vary ? PickVersion(clip) : clip;
        AudioSource s = sfx[next];
        next = (next + 1) % Voices;
        s.pitch = vary ? Random.Range(0.92f, 1.08f) : 1f; // ±8%: нет эффекта «пулемёта».
        s.PlayOneShot(pick, volume * sfxVolume);
    }

    // Случайная версия клипа, но не та же, что в прошлый раз.
    AudioClip PickVersion(AudioClip clip)
    {
        if (!bank.TryGetValue(clip, out AudioClip[] all))
            return clip;
        lastPlayed.TryGetValue(clip, out AudioClip last);
        AudioClip pick = all[Random.Range(0, all.Length)];
        if (pick == last)
            pick = all[(System.Array.IndexOf(all, pick) + 1) % all.Length];
        lastPlayed[clip] = pick;
        return pick;
    }
}
