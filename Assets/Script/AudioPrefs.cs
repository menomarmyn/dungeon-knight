using UnityEngine;

// Громкость музыки и эффектов: перевод ползунка 0..1 в децибелы и сохранение между запусками.
// Сохраняем через PlayerPrefs (три числа — этого хватает). Имена ключей = имена
// exposed-параметров микшера: MusicVol и SFXVol.
public static class AudioPrefs
{
    public const string Music = "MusicVol";
    public const string Sfx = "SFXVol";
    public const float Default = 0.8f;

    // Ухо слышит громкость в отношениях, поэтому Log10: 1 -> 0 dB, 0.5 -> −6 dB, 0.0001 -> −80 dB.
    public static float ToDb(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f; // Log10(0) = минус бесконечность.
    }

    public static float Get(string param)
    {
        return PlayerPrefs.GetFloat(param, Default);
    }

    public static void Set(string param, float value)
    {
        if (MusicManager.Mixer != null)
            MusicManager.Mixer.SetFloat(param, ToDb(value));
        PlayerPrefs.SetFloat(param, value);
    }

    // Записать на диск сразу (на случай, если приложение закроют, не выходя в меню).
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    // При запуске игры: применить сохранённые значения к микшеру.
    public static void ApplyAll()
    {
        Set(Music, Get(Music));
        Set(Sfx, Get(Sfx));
    }
}
