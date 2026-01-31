# PRACTICES.md - Game Development Principles Deep Dive

This document provides detailed explanations of each software engineering principle as applied to game development with Godot and C#. Reference this when making architectural decisions or when the CLAUDE.md mentions a principle.

---

## Table of Contents

1. [DRY - Don't Repeat Yourself](#dry---dont-repeat-yourself)
2. [SOLID Principles](#solid-principles)
3. [KISS - Keep It Simple, Stupid](#kiss---keep-it-simple-stupid)
4. [YAGNI - You Aren't Gonna Need It](#yagni---you-arent-gonna-need-it)
5. [Composition Over Inheritance](#composition-over-inheritance)
6. [Separation of Concerns](#separation-of-concerns)
7. [TDD - Test-Driven Development](#tdd---test-driven-development)
8. [Fail Fast](#fail-fast)
9. [Law of Demeter](#law-of-demeter)
10. [Command-Query Separation](#command-query-separation)
11. [Principle of Least Astonishment](#principle-of-least-astonishment)
12. [Defensive Programming](#defensive-programming)
13. [Idempotency](#idempotency)
14. [Database Normalization (for Game Data)](#database-normalization-for-game-data)
15. [Premature Optimization is the Root of All Evil](#premature-optimization-is-the-root-of-all-evil)
16. [Principle Conflicts & Trade-offs](#principle-conflicts--trade-offs)

---

## DRY - Don't Repeat Yourself

### Definition

Every piece of knowledge must have a single, unambiguous, authoritative representation within a system.

### Game Development Application

In games, duplication tends to happen in:
- Entity behaviors (movement, health, damage)
- UI elements (health bars, buttons, panels)
- Data definitions (stats spread across multiple files)
- Game logic (similar conditions checked in multiple places)

### Implementation Strategies

**1. Component Scenes for Shared Behavior**

Instead of copying health logic into every entity:

```csharp
// ❌ BAD: Health code duplicated in Player.cs, Enemy.cs, Boss.cs, NPC.cs
public partial class Player : CharacterBody2D
{
    private int _health = 100;
    private int _maxHealth = 100;
    
    public void TakeDamage(int amount)
    {
        _health = Mathf.Max(0, _health - amount);
        if (_health <= 0) Die();
    }
}
```

```csharp
// ✅ GOOD: Single HealthComponent used everywhere
public partial class HealthComponent : Node
{
    [Export] public int MaxHealth { get; set; } = 100;
    
    [Signal] public delegate void HealthChangedEventHandler(int current, int max);
    [Signal] public delegate void DiedEventHandler();
    
    private int _currentHealth;
    
    public override void _Ready() => _currentHealth = MaxHealth;
    
    public void TakeDamage(int amount)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
        
        if (_currentHealth <= 0)
            EmitSignal(SignalName.Died);
    }
    
    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
    }
}
```

**2. Resources for Shared Data**

```csharp
// ❌ BAD: Same values hardcoded in multiple places
// In IronSword.cs: damage = 10, speed = 1.2
// In IronSword.tscn: some other reference to 10 damage
// In BalanceSheet.xlsx: iron sword damage listed as 10

// ✅ GOOD: Single Resource definition
// res://Resources/Items/iron_sword.tres
public partial class WeaponData : Resource
{
    [Export] public string Id { get; set; } = "iron_sword";
    [Export] public int BaseDamage { get; set; } = 10;
    [Export] public float AttackSpeed { get; set; } = 1.2f;
    [Export] public Texture2D Icon { get; set; }
}

// All code references this single Resource
[Export] private WeaponData _weaponData;
```

**3. Utility Methods for Common Operations**

```csharp
// ✅ GOOD: Centralized helpers
public static class VectorUtils
{
    public static Vector2 DirectionTo(this Node2D from, Node2D to)
        => (to.GlobalPosition - from.GlobalPosition).Normalized();
    
    public static float DistanceTo(this Node2D from, Node2D to)
        => from.GlobalPosition.DistanceTo(to.GlobalPosition);
    
    public static bool IsWithinRange(this Node2D from, Node2D to, float range)
        => from.DistanceTo(to) <= range;
}
```

### When DRY Can Hurt

- **Premature abstraction**: Don't extract until you see the pattern 2-3 times
- **Forced coupling**: If two similar things evolve differently, duplication might be better
- **Readability sacrifice**: A small bit of duplication can be clearer than a complex abstraction

---

## SOLID Principles

### S - Single Responsibility Principle

**Definition**: A class should have only one reason to change.

**In Games**: One script handles one aspect of an entity's behavior.

```csharp
// ❌ BAD: God class
public partial class Player : CharacterBody2D
{
    // Movement code...
    // Combat code...
    // Inventory code...
    // Audio code...
    // Animation code...
    // UI update code...
    // Save/load code...
}

// ✅ GOOD: Separated responsibilities
// Player.tscn
// ├── PlayerMovement.cs      - Only handles movement
// ├── PlayerCombat.cs        - Only handles combat
// ├── PlayerInput.cs         - Only handles input mapping
// ├── HealthComponent.tscn   - Only handles health
// ├── Inventory.tscn         - Only handles items
// └── PlayerAnimator.cs      - Only handles animation state
```

### O - Open/Closed Principle

**Definition**: Software entities should be open for extension but closed for modification.

**In Games**: Add new behaviors without changing existing code.

```csharp
// ✅ GOOD: New abilities added via new scripts, not modifying base
public interface IAbility
{
    string Name { get; }
    float Cooldown { get; }
    bool CanUse(AbilityContext context);
    void Execute(AbilityContext context);
}

// Adding a new ability = new class, no changes to existing code
public class FireballAbility : IAbility { /* ... */ }
public class TeleportAbility : IAbility { /* ... */ }
public class ShieldAbility : IAbility { /* ... */ }

// Ability system doesn't need to know about specific abilities
public partial class AbilitySystem : Node
{
    private List<IAbility> _abilities = new();
    
    public void UseAbility(int index, AbilityContext context)
    {
        var ability = _abilities[index];
        if (ability.CanUse(context))
            ability.Execute(context);
    }
}
```

### L - Liskov Substitution Principle

**Definition**: Subtypes must be substitutable for their base types.

**In Games**: Any enemy can be treated as an enemy; any item as an item.

```csharp
// ❌ BAD: Violates LSP
public class Weapon : Item
{
    public virtual void Attack() { /* swing weapon */ }
}

public class Shield : Weapon  // Shield isn't really a weapon!
{
    public override void Attack() 
    {
        throw new NotSupportedException();  // LSP violation!
    }
}

// ✅ GOOD: Proper hierarchy
public abstract class Item { /* base item behavior */ }
public abstract class Equipment : Item { /* equippable behavior */ }
public class Weapon : Equipment { public void Attack() { } }
public class Shield : Equipment { public void Block() { } }
```

### I - Interface Segregation Principle

**Definition**: Clients shouldn't be forced to depend on interfaces they don't use.

**In Games**: Small, focused interfaces for entity capabilities.

```csharp
// ❌ BAD: Fat interface
public interface IEntity
{
    void TakeDamage(int amount);
    void Heal(int amount);
    void Move(Vector2 direction);
    void Attack();
    void OpenInventory();
    void StartDialogue();
    void SaveState();
}

// ✅ GOOD: Segregated interfaces
public interface IDamageable
{
    void TakeDamage(int amount);
}

public interface IHealable
{
    void Heal(int amount);
}

public interface IMoveable
{
    void Move(Vector2 direction);
}

public interface IInteractable
{
    void Interact(Node interactor);
}

// Entities implement only what they need
public partial class Crate : StaticBody2D, IDamageable
{
    public void TakeDamage(int amount) { /* break crate */ }
}

public partial class Player : CharacterBody2D, IDamageable, IHealable, IMoveable
{
    // Implements all three
}
```

### D - Dependency Inversion Principle

**Definition**: High-level modules shouldn't depend on low-level modules; both should depend on abstractions.

**In Games**: Game systems depend on interfaces, not concrete implementations.

```csharp
// ❌ BAD: Combat system depends on concrete types
public partial class CombatSystem : Node
{
    public void ProcessAttack(Player attacker, Goblin target)
    {
        target.TakeDamage(attacker.GetDamage());
    }
}

// ✅ GOOD: Combat system depends on abstractions
public partial class CombatSystem : Node
{
    public void ProcessAttack(IAttacker attacker, IDamageable target)
    {
        var damage = attacker.CalculateDamage();
        target.TakeDamage(damage);
    }
}
```

---

## KISS - Keep It Simple, Stupid

### Definition

Most systems work best if they are kept simple rather than made complicated.

### Game Development Application

Games are inherently complex. Code complexity should be minimized to leave room for game complexity.

```csharp
// ❌ BAD: Over-engineered for a simple platformer
public class MovementStrategyFactoryProvider
{
    public IMovementStrategyFactory CreateFactory(MovementConfig config)
    {
        return config.Type switch
        {
            MovementType.Ground => new GroundMovementStrategyFactory(config),
            MovementType.Flying => new FlyingMovementStrategyFactory(config),
            _ => throw new InvalidOperationException()
        };
    }
}

// ✅ GOOD: Direct, readable, works
public partial class PlayerMovement : CharacterBody2D
{
    [Export] private float _speed = 200f;
    [Export] private float _jumpForce = 400f;
    
    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;
        
        // Gravity
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        
        // Jump
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = -_jumpForce;
        
        // Horizontal movement
        var direction = Input.GetAxis("move_left", "move_right");
        velocity.X = direction * _speed;
        
        Velocity = velocity;
        MoveAndSlide();
    }
}
```

### KISS Guidelines

1. **Start simple, add complexity only when needed**
2. **Prefer linear code over branching** when possible
3. **Avoid abstractions until patterns emerge**
4. **If you can't explain it in one sentence, simplify it**
5. **Clever code is bad code** - write obvious code

---

## YAGNI - You Aren't Gonna Need It

### Definition

Don't implement something until it is necessary.

### Game Development Application

Games change constantly during development. Building for hypothetical futures wastes time and creates maintenance burden.

```csharp
// ❌ BAD: Building for features not yet needed
public class Inventory
{
    private List<Item> _items;
    private INetworkSyncManager _networkSync;  // No multiplayer planned
    private ICloudSaveProvider _cloudSave;     // Not implementing cloud saves
    private IModdingInterface _modInterface;   // No mod support planned
    
    // 500 lines of code for features that might never ship
}

// ✅ GOOD: Build what you need now
public class Inventory
{
    private List<Item> _items = new();
    private int _maxSlots;
    
    public bool TryAddItem(Item item) { /* implementation */ }
    public bool TryRemoveItem(Item item) { /* implementation */ }
    public Item? GetItem(int slot) { /* implementation */ }
}
```

### YAGNI Guidelines

1. **Implement features when you have an actual use case**
2. **Delete commented-out code** - version control exists
3. **Avoid "just in case" parameters and options**
4. **Build vertical slices** - complete feature, not horizontal infrastructure
5. **If adding "for later," question whether later will ever come**

---

## Composition Over Inheritance

### Definition

Favor object composition over class inheritance for code reuse.

### Game Development Application

This is especially important in games. Godot's node system is inherently compositional—embrace it.

```csharp
// ❌ BAD: Deep inheritance hierarchy
public class Entity : CharacterBody2D { }
public class LivingEntity : Entity { }
public class CombatEntity : LivingEntity { }
public class HumanoidEntity : CombatEntity { }
public class ArmedHumanoidEntity : HumanoidEntity { }
public class Player : ArmedHumanoidEntity { }  // 6 levels deep!

// What if you need a turret that has combat but isn't living?
// What if you need a ghost that's humanoid but has no combat collision?
```

```csharp
// ✅ GOOD: Composition via nodes
// Player.tscn
// ├── CharacterBody2D (root)
// │   ├── HealthComponent.tscn
// │   ├── CombatComponent.tscn
// │   ├── MovementComponent.cs
// │   └── InventoryComponent.tscn

// Turret.tscn - combat but no health or movement
// ├── StaticBody2D (root)
// │   └── CombatComponent.tscn

// Ghost.tscn - movement and health but no collision
// ├── Node2D (root)
// │   ├── HealthComponent.tscn
// │   └── FloatingMovementComponent.cs
```

### When Inheritance Makes Sense

- Inheriting from Godot base classes (`CharacterBody2D`, `Resource`, etc.)
- True "is-a" relationships (`RifleBullet` IS A `Bullet`)
- Small hierarchies (max 2-3 levels)

### Composition Implementation Pattern

```csharp
// Component defines behavior
public partial class HitboxComponent : Area2D
{
    [Export] public int Damage { get; set; } = 10;
    
    [Signal] public delegate void HitEventHandler(IDamageable target);
    
    private void OnBodyEntered(Node2D body)
    {
        if (body is IDamageable damageable)
        {
            damageable.TakeDamage(Damage);
            EmitSignal(SignalName.Hit, damageable);
        }
    }
}

// Entity uses component, doesn't inherit behavior
public partial class Enemy : CharacterBody2D
{
    [Export] private HitboxComponent _hitbox;
    [Export] private HealthComponent _health;
    
    public override void _Ready()
    {
        _health.Died += OnDied;
        _hitbox.Hit += OnHitSomething;
    }
}
```

---

## Separation of Concerns

### Definition

Software should be divided into distinct sections, each addressing a separate concern.

### The Three Pillars in Games

1. **Data**: What exists (stats, configuration, state)
2. **Logic**: How things behave (game rules, systems)
3. **Presentation**: How things appear (visuals, audio, UI)

```csharp
// ❌ BAD: Everything mixed together
public partial class Player : CharacterBody2D
{
    public void TakeDamage(int amount)
    {
        _health -= amount;
        
        // Logic mixed with presentation
        GetNode<AnimationPlayer>("AnimPlayer").Play("hurt");
        GetNode<AudioStreamPlayer>("HurtSound").Play();
        GetNode<Label>("../UI/HealthLabel").Text = $"HP: {_health}";
        GetNode<ProgressBar>("../UI/HealthBar").Value = _health;
        
        if (_health <= 0)
        {
            GetNode<AnimationPlayer>("AnimPlayer").Play("death");
            GetNode<AudioStreamPlayer>("DeathSound").Play();
            // ... more presentation in logic
        }
    }
}
```

```csharp
// ✅ GOOD: Separated concerns
// DATA: HealthComponent stores state
public partial class HealthComponent : Node
{
    public int CurrentHealth { get; private set; }
    [Signal] public delegate void HealthChangedEventHandler(int current, int max);
    [Signal] public delegate void DiedEventHandler();
    
    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        if (CurrentHealth <= 0)
            EmitSignal(SignalName.Died);
    }
}

// PRESENTATION: UI observes and reacts
public partial class HealthBar : ProgressBar
{
    [Export] private HealthComponent _health;
    
    public override void _Ready()
    {
        _health.HealthChanged += UpdateDisplay;
    }
    
    private void UpdateDisplay(int current, int max)
    {
        Value = (float)current / max * 100;
    }
}

// PRESENTATION: Visual effects react to events
public partial class PlayerVisuals : Node
{
    [Export] private HealthComponent _health;
    [Export] private AnimationPlayer _animator;
    [Export] private AudioStreamPlayer _hurtSound;
    
    public override void _Ready()
    {
        _health.HealthChanged += (cur, max) => {
            _animator.Play("hurt");
            _hurtSound.Play();
        };
    }
}
```

### Signal Flow for Separation

```
DATA (emits signals)
    │
    ├──▶ LOGIC (processes, emits results)
    │        │
    │        └──▶ More DATA changes
    │
    └──▶ PRESENTATION (observes, displays)
             │
             └──▶ (Never modifies game state)
```

---

## TDD - Test-Driven Development

### Definition

Write tests before writing the code that makes them pass.

### Game Development Application

Full TDD is impractical for all game code, but valuable for complex systems.

### What to Test

| Test | Don't Test |
|------|------------|
| Inventory add/remove/stack logic | "Does the animation look good?" |
| State machine transitions | Visual effects |
| Damage calculations | Physics "feel" |
| Save/load serialization | Input handling |
| Pathfinding algorithms | UI layout |
| Procedural generation rules | Audio mixing |
| Economy/balance systems | "Is it fun?" |

### TDD Cycle for Game Systems

```csharp
// 1. RED: Write a failing test
[TestCase]
public void Inventory_AddItem_IncreasesCount()
{
    var inventory = new Inventory(maxSlots: 5);
    var item = new TestItem("sword");
    
    inventory.Add(item);
    
    AssertInt(inventory.ItemCount).IsEqual(1);  // Fails: Inventory doesn't exist
}

// 2. GREEN: Write minimal code to pass
public class Inventory
{
    private List<Item> _items = new();
    public int ItemCount => _items.Count;
    
    public void Add(Item item) => _items.Add(item);
}

// 3. REFACTOR: Improve while keeping tests green
public class Inventory
{
    private readonly Item?[] _slots;
    public int ItemCount => _slots.Count(s => s != null);
    
    public bool TryAdd(Item item)
    {
        var emptySlot = Array.FindIndex(_slots, s => s == null);
        if (emptySlot == -1) return false;
        _slots[emptySlot] = item;
        return true;
    }
}
```

### Test Organization

```
Tests/
├── Unit/
│   ├── Inventory/
│   │   ├── InventoryAddTests.cs
│   │   ├── InventoryRemoveTests.cs
│   │   └── InventoryStackTests.cs
│   ├── Combat/
│   │   ├── DamageCalculationTests.cs
│   │   └── CriticalHitTests.cs
│   └── StateMachine/
│       └── StateTransitionTests.cs
└── Integration/
    ├── SaveLoadTests.cs
    └── ItemPickupTests.cs
```

---

## Fail Fast

### Definition

If something is wrong, fail immediately and obviously.

### Game Development Application

Silent failures create hard-to-debug states. Crash early with clear messages.

```csharp
// ❌ BAD: Silent failure
public void EquipWeapon(WeaponData? weapon)
{
    if (weapon == null) return;  // Silent failure - why didn't it equip?
    
    _currentWeapon = weapon;
}

// ✅ GOOD: Fail fast in debug, graceful in release
public void EquipWeapon(WeaponData? weapon)
{
    if (weapon == null)
    {
        GD.PushError("Attempted to equip null weapon");
        Debug.Assert(weapon != null, "Weapon cannot be null");
        return;  // Still graceful in release builds
    }
    
    _currentWeapon = weapon;
}
```

### Assertion Patterns

```csharp
public partial class HealthComponent : Node
{
    [Export] public int MaxHealth { get; set; } = 100;
    
    public override void _Ready()
    {
        // Fail fast on invalid configuration
        Debug.Assert(MaxHealth > 0, $"MaxHealth must be positive, got {MaxHealth}");
        Debug.Assert(GetParent() != null, "HealthComponent must have a parent");
    }
}
```

### Null Handling Strategies

```csharp
// Required dependencies - fail if missing
[Export] private HealthComponent _health = null!;  // null! = "trust me, it'll be set"

public override void _Ready()
{
    // Explicit check with clear error
    if (_health == null)
    {
        GD.PushError($"{Name}: HealthComponent export not assigned!");
        QueueFree();
        return;
    }
}

// Optional dependencies - handle gracefully
[Export] private AudioStreamPlayer? _hitSound;

private void OnHit()
{
    _hitSound?.Play();  // No sound if not configured, but not an error
}
```

---

## Law of Demeter

### Definition

A method should only call methods on: itself, its parameters, objects it creates, or its direct component objects.

### Game Development Application

Avoid reaching through objects. Use signals and direct references.

```csharp
// ❌ BAD: Law of Demeter violations
public void AttackNearestEnemy()
{
    // Reaching through multiple layers
    var damage = _player.Equipment.Weapon.Stats.BaseDamage;
    var enemy = GetTree().GetRoot().GetNode("World/Enemies").GetChild(0);
    var health = ((Enemy)enemy).Components.GetComponent<HealthComponent>();
    health.TakeDamage(damage);
}

// ✅ GOOD: Respecting Law of Demeter
public void AttackNearestEnemy()
{
    // Ask direct collaborators for what we need
    var damage = _combatStats.GetAttackDamage();  // Encapsulated
    var enemy = _targetingSystem.GetNearestEnemy();  // Injected dependency
    
    if (enemy is IDamageable damageable)
        damageable.TakeDamage(damage);
}
```

### Signals Enable Demeter-Friendly Communication

```csharp
// Components don't reach into the world
public partial class HealthComponent : Node
{
    [Signal] public delegate void DiedEventHandler();
    
    private void CheckDeath()
    {
        if (_health <= 0)
            EmitSignal(SignalName.Died);  // Just announce, don't orchestrate
    }
}

// Parent coordinates
public partial class Enemy : CharacterBody2D
{
    [Export] private HealthComponent _health;
    
    public override void _Ready()
    {
        _health.Died += HandleDeath;
    }
    
    private void HandleDeath()
    {
        EmitSignal(SignalName.EnemyDefeated, this);  // Signal up
        QueueFree();
    }
}
```

---

## Command-Query Separation

### Definition

Methods should either perform an action (command) OR return data (query), not both.

### Game Development Application

Makes code predictable and easier to reason about.

```csharp
// ❌ BAD: Combined command-query
public int TakeDamage(int amount)
{
    _health -= amount;        // Command: modifies state
    return _health;           // Query: returns value
}

public Item PopRandomItem()
{
    var index = _rng.Next(_items.Count);
    var item = _items[index];  // Query
    _items.RemoveAt(index);    // Command
    return item;               // Returning from command!
}
```

```csharp
// ✅ GOOD: Separated
// Commands (return void or success/failure)
public void TakeDamage(int amount) => _health -= amount;
public bool TryRemoveItem(Item item) => _items.Remove(item);

// Queries (no side effects)
public int GetHealth() => _health;
public Item? GetRandomItem() => _items.Count > 0 ? _items[_rng.Next(_items.Count)] : null;

// Usage
var item = inventory.GetRandomItem();
if (item != null && inventory.TryRemoveItem(item))
{
    DropItem(item);
}
```

### Exception: Fluent APIs

Builder patterns and fluent interfaces can return `this` for chaining:

```csharp
public class DialogueBuilder
{
    private List<string> _lines = new();
    
    public DialogueBuilder AddLine(string line)
    {
        _lines.Add(line);
        return this;  // Acceptable exception for fluent API
    }
    
    public Dialogue Build() => new Dialogue(_lines);
}

// Usage
var dialogue = new DialogueBuilder()
    .AddLine("Hello, traveler.")
    .AddLine("What brings you here?")
    .Build();
```

---

## Principle of Least Astonishment

### Definition

A component should behave in a way that most users would expect; the behavior should not astonish or surprise users.

### Game Development Application

Methods do what their names say. No hidden side effects.

```csharp
// ❌ BAD: Surprising behavior
public void SetHealth(int value)
{
    _health = value;
    
    // Surprise! Also does these things:
    UpdateUI();
    PlaySound(_health < _previousHealth ? "hurt" : "heal");
    CheckAchievements();
    AutoSave();
    
    if (_health <= 0)
    {
        DropAllItems();
        RespawnAtCheckpoint();
        NotifyLeaderboard();
    }
}

// ✅ GOOD: Predictable behavior
public void SetHealth(int value)
{
    var previousHealth = _health;
    _health = Mathf.Clamp(value, 0, _maxHealth);
    
    if (_health != previousHealth)
        EmitSignal(SignalName.HealthChanged, _health, _maxHealth);
    
    if (_health <= 0 && previousHealth > 0)
        EmitSignal(SignalName.Died);
    
    // Listeners handle their own responses - no surprises here
}
```

### Naming Guidelines

| Name | Expected Behavior |
|------|-------------------|
| `GetX()` | Returns X, no side effects |
| `SetX(value)` | Sets X, maybe emits change signal |
| `TryX()` | Attempts X, returns bool, no exception |
| `CanX()` | Returns bool, no side effects |
| `DoX()` / `ExecuteX()` | Performs action, may have side effects |
| `HandleX()` | Responds to event X |
| `OnX()` | Callback for event X |

---

## Defensive Programming

### Definition

Write code that anticipates and handles potential problems before they become bugs.

### Game Development Application

Players will do unexpected things. External data can be corrupted. Network can fail.

```csharp
public partial class SaveSystem : Node
{
    public PlayerSaveData? LoadPlayerData(string savePath)
    {
        // Defensive: Check file exists
        if (!FileAccess.FileExists(savePath))
        {
            GD.Print($"Save file not found: {savePath}");
            return null;
        }
        
        try
        {
            using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
            
            // Defensive: Check file opened successfully
            if (file == null)
            {
                GD.PushError($"Could not open save file: {savePath}");
                return null;
            }
            
            var json = file.GetAsText();
            var data = JsonSerializer.Deserialize<PlayerSaveData>(json);
            
            // Defensive: Validate loaded data
            if (data == null || !data.IsValid())
            {
                GD.PushError("Save data is invalid or corrupted");
                return null;
            }
            
            // Defensive: Sanitize/migrate data
            data = MigrateIfNeeded(data);
            data.Sanitize();
            
            return data;
        }
        catch (JsonException ex)
        {
            GD.PushError($"Save file parse error: {ex.Message}");
            return null;
        }
    }
}
```

### Defensive Patterns

**Input Validation**
```csharp
public void SetLevel(int level)
{
    // Clamp to valid range
    _level = Mathf.Clamp(level, 1, MaxLevel);
}
```

**Null Coalescing**
```csharp
var name = player?.Name ?? "Unknown Player";
var weapon = _equipment.GetWeapon() ?? _defaultWeapon;
```

**Default Values**
```csharp
public partial class EnemyConfig : Resource
{
    [Export] public float MoveSpeed { get; set; } = 100f;  // Always has default
    [Export] public int Health { get; set; } = 50;
    [Export] public Color TintColor { get; set; } = Colors.White;
}
```

---

## Idempotency

### Definition

An operation is idempotent if performing it multiple times has the same effect as performing it once.

### Game Development Application

Initialization, state setting, and save/load should be idempotent.

```csharp
// ❌ BAD: Not idempotent
public override void _Ready()
{
    _health += MaxHealth;  // Calling _Ready twice doubles health!
    EventBus.PlayerSpawned += OnPlayerSpawned;  // Multiple subscriptions!
}

// ✅ GOOD: Idempotent
public override void _Ready()
{
    _health = MaxHealth;  // Always sets to same value
    
    // Unsubscribe first to prevent duplicate handlers
    EventBus.PlayerSpawned -= OnPlayerSpawned;
    EventBus.PlayerSpawned += OnPlayerSpawned;
}
```

### Idempotent State Setting

```csharp
public void SetState(GameState newState)
{
    if (_currentState == newState)
        return;  // No-op if already in state
    
    _currentState?.Exit();
    _currentState = newState;
    _currentState.Enter();
}
```

### Idempotent Initialization Pattern

```csharp
private bool _initialized;

public void Initialize()
{
    if (_initialized) return;
    
    // One-time setup
    SetupConnections();
    LoadResources();
    
    _initialized = true;
}
```

---

## Database Normalization (for Game Data)

### Definition

Organize data to reduce redundancy and improve integrity.

### Game Development Application

Single source of truth for game data. Reference by ID, don't duplicate.

```csharp
// ❌ BAD: Denormalized - data duplicated
public class Weapon
{
    public string Name = "Iron Sword";
    public int Damage = 10;
    public string MaterialName = "Iron";
    public int MaterialHardness = 5;  // Duplicated if multiple iron items
}

public class Armor
{
    public string Name = "Iron Chestplate";
    public int Defense = 15;
    public string MaterialName = "Iron";
    public int MaterialHardness = 5;  // Same iron data duplicated!
}
```

```csharp
// ✅ GOOD: Normalized - reference shared data
// MaterialData.tres resources
public partial class MaterialData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public int Hardness { get; set; }
    [Export] public Color Tint { get; set; }
}

// Items reference material by ID or direct Resource reference
public partial class WeaponData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public int BaseDamage { get; set; }
    [Export] public MaterialData Material { get; set; }  // Reference, not copy
}
```

### Normalization in Save Data

```csharp
// ❌ BAD: Full item data in inventory
{
    "inventory": [
        { "id": "iron_sword", "name": "Iron Sword", "damage": 10, "material": "iron", "icon": "..." },
        { "id": "iron_sword", "name": "Iron Sword", "damage": 10, "material": "iron", "icon": "..." }
    ]
}

// ✅ GOOD: Only instance data, reference definitions
{
    "inventory": [
        { "itemId": "iron_sword", "durability": 95, "enchantments": [] },
        { "itemId": "iron_sword", "durability": 42, "enchantments": ["fire"] }
    ]
}

// At load time, resolve itemId to full ItemData from ResourceRegistry
```

---

## Premature Optimization is the Root of All Evil

### Definition

Don't optimize until you've measured and identified actual bottlenecks.

### Game Development Application

Make it work, make it right, make it fast—in that order.

```csharp
// ❌ BAD: Premature optimization
public class SpatialHashGrid  // Complex spatial partitioning
{
    // 200 lines of optimized code for a game with 10 enemies
}

// Developer spent 2 days on this.
// Actual game has max 50 entities.
// A simple list with O(n) lookup would have been fine.
// And the bottleneck turned out to be shader rendering anyway.
```

```csharp
// ✅ GOOD: Simple first, optimize when needed
public class EntityFinder
{
    private List<Entity> _entities = new();
    
    public Entity? FindNearest(Vector2 position)
    {
        // Simple O(n) scan - fine for most games
        return _entities
            .OrderBy(e => e.GlobalPosition.DistanceTo(position))
            .FirstOrDefault();
    }
}

// Later, IF profiling shows this is slow:
// 1. Measure actual performance
// 2. Identify if this is really the bottleneck
// 3. Consider alternatives (spatial hash, quadtree, etc.)
// 4. Implement simplest optimization that solves the problem
```

### The Optimization Process

1. **Make it work**: Get the feature functioning
2. **Make it right**: Refactor for clarity and maintainability
3. **Profile**: Use Godot's profiler to find actual bottlenecks
4. **Optimize surgically**: Only optimize proven hot spots
5. **Measure again**: Verify optimization actually helped

### Known Valid Optimizations

These patterns are almost always worth doing:

| Pattern | When |
|---------|------|
| Object pooling | Frequently spawned objects (bullets, particles) |
| Physics layers | Limiting collision checks |
| LOD | Large open worlds |
| Texture atlases | Many small sprites |
| `SetProcess(false)` | Disabling inactive objects |

---

## Principle Conflicts & Trade-offs

### DRY vs. KISS

**Conflict**: Eliminating duplication can create complex abstractions.

**Resolution**: Rule of Three. Don't abstract until you've seen the pattern 3 times. A little duplication is better than a wrong abstraction.

```csharp
// If you have similar code in 2 places: wait
// If you have similar code in 3 places: consider abstracting
// But only if the abstraction is simpler than the duplication
```

### YAGNI vs. Architecture

**Conflict**: Good architecture requires some upfront thinking about extensibility.

**Resolution**: Build solid foundations for core systems (save/load, state management, event system). Keep feature implementations scrappy and refactorable.

```
Core Systems: Invest in architecture
├── EventBus - well-designed from start
├── SaveSystem - handles migration from start
└── StateMachine - flexible state pattern

Feature Code: YAGNI applies
├── WeaponSwing - just make it work
├── EnemyAI - start simple, iterate
└── Dialogue - build what you need now
```

### Composition vs. Inheritance

**Conflict**: Sometimes inheritance genuinely models the domain better.

**Resolution**: Use inheritance for true "is-a" (Enemy IS a CharacterBody2D). Use composition for "has-a" and capabilities. Max 3 inheritance levels.

### Performance vs. Clean Code

**Conflict**: Sometimes optimized code is harder to read.

**Resolution**: Profile first. If optimization is needed, optimize surgically and document why the complex code exists.

```csharp
// OPTIMIZED: Using bit manipulation for performance
// Profiling showed this is called 10,000+ times per frame
// Original readable version was causing 15ms frame spikes
// See benchmark results in docs/optimization-notes.md
private int OptimizedOperation(int value)
{
    return (value >> 2) | ((value & 0x3) << 30);
}
```

---

## Quick Reference Card

| Situation | Apply |
|-----------|-------|
| Writing similar code again | DRY - extract component |
| Method doing multiple things | SRP - split it |
| Deep `if` nesting | KISS - flatten or extract |
| Building for "what if" | YAGNI - build for now |
| `A.B.C.D.Method()` | Law of Demeter - add intermediary |
| Method returns AND modifies | CQS - separate them |
| Method has surprise side effects | Least Astonishment - make obvious |
| External data input | Defensive - validate it |
| Initialization code | Idempotent - safe to re-run |
| Same data in multiple places | Normalize - single source of truth |
| "This might be slow" | No Premature Optimization - profile first |
| Need shared behavior | Composition - use components |
| Complex game logic | TDD - write tests first |
| Something seems wrong | Fail Fast - crash with message |

---

*This document should be referenced whenever making architectural decisions. When in doubt, start simple and evolve.*