using UnityEngine;

// Кристалл энергии: +1 к счётчику. Нужен, чтобы открыть портал.
public class Crystal : Pickup
{
    protected override bool Collect(GameObject player)
    {
        player.GetComponent<Player2D>().AddCrystal();
        return true;
    }
}
