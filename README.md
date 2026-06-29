# >_ESCALATE

A fast-paced 2D vertical platformer built in Unity, wrapped in a corrupted failing system. Climb as high as you can before the system you're running on stabilizes completely.

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-black?logo=unity)
![Status](https://img.shields.io/badge/status-finished-brightgreen)
![Platform](https://img.shields.io/badge/platform-2D-blue)
[![itch.io](https://img.shields.io/badge/itch.io-play-FA5C5C?logo=itch.io)](https://theselfwithasmile.itch.io/escalate)
![gameplay](Gameplay.gif)

---


## Overview

">_ESCALATE" is an endless climbing game where the player, as a virus, chains dashes, jumps, and momentum-based movement to ascend through forbidden memory space of a failing Windows 2000 OS. The space is devoid of predefined platforms and so it is your purpose to perturb the system, triggering its protection windows to be exploited as footholds.

The higher you climb, the more aggressive the system gets: collision events ramp up difficulty, spawn rates increase, and the world visually degrades.



## Gameplay Features

- **Momentum-based movement**: 8-directional input with acceleration and friction. Responsive platforming feel is emphasized (via coyote time and jump cut/buffering).
- **Movement techniques**: maximize skill expression via dashing (directonal velocity boost), warping (direct transform modification) and reflecting (large velocity boost rewarding precise timing near obstacles).
- **Physics System**: tracks collisions states (via Box Collider), and resolve them as candidate postion before committing (for stable rendering and deletion), enabling granular interaction (velocity transfer on impact, vertical damping...). Rigidbody2D's blackbox thus is entirely avoided.
- **Procedural Platforming**: collisions of varying strengths spawn either falling, interactive blocks or stable kinematic blocks, being the only platforming vehicles for climbing, encouraging aggressive plays.
- **Difficulty scaling**: gravity and block spawn trigger thresholds scale with player progress, blocks naturally become barriers difficult to climb on top. Scalar static noise post process emphasizes the chaotic feel.
- **Reactive background grid**: renders a background grid visually bending and bleeding chromatic abberration to ripples caused by movement/physics events via an unlit shader. Dithering bloom and scanline desync VFXs appear in response to collisions. The background pallette gradually inverts as the virus approaches victory.
- **UI rendering**: renders glitching text (garbled-character corruption).
- **Gamemode**: introduces hard and inifinite modes for replayability.


## Tech Stack

- **Engine:** Unity 2022.3.62f3 (LTS)
- **Language:** C#
- **Rendering:** Custom shaders (HLSL) for noise, banding, and dithering effects
- **UI:** TextMeshPro + Unity UGUI

## Project Structure

```
Assets/Scripts/
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

| Input | Action |
|---|---|
| `WASD` / Arrow Keys | Move |
| `Z` | Jump |
| `X` | Dash |
| `C` | Warp |
| `Z` (while dashing, near a block) | Reflect |
| `Escape` | Pause |


## A Note on Code Quality

This was my first Unity project, built on a significant time constraint as a way to learn the engine end-to-end. The game itself is finished and playable, but some of the underlying code reflects an early-stage learning process rather than production-grade conventions. 

The project nonetheless complements my foundational experience with graphics APIs, hence the heavier logic and visual polish uncommonly seen for a first project.

## Status

The project is complete and playable in its current state. No further feature work is planned, though the codebase remains a useful reference for the shader/VFX and gameplay-feel work involved.
