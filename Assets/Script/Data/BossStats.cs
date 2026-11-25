using UnityEngine;

// Баланс Короля слизней. Вторая фаза — когда HP ниже phase2At (доля от максимума).
[CreateAssetMenu(menuName = "Dungeon Knight/Boss Stats", fileName = "SlimeKing")]
public class BossStats : EnemyStats
{
    [Range(0f, 1f)] public float phase2At;
    public float introDelay;

    [Header("Flight")]
    public float moveTime;
    public float moveTimePhase2;
    public Vector2 hoverHeight;          // мин и макс высота над полом
    public Vector2 arenaMargin;          // отступ от левой и правой стены

    [Header("Fan of orbs")]
    public float fanWindUp;
    public int fanCount;
    public int fanCountPhase2;
    public float fanSpread;
    public float fanSpreadPhase2;
    public int fanWaves;
    public int fanWavesPhase2;
    public float fanWaveGap;
    public float pauseAfterFan;
    public float pauseAfterFanPhase2;

    [Header("Fire rain")]
    public int rainColumns;
    public int rainColumnsPhase2;
    public float rainWarnTime;
    public float rainWarnTimePhase2;
    public float pauseAfterRain;
    public float pauseAfterRainPhase2;
}
