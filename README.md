# The Jolly Leprechaun

A 3D stealth-platformer built with **Godot 4.5** and **C#** where you play as a mischievous leprechaun evading enemies through hiding and scaring mechanics.

## Gameplay

You are a leprechaun trying to avoid being caught by patrolling enemies. Use your wits to:

- **Hide** in bushes and barrels to avoid detection
- **Scare** enemies to make them flee in terror
- **Evade** enemies by staying out of their vision cones
- **Platform** across the environment to reach safety

If an enemy catches you, you'll have to restart the level!

## Features

- 3D platformer movement with camera-relative controls
- Two enemy types with unique behaviors:
  - **Bird Enemy** - Can fly and has aerial patrol patterns
  - **Humanoid Enemy** - Ground-based with wider detection range
- AI state machine with Idle, Patrol, Chase, and Scared states
- Vision cone detection with line-of-sight checks
- Hiding system that makes you undetectable
- Scare ability with cooldown
- Animated characters with state-based animations
- Full keyboard/mouse and gamepad support
- Pause menu with fullscreen toggle

## Controls

### Keyboard & Mouse

| Action | Key |
|--------|-----|
| Move | WASD |
| Jump | Space |
| Sprint | Left Shift |
| Scare | E |
| Interact/Hide | F |
| Look Around | Mouse |
| Zoom In/Out | Scroll Wheel |
| Pause | Escape |

### Gamepad

| Action | Button |
|--------|--------|
| Move | Left Stick |
| Jump | A / Cross |
| Sprint | RT / R2 |
| Scare | X / Square |
| Interact/Hide | Y / Triangle |
| Look Around | Right Stick |
| Zoom Toggle | Left Stick Click |
| Pause | Start |

## Requirements

- [Godot 4.5+](https://godotengine.org/) with .NET support
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/the-jolly-leprechaun.git
   cd the-jolly-leprechaun
   ```

2. Build the C# project:
   ```bash
   dotnet build
   ```

3. Open in Godot:
   - Launch Godot 4.5+ (.NET version)
   - Import the project by selecting the `project.godot` file
   - Press F5 to run the game

## Project Structure

```
res://
├── Assets/
│   └── Models/              # 3D character models (.glb)
├── Resources/
│   └── Characters/          # Enemy data resources (.tres)
├── Scenes/
│   ├── Entities/
│   │   ├── Player/          # Player scene
│   │   ├── Enemies/         # Bird and Humanoid enemy scenes
│   │   └── Interactables/   # Hiding spots
│   ├── Levels/              # Game levels
│   └── UI/
│       ├── HUD/             # In-game HUD
│       └── Menus/           # Main menu, pause menu
├── Scripts/
│   ├── Core/
│   │   ├── Autoloads/       # Singletons (InputManager, GameEvents, GameManager)
│   │   ├── Base/            # Base classes
│   │   └── Interfaces/      # C# interfaces (IState, IScareable, IHideable, etc.)
│   ├── Components/
│   │   ├── DetectionComponent.cs
│   │   └── StateMachine/    # State machine and enemy states
│   ├── Data/                # Data structures (EnemyData)
│   ├── Entities/
│   │   ├── Player/          # Player scripts
│   │   ├── Enemies/         # Enemy scripts
│   │   └── Interactables/   # Hiding spot scripts
│   └── UI/                  # Menu scripts
└── docs/
    └── PRACTICES.md         # Development principles
```

## Architecture

### Core Systems

- **InputManager** - Unified input abstraction for keyboard/mouse and gamepad
- **GameEvents** - Global event bus for decoupled communication
- **GameManager** - Game state, level loading, pause functionality

### Enemy AI

Enemies use a state machine pattern with four states:

1. **IdleState** - Standing still, looking around
2. **PatrolState** - Moving between patrol points
3. **ChaseState** - Pursuing the detected player
4. **ScaredState** - Fleeing from the player after being scared

### Detection System

The `DetectionComponent` provides:
- Configurable vision cone (range and angle)
- Line-of-sight raycasting
- Respects hiding state (won't detect hidden players)

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IState` | State machine states |
| `IScareable` | Entities that can be scared |
| `IHideable` | Objects players can hide in |
| `IDetectable` | Entities that can be detected |
| `IInteractable` | Objects players can interact with |

## Development

### Building

```bash
# Build the project
dotnet build

# Build with warnings as errors
dotnet build /p:TreatWarningsAsErrors=true

# Format code
dotnet format
```

### Running Tests

```bash
dotnet test
```

### Code Style

This project follows the conventions defined in [CLAUDE.md](CLAUDE.md):
- C# naming conventions (PascalCase for public, _camelCase for private fields)
- Composition over inheritance
- Signal-based communication for decoupling
- Resource-based data for configuration

## Credits

- Built with [Godot Engine](https://godotengine.org/) 4.5
- Character models created with various 3D modeling tools

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
