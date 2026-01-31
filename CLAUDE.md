# CLAUDE.md - Godot C# Game Development

## Project Overview

This is a Godot 4.x game project using C# as the primary scripting language. All development follows established software engineering principles adapted for game development contexts.

## Quick Reference

```bash
# Build and run
dotnet build
godot --path . --editor     # Open editor
godot --path .              # Run game

# Testing (GdUnit4)
dotnet test

# Format code
dotnet format

# Analyze code
dotnet build /p:TreatWarningsAsErrors=true
```

## Core Principles

> **READ BEFORE WRITING CODE**: See `docs/PRACTICES.md` for detailed explanations.

| Principle | Game Dev Application |
|-----------|---------------------|
| **DRY** | Extract components, use Resources for shared data |
| **SOLID** | One script = one responsibility, depend on abstractions |
| **KISS** | Simplest working solution wins |
| **YAGNI** | Build for today's needs, not hypothetical futures |
| **Composition > Inheritance** | Node composition, max 3 levels inheritance |
| **Separation of Concerns** | Logic ≠ Presentation ≠ Data |
| **Fail Fast** | Validate inputs, crash with clear messages in debug |
| **Law of Demeter** | Talk to friends only, use signals for decoupling |
| **Command-Query Separation** | Methods do OR return, not both |
| **Least Astonishment** | Code does what its name says, nothing hidden |
| **Defensive Programming** | Validate external data, provide defaults |
| **Idempotency** | Init methods safe to call multiple times |
| **TDD** | Test complex systems, not visual/feel code |
| **No Premature Optimization** | Profile first, optimize bottlenecks only |

## Project Structure

```
res://
├── Scenes/
│   ├── Entities/           # Player, enemies, NPCs, interactables
│   │   └── Player/
│   │       ├── Player.tscn
│   │       └── player_components/
│   ├── UI/                 # All user interface scenes
│   │   ├── HUD/
│   │   ├── Menus/
│   │   └── Components/     # Reusable UI elements
│   ├── Levels/             # Level/world scenes
│   └── Components/         # Reusable behavior component scenes
│       ├── HealthComponent.tscn
│       ├── HitboxComponent.tscn
│       └── StateMachine/
├── Scripts/
│   ├── Core/               # Autoloads, managers, base classes
│   │   ├── Autoloads/      # Global singletons
│   │   ├── Base/           # Abstract base classes
│   │   └── Interfaces/     # C# interfaces
│   ├── Components/         # Attachable behavior scripts
│   ├── Data/               # Data structures, DTOs, enums
│   ├── Systems/            # Game systems (combat, inventory, save/load)
│   ├── Utils/              # Static helpers, extension methods
│   └── Generated/          # Any generated code (Godot source gen)
├── Resources/
│   ├── Items/              # Item Resource definitions
│   ├── Characters/         # Character stat blocks
│   ├── Abilities/          # Ability definitions
│   └── Config/             # Balance data, game settings
├── Assets/
│   ├── Art/
│   │   ├── Sprites/
│   │   ├── Tilesets/
│   │   └── UI/
│   ├── Audio/
│   │   ├── Music/
│   │   ├── SFX/
│   │   └── Voice/
│   ├── Fonts/
│   └── Shaders/
├── Tests/                  # GdUnit4 test files
│   ├── Unit/
│   └── Integration/
├── Addons/                 # Godot plugins
└── docs/                   # Documentation
    └── PRACTICES.md        # Detailed principle explanations
```

## Code Standards

### File Naming
- **Scenes**: `PascalCase.tscn` (e.g., `PlayerCharacter.tscn`)
- **Scripts**: `PascalCase.cs` matching class name (e.g., `PlayerMovement.cs`)
- **Resources**: `snake_case.tres` (e.g., `iron_sword.tres`)
- **Interfaces**: `IPascalCase.cs` with `I` prefix (e.g., `IDamageable.cs`)

### C# Conventions

```csharp
// Use partial for Godot source generators
public partial class PlayerMovement : CharacterBody2D
{
    // [Export] for inspector-exposed fields
    [Export] private float _moveSpeed = 200f;
    [Export] private PackedScene _bulletScene;
    
    // Private fields: _camelCase
    private Vector2 _velocity;
    private bool _isGrounded;
    
    // Properties: PascalCase
    public bool IsAlive => _health > 0;
    
    // Signals: PascalCase, event-style naming
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
    
    // Methods: PascalCase
    public void TakeDamage(int amount) { }
    
    // Private methods: PascalCase (C# convention)
    private void HandleInput() { }
}
```

### Required Code Patterns

**Signal Declaration & Usage**
```csharp
// Declare
[Signal] public delegate void InteractedEventHandler(Node interactor);

// Emit
EmitSignal(SignalName.Interacted, this);

// Connect (prefer in _Ready)
target.Interacted += OnTargetInteracted;
```

**Null Safety**
```csharp
// Use nullable reference types
private PlayerStats? _stats;

// Null-conditional operators
_stats?.ModifyHealth(amount);

// Null-coalescing for defaults
var speed = _config?.MoveSpeed ?? DefaultSpeed;
```

**Export Groups**
```csharp
[ExportGroup("Movement")]
[Export] private float _moveSpeed = 200f;
[Export] private float _jumpForce = 400f;

[ExportGroup("Combat")]
[Export] private int _baseDamage = 10;
```

### Forbidden Patterns

❌ **God Objects**
```csharp
// BAD: One script doing everything
public partial class Player : CharacterBody2D
{
    // Movement, combat, inventory, UI, audio, saving all in one class
}
```

✅ **Composed Behaviors**
```csharp
// GOOD: Separate components
Player.tscn
├── PlayerMovement.cs
├── HealthComponent (scene)
├── InventoryComponent (scene)
└── PlayerInput.cs
```

❌ **Deep Coupling**
```csharp
// BAD: Reaching through objects
var dmg = GetParent().GetParent().GetNode<Enemy>("Enemy").Weapon.Stats.Damage;
```

✅ **Decoupled Communication**
```csharp
// GOOD: Signals and direct references
[Export] private HealthComponent _health;
_health.Damaged += OnDamaged;
```

❌ **Magic Strings**
```csharp
// BAD
GetNode<Player>("/root/Main/World/Player");
AnimationPlayer.Play("attack_01");
```

✅ **Type-Safe References**
```csharp
// GOOD
[Export] private Player _player;
[Export] private StringName _attackAnimation = "attack_01";
```

❌ **Command-Query Violation**
```csharp
// BAD: Both modifies AND returns
public int TakeDamage(int amount) 
{
    _health -= amount;  // Command
    return _health;     // Query
}
```

✅ **Separated Concerns**
```csharp
// GOOD
public void TakeDamage(int amount) => _health -= amount;
public int GetHealth() => _health;
```

## Architecture Patterns

### Component-Based Design

Entities are composed of reusable component scenes:

```
Enemy.tscn
├── Sprite2D
├── CollisionShape2D
├── HealthComponent.tscn      # Manages HP, emits Died signal
├── HitboxComponent.tscn      # Deals damage on contact
├── HurtboxComponent.tscn     # Receives damage
├── StateMachine/
│   ├── IdleState.tscn
│   ├── ChaseState.tscn
│   └── AttackState.tscn
└── NavigationAgent2D
```

### State Machine Pattern

```csharp
// State interface
public interface IState
{
    void Enter();
    void Exit();
    void Update(double delta);
    void PhysicsUpdate(double delta);
}

// State machine manages transitions
public partial class StateMachine : Node
{
    [Export] private IState _initialState;
    private IState _currentState;
    
    public void TransitionTo(IState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
}
```

### Resource-Based Data

```csharp
// Define data structure
public partial class ItemData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Texture2D Icon { get; set; }
    [Export] public int MaxStack { get; set; } = 1;
    [Export] public ItemType Type { get; set; }
}

// Reference in code
[Export] private ItemData _itemData;
```

### Autoload Services

Minimal, focused singletons:

```csharp
// res://Scripts/Core/Autoloads/EventBus.cs
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }
    
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void LevelCompletedEventHandler(int levelIndex);
    
    public override void _Ready() => Instance = this;
}
```

Register in Project Settings > Autoload:
- `EventBus` → res://Scripts/Core/Autoloads/EventBus.cs
- `AudioManager` → res://Scripts/Core/Autoloads/AudioManager.cs
- `SaveSystem` → res://Scripts/Core/Autoloads/SaveSystem.cs

### Signal Flow

```
Signal UP, Call DOWN

Parent
  │
  ├── ChildA ──signal──▶ Parent listens
  │     │
  │     └── GrandchildA
  │
  └── ChildB ◀──method call── Parent commands
```

## Testing Strategy

### What to Test

✅ **Test These**
- State machines and transitions
- Inventory operations (add, remove, stack, split)
- Save/load serialization
- Combat calculations (damage, defense, crit)
- Economy systems (currency, trading)
- Pathfinding edge cases
- Resource parsing

❌ **Don't Test These**
- Visual appearance ("does it look right?")
- "Game feel" (play test instead)
- Godot engine behavior
- Simple getters/setters

### Test Structure (GdUnit4)

```csharp
[TestSuite]
public partial class InventoryTests : GdUnitTestSuite
{
    private Inventory _inventory;
    
    [Before]
    public void Setup()
    {
        _inventory = new Inventory(maxSlots: 10);
    }
    
    [TestCase]
    public void AddItem_WhenSpaceAvailable_ReturnsTrue()
    {
        var item = CreateTestItem();
        
        var result = _inventory.TryAddItem(item);
        
        AssertBool(result).IsTrue();
        AssertInt(_inventory.ItemCount).IsEqual(1);
    }
    
    [TestCase]
    public void AddItem_WhenFull_ReturnsFalse()
    {
        FillInventory(_inventory);
        var item = CreateTestItem();
        
        var result = _inventory.TryAddItem(item);
        
        AssertBool(result).IsFalse();
    }
}
```

## Performance Guidelines

### Profile First
```csharp
// Use Godot's built-in profiler
// Debug > Profiler

// Or manual timing for specific code
var sw = Stopwatch.StartNew();
ExpensiveOperation();
GD.Print($"Operation took {sw.ElapsedMilliseconds}ms");
```

### Known Optimization Points

| Pattern | When to Apply |
|---------|--------------|
| Object pooling | Bullets, particles, frequently spawned objects |
| Physics layers | Limit collision checks |
| `_Process` vs `_PhysicsProcess` | Use appropriately, disable when not needed |
| `SetProcess(false)` | Disable updates on inactive objects |
| Visibility notifiers | Cull off-screen processing |
| LOD | Large worlds with many objects |

### Don't Optimize (Until Profiler Says So)

- String concatenation in non-hot paths
- Minor allocations
- "Clever" bit manipulation for readability
- Inlining small methods manually

## Git Workflow

### Commit Message Format

```
type(scope): description

[optional body]

[optional footer]
```

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`

Examples:
```
feat(combat): add critical hit system
fix(inventory): prevent negative stack counts  
refactor(player): extract movement to component
docs(readme): add build instructions
test(save): add serialization round-trip tests
```

### Branch Strategy

- `main` - stable, playable builds only
- `develop` - integration branch
- `feature/name` - new features
- `fix/name` - bug fixes
- `refactor/name` - code improvements

### .gitignore Essentials

```gitignore
# Godot
.godot/
*.import

# .NET
bin/
obj/
*.user

# IDE
.vs/
.idea/
*.sln.DotSettings.user

# OS
.DS_Store
Thumbs.db
```

## Common Gotchas

### Godot + C# Specific

1. **Partial Classes Required**
   ```csharp
   public partial class MyNode : Node  // ✅
   public class MyNode : Node          // ❌ Won't work with signals/exports
   ```

2. **Signal Naming**
   ```csharp
   [Signal] public delegate void DiedEventHandler();  // Must end in EventHandler
   ```

3. **Export Initialization**
   ```csharp
   [Export] private float _speed = 100f;  // ✅ Has default
   [Export] private Node _target;         // ⚠️ Will be null until set in editor
   ```

4. **Calling Godot Methods Before Ready**
   ```csharp
   public override void _Ready()
   {
       // Safe to call GetNode, GetTree, etc. HERE
   }
   
   // Constructor runs before the node is in the tree!
   public MyNode()
   {
       GetNode<Foo>("Bar");  // ❌ Will fail
   }
   ```

5. **C# async vs Godot Timers**
   ```csharp
   // Prefer Godot's signal-based approach for game logic
   await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
   
   // Or use async for truly async operations (file I/O, network)
   await SomeAsyncOperation();
   ```

## AI Assistant Instructions

When generating code for this project:

1. **Follow all conventions** in this document without exception
2. **Explain architectural decisions** in comments when non-obvious
3. **Prefer composition** over inheritance for game objects
4. **Use signals** for decoupled communication
5. **Create Resources** for data-driven content
6. **Write tests** for complex game logic systems
7. **Never use magic strings** for node paths or animations
8. **Apply KISS** - simplest working solution wins
9. **Flag potential issues** specific to Godot/C#
10. **Reference `docs/PRACTICES.md`** when a principle is being applied

When asked to create a new system:
1. Clarify requirements and scope
2. Propose architecture with component breakdown
3. Identify what should be tested
4. Implement incrementally, simplest pieces first
5. Refactor only when patterns emerge

---

*Last Updated: Project Initialization*
*See `docs/PRACTICES.md` for principle deep-dives*