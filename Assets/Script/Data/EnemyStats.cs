using UnityEngine;

// Общий баланс врага: здоровье, урон касанием, очки и зрение.
// Наследники (SlimeStats, BatStats, ArcherStats, BossStats) добавляют свои числа атаки.
[CreateAssetMenu(menuName = "Dungeon Knight/Enemy Stats", fileName = "Enemy")]
public class EnemyStats : ScriptableObject
{
    [Header("Health")]
    public int maxHp;
    public int contactDamage;
    public int scoreValue;

    [Header("Sight")]
    public float viewDistance;
    [Range(0f, 360f)] public float viewAngle;
}
