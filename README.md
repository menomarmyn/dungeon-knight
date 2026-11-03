# Dungeon Knight: Rise of the Slime King

A mobile 2D action platformer made in **Unity 6.6 (6000.6.4f1)**. A tiny knight with a blaster runs and jumps through a pixel dungeon, shoots slimes and bats, collects energy crystals to open portals, and defeats the Slime King.

Game Development Fundamentals · weeks 9–13 project (grown from the midterm).

## Platform
- iOS / Android, **landscape**
- UI tested in Device Simulator: 16:9 phone, 19.5:9 notch phone, 4:3 tablet

## Controls
| On screen | Keyboard (debug) | Action |
|---|---|---|
| Joystick (bottom-left) | A / D, arrows | Run |
| Green button (bottom-right) | Space / W | Jump (hold = higher) |
| Red button (bottom-right) | X / J | Fire (hold = auto-fire) |
| II (top-right) | Esc / P | Pause: Resume · Restart · Menu |

## Screens
Main Menu → Level 1 → Level 2 → Boss → Win, plus the Pause menu.
All canvases: Canvas Scaler *Scale With Screen Size*, 1920 × 1080, Match = 1; HUD and buttons inside a `SafeArea` panel.

## How to open
1. Open the folder in Unity Hub with Unity 6000.6.4f1.
2. Menu **Midterm → Build Everything** (rebuilds scenes, prefabs, animators and the HUD).
3. Open `Assets/Scenes/Midterm/MainMenu.unity` and press Play.

## Project layout
- `Assets/Script` — game logic (player, enemies, boss, HUD, pause, score)
- `Assets/Midterm/Art`, `Audio`, `Fonts` — original pixel art, chiptune sounds, Press Start 2P font (OFL)
- `Assets/Midterm/Levels` — level layouts as text maps
- `Assets/Midterm/Editor/MidtermBuilder.cs` — editor tool that builds the scenes


Assets and licenses: see `README.txt`. AI use: see `AI_LOG.txt`.
