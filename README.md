# Ascendancy

A fast-paced 2D vertical platformer built in Unity, wrapped in a corrupted, glitching "failing system" aesthetic. Climb as high as you can before the system you're running on destabilizes completely.

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-black?logo=unity)
![Status](https://img.shields.io/badge/status-finished-brightgreen)
![Platform](https://img.shields.io/badge/platform-2D-blue)

## Overview

Ascendancy is an endless climbing game where the player chains dashes, jumps, and momentum-based movement to ascend through a procedurally spawning column of blocks. The higher you climb, the more the environment fights back: collision events ramp up difficulty, spawn rates increase, and the world visually degrades through glitch, static, and dithering effects that sell the idea of a system on the verge of collapse.

The whole presentation layer: UI, VFX, shaders, and audio: leans into this "failing terminal" theme: stuttering glitch-text, scanline/static post-processing, bloom dithering ripples, and a reactive soundtrack that responds to how close the system is to falling apart.

## Gameplay Features

- **Momentum-based movement**: 8-directional input, ground/air acceleration and friction, coyote time, and jump buffering for responsive platforming feel.
- **Dash & warp mechanics**: directional dashing with a chargeable warp and a reflect system that rewards precise timing near obstacles.
- **Dynamic difficulty scaling**: block spawn rate and behavior scale with player progress, driven by a physics-based collision/event system.
- **Self-spawning kinematic blocks**: the world generates itself ahead of the player, including animated spawn-in blocks.
- **"System disruption" scoring**: progress is framed as a percentage of system disruption rather than a traditional score/height counter.
- **Reactive audio**: background music shifts in response to game state and instability.

## Visual / Tech Showcase

This project doubles as a showcase of custom shader and VFX work built specifically for the failing-system theme:

- Custom **static noise** and **grid** shaders
- Bloom **dithering ripple** effects tied to player movement and impacts
- **Terminal-style text glitching** (stuttering, garbled-character corruption) for UI text
- Afterimage / motion-trail VFX on the player
- Velocity-based sprite stretching for physics bodies
- Fully reactive UI screen system (home, settings, pause, game over, win, navigation) driven by a single state-aware controller

## Tech Stack

- **Engine:** Unity 2022.3.62f3 (LTS)
- **Language:** C#
- **Rendering:** Custom shaders (HLSL) for noise, banding, and dithering effects
- **UI:** TextMeshPro + Unity UGUI
- **Other:** Unity Timeline, Visual Scripting package included (not core to gameplay)

## Project Structure

```
Assets/
  Scenes/         # Main game scene
  Scripts/
    Player.cs           # Core player movement, dash, warp, reflect logic
    PhysicsBody.cs       # Shared physics base (velocity, gravity, collisions)
    PhysicsManager.cs    # Collision event hub driving difficulty scaling
    Block.cs / BlockSpawner.cs   # Procedural block generation
    UI.cs                 # Screen state management and navigation
    ProgressBar.cs / ProgressHandler.cs  # "Disruption" progress tracking
    TerminalStutter.cs    # Glitch-text VFX for UI
    Afterimage.cs / PlayerVFX.cs  # Player visual effects
    RippleManager.cs       # Bloom dithering ripple VFX
    AudioManager.cs        # Reactive music/audio system
    CameraController.cs    # Camera follow/feel
    Grid.shader / StaticNoise.shader   # Custom shaders
```

## Controls

| Action | Key |
|---|---|
| Move | `WASD` / Arrow Keys |
| Dash | (bound in-project) |
| Jump | (bound in-project) |

## Running the Project

1. Install **Unity 2022.3.62f3** (or a compatible 2022.3 LTS version) via Unity Hub.
2. Clone this repository.
3. Open the project folder in Unity Hub and let it import.
4. Open `Assets/Scenes/MainScene.unity` and press Play.

## A Note on Code Quality

This was my first full Unity project, built as a way to learn the engine end-to-end: gameplay programming, shaders, VFX, UI flow, and audio integration. The game itself is finished and playable, but some of the underlying code reflects an early-stage learning process rather than production-grade conventions (naming consistency, leftover debug code, some duplicated logic, etc.). I'm calling that out upfront rather than pretending otherwise: happy to talk through any part of the implementation and what I'd do differently with what I know now.

## Status

The project is complete and playable in its current state. No further feature work is planned, though the codebase remains a useful reference for the shader/VFX and gameplay-feel work involved.
