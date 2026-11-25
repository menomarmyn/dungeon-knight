using UnityEngine;

// Кристалл энергии: +1 к счётчику и очки (Items.asset). Нужен, чтобы открыть портал.
public class Crystal : Pickup
{
    public ItemValues items;

    protected override bool Collect(GameObject player)
    {
        player.GetComponent<Player2D>().AddCrystal();
        GameScore.Add(items.crystalScore);
        return true;
    }
}
