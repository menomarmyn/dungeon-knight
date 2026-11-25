using UnityEngine;

// Баланс героя. Числа живут в ассете Assets/Midterm/Data/Player.asset, а не в коде:
// поменял значение в Inspector — игра изменилась без правки скриптов.
[CreateAssetMenu(menuName = "Dungeon Knight/Player Stats", fileName = "Player")]
public class PlayerStats : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed;
    public float jumpForce;
    [Tooltip("Можно прыгнуть чуть позже, чем сошёл с края (сек).")]
    public float coyoteTime;
    [Tooltip("Нажатие прыжка чуть раньше приземления тоже считается (сек).")]
    public float jumpBuffer;
    [Tooltip("Отпустил кнопку раньше — скорость вверх умножается на это число.")]
    [Range(0f, 1f)] public float jumpCutMultiplier;

    [Header("Health")]
    public int maxHp;
    public float invincibleTime;
    public Vector2 knockback;
    public float knockbackTime;

    [Header("Shooting")]
    public float fireCooldown;
}
