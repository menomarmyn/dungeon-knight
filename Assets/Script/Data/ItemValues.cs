using UnityEngine;

// Ценность предметов и урон ловушек.
[CreateAssetMenu(menuName = "Dungeon Knight/Item Values", fileName = "Items")]
public class ItemValues : ScriptableObject
{
    [Header("Pickups")]
    public int crystalScore;
    public int heartHeal;

    [Header("Traps")]
    public int spikeDamage;
    public int rockDamage;
    public float rockLifetime;

    [Header("Falling rocks (spawner)")]
    public float rockFirstDelay;
    public float rockInterval;
    public float rockSpreadX;            // половина ширины зоны падения
    public float rockWarnTime;           // грохот за столько секунд до падения
}
