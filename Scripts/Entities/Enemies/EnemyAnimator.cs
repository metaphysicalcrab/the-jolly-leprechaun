using Godot;
using System.Collections.Generic;
using TheJollyLeprechaun.Core.Base;

namespace TheJollyLeprechaun.Entities.Enemies;

/// <summary>
/// Controls enemy animations based on state and movement.
/// Listens to state machine changes and velocity.
/// </summary>
public partial class EnemyAnimator : Node
{
    [ExportGroup("Animation Names")]
    [Export] private string _idleAnimation = "Idle";
    [Export] private string _walkAnimation = "Walk";
    [Export] private string _runAnimation = "Run";
    [Export] private string _sprintAnimation = "Run"; // Use Run if no sprint
    [Export] private string _jumpAnimation = "TakeOff";
    [Export] private string _scaredAnimation = "Run"; // Use Run for scared fleeing

    [ExportGroup("Bird Flight Animations")]
    [Export] private string _flyingAnimation = "Flying";
    [Export] private string _takeoffAnimation = "TakeOff";
    [Export] private string _landingAnimation = "Landing";

    [ExportGroup("References")]
    [Export] private NodePath _animationPlayerPath = "";
    [Export] private NodePath _enemyPath = "..";
    [Export] private NodePath _stateMachinePath = "../StateMachine";

    [ExportGroup("Settings")]
    [Export] private float _walkThreshold = 0.1f;
    [Export] private float _runThreshold = 2.0f;
    [Export] private float _sprintThreshold = 4.0f;

    private AnimationPlayer? _animationPlayer;
    private BaseEnemy? _enemy;
    private BirdEnemy? _birdEnemy; // For flight animation handling
    private StateMachine? _stateMachine;
    private string _currentAnimation = "";
    private string _currentState = "";
    private string _flightPhase = "grounded"; // grounded, takeoff, flying, landing

    // Resolved animation names (mapped from available animations)
    private readonly Dictionary<string, string> _resolvedAnimations = new();

    public override void _Ready()
    {
        _enemy = GetNodeOrNull<BaseEnemy>(_enemyPath);
        _stateMachine = GetNodeOrNull<StateMachine>(_stateMachinePath);

        // Subscribe to state changes
        if (_stateMachine != null)
        {
            _stateMachine.StateChanged += OnStateChanged;
        }

        // Check if this is a bird enemy and subscribe to flight events
        if (_enemy is BirdEnemy bird)
        {
            _birdEnemy = bird;
            _birdEnemy.FlightStateChanged += OnFlightStateChanged;
            GD.Print($"EnemyAnimator ({_enemy?.Name}): Subscribed to bird flight events");
        }

        // Defer AnimationPlayer search to ensure parent nodes are ready
        // BaseEnemy.Model won't be set until BaseEnemy._Ready() runs
        CallDeferred(nameof(InitializeAnimationPlayer));
    }

    private void InitializeAnimationPlayer()
    {
        // Try to find AnimationPlayer in the model hierarchy
        if (!string.IsNullOrEmpty(_animationPlayerPath))
        {
            _animationPlayer = GetNodeOrNull<AnimationPlayer>(_animationPlayerPath);
        }

        if (_animationPlayer == null && _enemy?.Model != null)
        {
            // Recursively search model hierarchy for AnimationPlayer
            _animationPlayer = FindAnimationPlayerRecursive(_enemy.Model);

            // Debug: Print model hierarchy to understand structure
            if (_animationPlayer == null)
            {
                GD.Print($"EnemyAnimator ({_enemy.Name}): Model hierarchy:");
                PrintNodeHierarchy(_enemy.Model, "  ");
            }
        }

        if (_animationPlayer != null)
        {
            var animList = _animationPlayer.GetAnimationList();
            GD.Print($"EnemyAnimator ({_enemy?.Name}): Found AnimationPlayer with {animList.Length} animations: {string.Join(", ", animList)}");

            // Resolve animation names
            ResolveAnimationNames(animList);
        }
        else
        {
            GD.Print($"EnemyAnimator ({_enemy?.Name}): No AnimationPlayer found - animations disabled");

            // Additional debug: check if Model is null
            if (_enemy?.Model == null)
            {
                GD.PrintErr($"EnemyAnimator ({_enemy?.Name}): Enemy Model is null!");
            }
        }
    }

    public override void _ExitTree()
    {
        if (_stateMachine != null)
        {
            _stateMachine.StateChanged -= OnStateChanged;
        }

        if (_birdEnemy != null)
        {
            _birdEnemy.FlightStateChanged -= OnFlightStateChanged;
        }
    }

    private void OnFlightStateChanged(bool isFlying, string flightPhase)
    {
        _flightPhase = flightPhase;
        UpdateAnimation();
    }

    /// <summary>
    /// Recursively search for an AnimationPlayer in the node hierarchy.
    /// </summary>
    private AnimationPlayer? FindAnimationPlayerRecursive(Node node, int depth = 0)
    {
        if (depth > 10) return null; // Prevent infinite recursion

        foreach (var child in node.GetChildren())
        {
            if (child is AnimationPlayer ap)
            {
                return ap;
            }

            var found = FindAnimationPlayerRecursive(child, depth + 1);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Print node hierarchy for debugging.
    /// </summary>
    private void PrintNodeHierarchy(Node node, string indent)
    {
        foreach (var child in node.GetChildren())
        {
            GD.Print($"{indent}{child.Name} ({child.GetType().Name})");
            if (indent.Length < 10) // Limit depth
            {
                PrintNodeHierarchy(child, indent + "  ");
            }
        }
    }

    /// <summary>
    /// Try to find matching animations from the available list.
    /// </summary>
    private void ResolveAnimationNames(string[] availableAnimations)
    {
        var namesToFind = new Dictionary<string, string>
        {
            { "idle", _idleAnimation },
            { "walk", _walkAnimation },
            { "run", _runAnimation },
            { "sprint", _sprintAnimation },
            { "jump", _jumpAnimation },
            { "scared", _scaredAnimation }
        };

        // Add bird flight animations if this is a bird enemy
        if (_enemy is BirdEnemy)
        {
            namesToFind.Add("flying", _flyingAnimation);
            namesToFind.Add("takeoff", _takeoffAnimation);
            namesToFind.Add("landing", _landingAnimation);
        }

        foreach (var (key, targetName) in namesToFind)
        {
            var resolved = FindMatchingAnimation(targetName, availableAnimations);
            if (!string.IsNullOrEmpty(resolved))
            {
                _resolvedAnimations[key] = resolved;
                GD.Print($"EnemyAnimator ({_enemy?.Name}): Mapped '{key}' -> '{resolved}'");

                // Enable looping for movement and flying animations
                if (key is "idle" or "walk" or "run" or "sprint" or "scared" or "flying")
                {
                    EnableAnimationLooping(resolved);
                }
            }
            else
            {
                GD.Print($"EnemyAnimator ({_enemy?.Name}): No animation found for '{key}' (searched for '{targetName}')");
            }
        }
    }

    private void EnableAnimationLooping(string animationName)
    {
        if (_animationPlayer == null) return;

        var animation = _animationPlayer.GetAnimation(animationName);
        if (animation != null && animation.LoopMode == Animation.LoopModeEnum.None)
        {
            animation.LoopMode = Animation.LoopModeEnum.Linear;
            GD.Print($"EnemyAnimator ({_enemy?.Name}): Enabled looping for '{animationName}'");
        }
    }

    private string? FindMatchingAnimation(string targetName, string[] available)
    {
        var lowerTarget = targetName.ToLowerInvariant();

        foreach (var anim in available)
        {
            var lowerAnim = anim.ToLowerInvariant();

            // Exact match (case-insensitive)
            if (lowerAnim == lowerTarget)
                return anim;

            // Check if animation ends with the target
            if (lowerAnim.EndsWith(lowerTarget))
                return anim;

            // Check if animation contains the target
            if (lowerAnim.Contains(lowerTarget))
                return anim;
        }

        return null;
    }

    public override void _Process(double delta)
    {
        if (_animationPlayer == null || _enemy == null) return;

        UpdateAnimation();
    }

    private void OnStateChanged(string previousState, string newState)
    {
        _currentState = newState;

        // Immediately update animation based on new state
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (_enemy == null || _animationPlayer == null) return;

        // Handle bird flight animations first
        if (_birdEnemy != null && _flightPhase != "grounded")
        {
            string flightAnimKey = _flightPhase switch
            {
                "takeoff" => "takeoff",
                "flying" => "flying",
                "landing" => "landing",
                _ => "flying" // Fallback to flying
            };

            PlayAnimationByKey(flightAnimKey);
            return;
        }

        var velocity = _enemy.Velocity;
        var horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();

        string targetKey;

        // Check current state for special animations
        if (_currentState.Contains("Scared"))
        {
            // Use sprint/run animation for scared fleeing
            targetKey = horizontalSpeed > _sprintThreshold ? "sprint" :
                       horizontalSpeed > _runThreshold ? "run" : "scared";
        }
        else if (_currentState.Contains("Chase"))
        {
            // Chasing - use faster animations
            targetKey = horizontalSpeed > _sprintThreshold ? "sprint" :
                       horizontalSpeed > _runThreshold ? "run" :
                       horizontalSpeed > _walkThreshold ? "walk" : "idle";
        }
        else if (_currentState.Contains("Patrol"))
        {
            // Patrolling - use walk
            targetKey = horizontalSpeed > _walkThreshold ? "walk" : "idle";
        }
        else
        {
            // Idle or other states
            targetKey = horizontalSpeed > _sprintThreshold ? "sprint" :
                       horizontalSpeed > _runThreshold ? "run" :
                       horizontalSpeed > _walkThreshold ? "walk" : "idle";
        }

        PlayAnimationByKey(targetKey);
    }

    private void PlayAnimationByKey(string animationKey)
    {
        if (_animationPlayer == null) return;

        // Get the resolved animation name
        if (!_resolvedAnimations.TryGetValue(animationKey, out var animationName))
        {
            // Fallback to idle if animation not found
            if (animationKey != "idle" && _resolvedAnimations.TryGetValue("idle", out animationName))
            {
                // Use idle as fallback
            }
            else
            {
                return;
            }
        }

        if (animationName == _currentAnimation) return;

        _animationPlayer.Play(animationName);
        _currentAnimation = animationName;
    }
}
