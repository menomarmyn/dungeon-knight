using System;
using System.IO;
using UnityEngine;

// Сохранение в JSON-файл: Application.persistentDataPath/save.json.
// Правила:
//  - нет файла или он битый -> игра не падает, берём значения по умолчанию (new SaveData());
//  - запись через временный файл: если телефон выключится посреди записи, старый save.json останется целым;
//  - сохраняем после уровня, при закрытии настроек и в OnApplicationPause(true) (MusicManager).
public static class SaveSystem
{
    public const int CurrentVersion = 1;
    const string FileName = "save.json";

    static SaveData data;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // Данные загружаются один раз при первом обращении.
    public static SaveData Data
    {
        get
        {
            if (data == null)
                data = Load();
            return data;
        }
    }

    public static SaveData Load()
    {
        if (!File.Exists(FilePath))
        {
            Debug.Log("[Save] No save file yet, starting with defaults: " + FilePath);
            return new SaveData();
        }
        try
        {
            string json = File.ReadAllText(FilePath);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // Битый JSON -> исключение.
            if (loaded == null)
                throw new Exception("file is empty");
            Debug.Log("[Save] Loaded " + FilePath);
            return Validate(loaded);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Save] save.json is broken (" + e.Message + "). Starting with defaults.");
            try { File.Copy(FilePath, FilePath + ".broken", true); } catch (Exception) { } // Оставляем копию для разбора.
            return new SaveData();
        }
    }

    // Файл мог быть отредактирован руками: чиним то, что можно починить.
    static SaveData Validate(SaveData d)
    {
        var defaults = new SaveData();
        if (d.unlockedLevels == null || d.unlockedLevels.Count == 0)
            d.unlockedLevels = defaults.unlockedLevels;
        if (!d.unlockedLevels.Contains(1))
            d.unlockedLevels.Add(1);
        if (d.completedLevels == null)
            d.completedLevels = defaults.completedLevels;
        if (d.bestScore < 0)
            d.bestScore = 0;
        d.musicVolume = Mathf.Clamp(d.musicVolume, AudioPrefs.Min, 1f);
        d.sfxVolume = Mathf.Clamp(d.sfxVolume, AudioPrefs.Min, 1f);
        d.version = CurrentVersion;
        return d;
    }

    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, FilePath, true);
            File.Delete(tmp);
            Debug.Log("[Save] Saved " + FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("[Save] Could not save: " + e.Message);
        }
    }

    public static bool IsUnlocked(int levelNumber) => Data.unlockedLevels.Contains(levelNumber);
    public static bool IsCompleted(int levelNumber) => Data.completedLevels.Contains(levelNumber);

    // Уровень пройден: отмечаем, открываем следующий, обновляем рекорд и сразу пишем на диск.
    public static void CompleteLevel(int levelNumber, int score)
    {
        if (!Data.completedLevels.Contains(levelNumber))
            Data.completedLevels.Add(levelNumber);
        if (!Data.unlockedLevels.Contains(levelNumber + 1))
            Data.unlockedLevels.Add(levelNumber + 1);
        ReportScore(score);
        Save();
    }

    public static void ReportScore(int score)
    {
        if (score > Data.bestScore)
            Data.bestScore = score;
    }

    // Кнопка RESET PROGRESS: прогресс и рекорд сбрасываются, громкость остаётся.
    public static void ResetProgress()
    {
        var fresh = new SaveData { musicVolume = Data.musicVolume, sfxVolume = Data.sfxVolume };
        data = fresh;
        Save();
    }
}
