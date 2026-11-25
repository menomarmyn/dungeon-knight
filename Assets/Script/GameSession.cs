using UnityEngine;

// Какой уровень сейчас играется и куда идти дальше. Живёт между сценами (static).
// Все уровни играются в одной сцене Game: она берёт LevelData по номеру LevelIndex.
public static class GameSession
{
    public const string GameScene = "Game";
    public const string MenuScene = "MainMenu";
    public const string WinScene = "Win";

    public static int LevelIndex { get; private set; }   // 0, 1, 2 — индекс в LevelCatalog

    public static LevelData Current
    {
        get
        {
            LevelCatalog c = LevelCatalog.Instance;
            return c != null && c.levels.Length > 0 ? c.levels[Mathf.Clamp(LevelIndex, 0, c.levels.Length - 1)] : null;
        }
    }

    public static int LevelNumber => LevelIndex + 1;      // для игрока и для save.json — с 1

    // Из меню выбора уровня: новый забег, очки с нуля.
    public static void StartLevel(int index)
    {
        LevelIndex = index;
        GameScore.ResetAll();
        SceneFader.Load(GameScene);
    }

    // Портал или победа над боссом: сохраняем прогресс и идём дальше.
    public static void CompleteLevel()
    {
        SaveSystem.CompleteLevel(LevelNumber, GameScore.Score);
        LevelCatalog c = LevelCatalog.Instance;
        if (c != null && LevelIndex + 1 < c.levels.Length)
        {
            LevelIndex++;
            SceneFader.Load(GameScene);   // Очки сохраняются: следующий уровень продолжает забег.
        }
        else
        {
            SceneFader.Load(WinScene);
        }
    }
}
