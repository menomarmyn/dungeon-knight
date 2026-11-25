using UnityEngine;

// Баланс гоблина-лучника (SHOOTER).
[CreateAssetMenu(menuName = "Dungeon Knight/Archer Stats", fileName = "GoblinArcher")]
public class ArcherStats : EnemyStats
{
    public float turnAroundTime;
    [Tooltip("Замах: прицел виден, можно уйти с линии.")]
    public float aimTime;
    [Tooltip("Последние доли секунды прицел больше не следит за игроком.")]
    public float lockTime;
    public float cooldown;
}
