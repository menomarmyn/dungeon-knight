// Очки за всю игру. Статический класс: значение живёт между сценами (уровнями).
// Кристалл +100, слизень / мышь +50, босс +1000.
public static class GameScore
{
    public static int Score { get; private set; }

    private static int levelStartScore; // Очки в начале уровня — к ним возвращаемся при рестарте.

    public static void Add(int points)
    {
        Score += points;
        if (HUD.Instance != null)
            HUD.Instance.SetScore(Score);
    }

    // Новая игра из главного меню.
    public static void ResetAll()
    {
        Score = 0;
        levelStartScore = 0;
    }

    // Вызывается HUD при загрузке уровня.
    public static void MarkLevelStart()
    {
        levelStartScore = Score;
    }

    // Смерть или Restart — очки за этот уровень сгорают.
    public static void ResetToLevelStart()
    {
        Score = levelStartScore;
    }
}
