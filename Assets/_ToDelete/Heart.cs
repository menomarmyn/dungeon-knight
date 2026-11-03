// Старое падающее сердце из A6 (наследник Hazard). В мидтерме лечит HeartPickup.
public class Heart : Hazard
{
    protected override void OnHitPlayer(Health player)
    {
        player.Heal(1);
    }

    // Сердце нельзя уничтожить выстрелом.
    public override void OnShot()
    {
    }
}
