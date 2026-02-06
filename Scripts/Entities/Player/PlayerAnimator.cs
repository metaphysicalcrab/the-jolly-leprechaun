using Godot;
using System.Collections.Generic;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Controls player animations based on movement state.
/// Supports normal locomotion, sneak variants, and one-shot action animations.
/// </summary>
public partial class PlayerAnimator : Node
{
    [ExportGroup("Locomotion Animations")]
    [Export] private string _idleAnimation = "idle";
    [Export] private string _walkAnimation = "walk";
    [Export] private string _runAnimation = "run";
    [Export] private string _sprintAnimation = "sprint";
    [Export] private string _jumpAnimation = "jump";
    [Export] private string _skipAnimation = "skip";

    [ExportGroup("Sneak Animations")]
    [Export] private string _sneakIdleAnimation = "sneak_idle";
    [Export] private string _sneakWalkAnimation = "sneak_walk";
    [Export] private string _sneakTiptoeAnimation = "sneak_tiptoe";
    [Export] private string _sneakRunAnimation = "sneak_run";

    [ExportGroup("Action Animations")]
    [Export] private string _doubleJumpAnimation = "double_jump";
    [Export] private string _scareAnimation = "scare";
    [Export] private string _transformAnimation = "initiate_transform";
    [Export] private string _sneakStumbleAnimation = "sneak_stumble_roll";
    [Export] private string _victoryAnimation = "victory";
    [Export] private string _victoryJumpAnimation = "victory_jump";
    [Export] private string _hurtAnimation = "hurt";
    [Export] private string _loserFalloverAnimation = "loser_fallover";

    [ExportGroup("References")]
    [Export] private NodePath _animationPlayerPath = "";
    [Export] private NodePath _bodyPath = "..";
    [Export] private NodePath _movementPath = "../PlayerMovement";

    [ExportGroup("Speed Thresholds")]
    [Export] private float _walkThreshold = 0.1f;
    [Export] private float _runThreshold = 3.0f;
    [Export] private float _sprintThreshold = 5.5f;

    [ExportGroup("Sneak Thresholds")]
    [Export] private float _sneakTiptoeThreshold = 0.4f;
    [Export] private float _sneakRunThreshold = 2.0f;

    [ExportGroup("Scare Mesh Hiding")]
    [Export] private float _scareHideDelay = 0.33f;

    [Signal] public delegate void ActionAnimationStartedEventHandler(string animationKey);
    [Signal] public delegate void ActionAnimationFinishedEventHandler(string animationKey);

    private AnimationPlayer? _animationPlayer;
    private CharacterBody3D? _body;
    private PlayerMovement? _movement;
    private string _currentAnimation = "";

    // Action animation lock - prevents movement animations from interrupting one-shot actions
    private bool _isPlayingActionAnimation;
    private string _currentActionKey = "";

    // Scare mesh hiding
    private MeshInstance3D? _shirtMesh;
    private MeshInstance3D? _hatMesh;
    private SceneTreeTimer? _scareHideTimer;

    // Resolved animation names (mapped from available animations)
    private readonly Dictionary<string, string> _resolvedAnimations = new();

    // Keys that should loop
    private static readonly HashSet<string> LoopingKeys = new()
    {
        "idle", "walk", "run", "sprint", "skip",
        "sneak_idle", "sneak_walk", "sneak_tiptoe", "sneak_run",
        "victory"
    };

    public override void _Ready()
    {
        _body = GetNodeOrNull<CharacterBody3D>(_bodyPath);
        _movement = GetNodeOrNull<PlayerMovement>(_movementPath);

        FindAnimationPlayer();

        if (_animationPlayer != null)
        {
            var animList = _animationPlayer.GetAnimationList();
            GD.Print($"PlayerAnimator: Found AnimationPlayer with {animList.Length} animations: {string.Join(", ", animList)}");

            ResolveAnimationNames(animList);

            _animationPlayer.AnimationFinished += OnAnimationFinished;
        }
        else
        {
            GD.Print("PlayerAnimator: No AnimationPlayer found - animations disabled");
        }

        if (_movement != null)
        {
            _movement.Jumped += OnJumped;
            _movement.Landed += OnLanded;
            _movement.DoubleJumped += OnDoubleJumped;
        }

        FindScareMeshes();
    }

    private void FindAnimationPlayer()
    {
        if (!string.IsNullOrEmpty(_animationPlayerPath))
        {
            _animationPlayer = GetNodeOrNull<AnimationPlayer>(_animationPlayerPath);
        }

        if (_animationPlayer == null)
        {
            _animationPlayer = GetNodeOrNull<AnimationPlayer>("../Model/LeprechaunModel/AnimationPlayer");
            _animationPlayer ??= GetNodeOrNull<AnimationPlayer>("../Model/AnimationPlayer");

            var modelNode = GetNodeOrNull<Node3D>("../Model/LeprechaunModel");
            if (modelNode != null && _animationPlayer == null)
            {
                foreach (var child in modelNode.GetChildren())
                {
                    if (child is AnimationPlayer ap)
                    {
                        _animationPlayer = ap;
                        break;
                    }
                }
            }
        }
    }

    private void FindScareMeshes()
    {
        var modelNode = GetNodeOrNull<Node3D>("../Model/LeprechaunModel");
        if (modelNode == null) return;

        _shirtMesh = FindMeshByName(modelNode, "CHAR_shirt");
        _hatMesh = FindMeshByName(modelNode, "PROP_hat");

        if (_shirtMesh != null) GD.Print("PlayerAnimator: Found CHAR_shirt mesh for scare");
        if (_hatMesh != null) GD.Print("PlayerAnimator: Found PROP_hat mesh for scare");
    }

    private static MeshInstance3D? FindMeshByName(Node root, string name)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is MeshInstance3D mesh && child.Name.ToString().Contains(name))
                return mesh;

            var found = FindMeshByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void ResolveAnimationNames(string[] availableAnimations)
    {
        var namesToFind = new Dictionary<string, string>
        {
            // Locomotion
            { "idle", _idleAnimation },
            { "walk", _walkAnimation },
            { "run", _runAnimation },
            { "sprint", _sprintAnimation },
            { "jump", _jumpAnimation },
            { "fall", _jumpAnimation },
            { "skip", _skipAnimation },
            // Sneak
            { "sneak_idle", _sneakIdleAnimation },
            { "sneak_walk", _sneakWalkAnimation },
            { "sneak_tiptoe", _sneakTiptoeAnimation },
            { "sneak_run", _sneakRunAnimation },
            // Actions
            { "double_jump", _doubleJumpAnimation },
            { "scare", _scareAnimation },
            { "initiate_transform", _transformAnimation },
            { "sneak_stumble", _sneakStumbleAnimation },
            { "victory", _victoryAnimation },
            { "victory_jump", _victoryJumpAnimation },
            { "hurt", _hurtAnimation },
            { "loser_fallover", _loserFalloverAnimation },
        };

        foreach (var (key, targetName) in namesToFind)
        {
            var resolved = FindMatchingAnimation(targetName, availableAnimations);
            if (!string.IsNullOrEmpty(resolved))
            {
                _resolvedAnimations[key] = resolved;
                GD.Print($"PlayerAnimator: Mapped '{key}' -> '{resolved}'");

                if (LoopingKeys.Contains(key))
                {
                    EnableAnimationLooping(resolved);
                }
            }
            else
            {
                GD.Print($"PlayerAnimator: No animation found for '{key}' (searched for '{targetName}')");
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
            GD.Print($"PlayerAnimator: Enabled looping for '{animationName}'");
        }
    }

    private string? FindMatchingAnimation(string targetName, string[] available)
    {
        var lowerTarget = targetName.ToLowerInvariant();

        // Pass 1: Exact match (e.g., "jump" == "jump", not "double_jump")
        foreach (var anim in available)
        {
            if (anim.ToLowerInvariant() == lowerTarget)
                return anim;
        }

        // Pass 2: Ends-with (e.g., "Armature|idle" matches "idle")
        foreach (var anim in available)
        {
            if (anim.ToLowerInvariant().EndsWith(lowerTarget))
                return anim;
        }

        // Pass 3: Contains (loose fallback)
        foreach (var anim in available)
        {
            if (anim.ToLowerInvariant().Contains(lowerTarget))
                return anim;
        }

        return null;
    }

    public override void _ExitTree()
    {
        if (_movement != null)
        {
            _movement.Jumped -= OnJumped;
            _movement.Landed -= OnLanded;
            _movement.DoubleJumped -= OnDoubleJumped;
        }

        if (_animationPlayer != null)
        {
            _animationPlayer.AnimationFinished -= OnAnimationFinished;
        }
    }

    public override void _Process(double delta)
    {
        if (_animationPlayer == null || _body == null) return;

        if (!_isPlayingActionAnimation)
        {
            UpdateAnimation();
        }
    }

    private void UpdateAnimation()
    {
        if (_body == null || _movement == null) return;

        var velocity = _body.Velocity;
        var horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
        var isOnFloor = _body.IsOnFloor();

        string targetKey;

        if (!isOnFloor)
        {
            if (_movement.JumpCount >= 2)
            {
                targetKey = "double_jump";
            }
            else
            {
                targetKey = velocity.Y > 0 ? "jump" : "fall";
            }
        }
        else if (_movement.IsSneaking)
        {
            // Tiptoe at lowest speeds, then walk, then run
            if (horizontalSpeed > _sneakRunThreshold)
            {
                targetKey = "sneak_run";
            }
            else if (horizontalSpeed > _sneakTiptoeThreshold)
            {
                targetKey = "sneak_walk";
            }
            else if (horizontalSpeed > _walkThreshold)
            {
                targetKey = "sneak_tiptoe";
            }
            else
            {
                targetKey = "sneak_idle";
            }
        }
        else if (horizontalSpeed > _sprintThreshold)
        {
            targetKey = "sprint";
        }
        else if (horizontalSpeed > _runThreshold)
        {
            targetKey = "run";
        }
        else if (horizontalSpeed > _walkThreshold)
        {
            targetKey = "walk";
        }
        else
        {
            targetKey = "idle";
        }

        PlayAnimationByKey(targetKey);
    }

    private void PlayAnimationByKey(string animationKey)
    {
        if (_animationPlayer == null) return;

        if (!_resolvedAnimations.TryGetValue(animationKey, out var animationName))
        {
            return;
        }

        if (animationName == _currentAnimation) return;

        _animationPlayer.Play(animationName);
        _currentAnimation = animationName;
    }

    /// <summary>
    /// Play a one-shot action animation that locks out movement animations until it finishes.
    /// Disables player movement for the duration.
    /// </summary>
    public void PlayActionAnimation(string key)
    {
        if (_animationPlayer == null) return;
        if (!_resolvedAnimations.TryGetValue(key, out var animationName)) return;

        _isPlayingActionAnimation = true;
        _currentActionKey = key;
        _movement?.SetEnabled(false);

        _animationPlayer.Play(animationName);
        _currentAnimation = animationName;

        EmitSignal(SignalName.ActionAnimationStarted, key);

        if (key == "scare")
        {
            StartScareHideTimer();
        }
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (!_isPlayingActionAnimation) return;

        _isPlayingActionAnimation = false;
        var finishedKey = _currentActionKey;
        _currentActionKey = "";
        _movement?.SetEnabled(true);

        if (finishedKey == "scare")
        {
            RestoreScareMeshes();
        }

        EmitSignal(SignalName.ActionAnimationFinished, finishedKey);
    }

    private void StartScareHideTimer()
    {
        _scareHideTimer = GetTree().CreateTimer(_scareHideDelay);
        _scareHideTimer.Timeout += HideScareMeshes;
    }

    private void HideScareMeshes()
    {
        if (_shirtMesh != null) _shirtMesh.Visible = false;
        if (_hatMesh != null) _hatMesh.Visible = false;
    }

    private void RestoreScareMeshes()
    {
        if (_shirtMesh != null) _shirtMesh.Visible = true;
        if (_hatMesh != null) _hatMesh.Visible = true;
    }

    private void OnJumped()
    {
        PlayAnimationByKey("jump");
    }

    private void OnDoubleJumped()
    {
        // Force play double_jump even if we're already airborne
        _currentAnimation = "";
        PlayAnimationByKey("double_jump");
    }

    private void OnLanded()
    {
        // Will be handled by UpdateAnimation on next frame
    }
}
