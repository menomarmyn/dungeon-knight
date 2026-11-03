DUNGEON KNIGHT — Rise of the Slime King
Game Development Fundamentals · Midterm Project (Path A — 2D side-scroller)

ENGINE
  Unity 6.6 (6000.6.4f1), Universal Render Pipeline (URP 17.6)
  Packages: Input System 1.20, 2D Sprite, 2D Tilemap, TextMesh Pro (uGUI 2.6)

HOW TO OPEN / RUN
  1. Open the project folder in Unity Hub (Unity 6000.6.4f1).
  2. Menu  Midterm -> Build Everything  (re-creates all scenes, prefabs, animators, HUD and Build Settings).
  3. Open Assets/Scenes/Midterm/MainMenu.unity and press Play.
  Phone (iPhone + Windows): Unity Remote 5 + cable; Edit -> Project Settings -> Editor -> Device: Any iOS Device.

GAME FLOW (5 screens)
  MainMenu -> Level1 (The Crypt, tutorial) -> Level2 (Deep Halls) -> Level3_Boss (Slime King) -> Win -> MainMenu
  Goal of Levels 1-2: collect N energy crystals, the portal opens, enter the portal.
  Death (HP = 0 or falling into a pit) restarts the current level.

CONTROLS
  Phone: on-screen joystick (move), green button (jump), red button (fire, hold for auto-fire)
  Keyboard: A/D or arrows — move, Space/W — jump, X/J — fire

WHERE THE COURSE TOPICS ARE
  A4 Physics   : Player2D.cs (Rigidbody2D velocity, OverlapBox ground check), Tilemap + CompositeCollider2D,
                 PlatformEffector2D one-way ledges, pickup triggers (Pickup.cs), damage on contact (Enemy.cs, Spikes.cs)
  A5 Animation : Hero Animator (Idle/Run/Jump/Fall + flipX), Slime/Bat/Crystal/Torch/Portal/Boss animators,
                 moving platform + lift + heart bob = Animation Clips (Assets/Midterm/Generated/Anim)
  A6 Scripting : Shooter.cs (cooldown), Boss.cs (Instantiate spawn patterns, coroutines, phase 2),
                 Health.cs (invincibility coroutine + blinking), inheritance: Enemy -> SlimeEnemy/BatEnemy/Boss,
                 Projectile -> Bullet/EnemyProjectile, Pickup -> Crystal/HeartPickup, Hazard -> Rock;
                 SceneManager in SceneFader.cs / Health.cs; sound in GameAudio.cs
  A7 Input     : Assets/Midterm/Input/GameControls.inputactions, PlayerInput on the Player prefab,
                 On-Screen Stick (<Gamepad>/leftStick) and On-Screen Buttons (<Gamepad>/buttonSouth, buttonWest)

ASSETS AND LICENSES
  All sprites (knight, slimes, bats, Slime King, tiles, background, portal, UI, icons) — original pixel art,
  generated procedurally for this project (Python + Pillow). No third-party sprites. License: own work.
  All sound effects and the 3 music loops (menu / level / boss) — original chiptune, synthesized
  procedurally for this project (Python + NumPy). License: own work.
  Font: "Press Start 2P" by CodeMan38 — SIL Open Font License 1.1
        (source: Google Fonts / fontsource, license file: Assets/Midterm/Fonts/PressStart2P_OFL.txt)
  Visual style inspired by Soul Knight (ChillyRoom) — no assets from that game are used.

FOLDERS
  Assets/Script            — all game-logic C# scripts
  Assets/Midterm/Art       — sprites and sprite sheets
  Assets/Midterm/Audio     — sounds and music
  Assets/Midterm/Levels    — level layouts as text maps (editable: # stone, = ledge, ^ spikes, c crystal, ...)
  Assets/Midterm/Editor    — MidtermBuilder.cs (editor tool that builds scenes/prefabs)
  Assets/Midterm/Generated — generated prefabs, tiles, animation clips, animator controllers, font asset
  Assets/Scenes/Midterm    — the 5 game scenes
