using UnityEngine;

// Сердце лечит heartHeal HP (Items.asset). Если здоровье полное — остаётся лежать.
public class HeartPickup : Pickup
{
    public ItemValues items;

    protected override bool Collect(GameObject player)
    {
        Health h = player.GetComponent<Health>();
        if (h.Hp >= h.MaxHp)
            return false;
        h.Heal(items.heartHeal);
        return true;
    }
}
