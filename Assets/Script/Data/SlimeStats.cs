using UnityEngine;

// Баланс слизня (PATROLLER).
[CreateAssetMenu(menuName = "Dungeon Knight/Slime Stats", fileName = "Slime")]
public class SlimeStats : EnemyStats
{
    [Header("Patrol")]
    public float patrolSpeed;
    public float waitAtPoint;

    [Header("Chase")]
    public float chaseSpeed;
    public float loseSightTime;
    public float attackRange;

    [Header("Attack")]
    public float windUp;
    public float lungeSpeed;
    public float lungeTime;
    public float cooldown;
}
