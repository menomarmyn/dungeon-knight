using UnityEngine;

// Громкость музыки и эффектов: перевод ползунка 0..1 в децибелы.
// Неделя 11: значения хранятся в save.json (SaveSystem), а не в PlayerPrefs.
// Имена = exposed-параметры микшера: MusicVol и SFXVol.
public static class AudioPrefs
{
    public const string Music = "MusicVol";
    public const string Sfx = "SFXVol";
    public const float Min = 0.0001f; // Log10(0) = минус бесконечность, поэтому не ниже 0.0001 (= −80 dB).

    // Ухо слышит громкость в отношениях, поэтому Log10: 1 -> 0 dB, 0.5 -> −6 dB, 0.0001 -> −80 dB.
    public static float ToDb(float value)
    {
        return Mathf.Log10(Mathf.Max(value, Min)) * 20f;
    }

    public static float Get(string param)
    {
        return param == Music ? SaveSystem.Data.musicVolume : SaveSystem.Data.sfxVolume;
    }

    public static void Set(string param, float value)
    {
        if (MusicManager.Mixer != null)
            MusicManager.Mixer.SetFloat(param, ToDb(value));
        if (param == Music)
            SaveSystem.Data.musicVolume = value;
        else
            SaveSystem.Data.sfxVolume = value;
    }

    // Настройки изменились (закрыли окно Settings) — пишем save.json.
    public static void Save()
    {
        SaveSystem.Save();
    }

    // При запуске игры: применить сохранённые значения к микшеру.
    public static void ApplyAll()
    {
        Set(Music, Get(Music));
        Set(Sfx, Get(Sfx));
    }
}
