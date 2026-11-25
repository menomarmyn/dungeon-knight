using UnityEngine;

// Скорость, время жизни и урон снаряда: пуля героя, стрела, сфера и огненный шар босса.
[CreateAssetMenu(menuName = "Dungeon Knight/Projectile Stats", fileName = "Projectile")]
public class ProjectileStats : ScriptableObject
{
    public float speed;
    public float lifetime;
    public int damage;
}
