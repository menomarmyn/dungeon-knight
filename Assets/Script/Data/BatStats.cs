using UnityEngine;

// Баланс летучей мыши (CHARGER).
[CreateAssetMenu(menuName = "Dungeon Knight/Bat Stats", fileName = "Bat")]
public class BatStats : EnemyStats
{
    [Header("Patrol")]
    public float patrolSpeed;
    public float waveHeight;

    [Header("Dash")]
    public float windUp;
    public float dashSpeed;
    public float dashTime;
    public float recoverTime;
    public float returnSpeed;
}
