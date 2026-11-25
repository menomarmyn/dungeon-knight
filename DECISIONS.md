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
## Week 12 — Sound pass (mixer, music, saved volume)

2026-11-24  Before the sound pass the Midterm folder (art, audio, builder) was found inside Assets/_ToDelete by accident, so Midterm → Build Everything could not find the files. Moved it back to Assets/Midterm (in Unity, so the GUIDs stayed) before changing anything.
2026-11-24  Asked the agent for a sound layer. Kept one static entry point GameAudio.Play(clip) so old scripts did not change; inside it a pool of 8 AudioSources (made once per scene, Output = SFX) instead of a new AudioSource per shot. Random pitch 0.92–1.08 and 2–3 versions of hit / shoot / hurt / enemy death, never the same clip twice in a row.
2026-11-24  Every enemy got its own wind-up sound at the start of the wind-up (slime, bat, archer, boss fan attack), and the falling rocks now rumble 0.5 s before they drop. Boss wind-up wait 0.35 s -> 0.45 s so the sound is heard before the orbs fly.
2026-11-24  Volume: Log10(v) × 20 with slider min 0.0001 (0.5 -> −6 dB), PlayerPrefs, applied once at launch in MusicManager.Start. Test: Music set to −13 dB, Play mode stopped and started again -> still −13 dB.
2026-11-24  Unity showed "same field name is serialized multiple times: Boss player" — Boss and its base class Enemy both had a private field player. Renamed the Boss field to hero. The first Settings window wrapped "MUSIC" to two lines and "-13 dB" did not fit: label 48 -> 40 px, value 48 -> 34 px.

## Week 11 — Data and saves (done late, after week 12)

2026-11-25  Asked the agent to move all balance numbers into ScriptableObjects. It made one asset per enemy type (Slime, Bat, GoblinArcher, SlimeKing), plus Player, Items and one ProjectileStats per projectile. Scripts now only read stats.xxx; the starting values were copied from the old field defaults so the game plays the same.
2026-11-25  The builder overwrote everything on each Build Everything. Changed it so data assets are created only if missing and never overwritten, otherwise my Inspector edits would be lost on the next build.
2026-11-25  Three scenes Level1/2/3 were replaced by one scene Game + LevelLoader that builds any LevelData at runtime (map, crystals to open the portal, time limit, music). Old text maps were imported into Level_01/02/03.asset once.
2026-11-25  Save: JsonUtility to save.json in persistentDataPath (no BinaryFormatter). Unlocked levels are a List<int>, because JsonUtility skips Dictionary. Broken or missing file -> try/catch -> new SaveData() defaults; the broken file is copied to save.json.broken. Writing goes through a .tmp file so a half-written save cannot replace a good one.
2026-11-25  Volume moved from PlayerPrefs into save.json (assignment 12 asks for "last week's save system"). Saves happen after a level, when Settings closes, and in OnApplicationPause(true) on the MusicManager, which lives in every scene.
