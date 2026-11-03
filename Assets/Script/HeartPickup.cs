using UnityEngine;

// Сердце лечит 1 HP. Если здоровье полное — остаётся лежать.
public class HeartPickup : Pickup
{
    protected override bool Collect(GameObject player)
    {
        Health h = player.GetComponent<Health>();
        if (h.hp >= h.maxHp)
            return false;
        h.Heal(1);
        return true;
    }
}
