# DECISIONS.md — Dungeon Knight

Decision log: what I asked the AI agent (Claude), what it got wrong, what I changed and why.
3–5 lines per week.

## Week 10 — Three enemy types (FSM, sight, fair attacks)

2026-11-11  Asked the agent for three enemies with different state machines: Slime = Patroller (Patrol → Chase → WindUp → Lunge → Cooldown), Goblin Archer = Shooter (Watch → Aim → Shoot → Cooldown), Bat = Charger (Patrol → WindUp → Dash → Recover). Old Slime/Bat only walked between two points.
2026-11-11  Sight check is shared in the base class Enemy: distance, then angle of the view cone, then Physics2D.Raycast to the player on the Ground layer, so walls block it. The player is found once and cached (no GameObject.Find every frame).
2026-11-11  Fairness rules: every attack has a wind-up with a red flash, a "!" icon and a sound (Slime 0.45 s, Archer 0.6 s, Bat 0.7 s), a cooldown after it, and no attack starts while the enemy is off-screen (OnScreen check).
2026-11-11  Archer aim line follows the player and freezes for the last 0.2 s, so the player can step out of the line. Bat locks its dash direction at the end of the wind-up for the same reason.
2026-11-11  Level 2 end = arena with all three types together (slime on the ground, bat in the air, archer on the ledge). One archer added near the end of Level 1 with the sign "RED LINE = ARROW!".

2026-11-11  Changed after playtest: GoblinArcher.cooldown 1.6 -> 2.0 (arrows too often), BatEnemy.windUp 0.7 -> 0.6 (bat too easy)