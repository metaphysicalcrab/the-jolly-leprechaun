using Godot;
using System.Collections.Generic;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Controls player animations based on movement state.
/// Attach to the player and configure the AnimationPlayer reference.
/// </summary>
public partial class PlayerAnimator : Node
{
    [ExportGroup("Animation Names")]
    [Export] private string _idleAnimation = "idle";
    [Export] private string _walkAnimation = "walk";
    [Export] private string _runAnimation = "run";
    [Export] private string _sprintAnimation = "sprint";
    [Export] private string _jumpAnimation = "jump";
    // Note: Using jump for fall since model doesn't have a fall animation

    [ExportGroup("References")]
    [Export] private NodePath _animationPlayerPath = "";
    [Export] private NodePath _bodyPath = "..";
    [Export] private NodePath _movementPath = "../PlayerMovement";

    [ExportGroup("Settings")]
    [Export] private float _walkThreshold = 0.1f;
    [Export] private float _runThreshold = 3.0f;
    [Export] private float _sprintThreshold = 5.5f;

    private AnimationPlayer? _animationPlayer;
    private CharacterBody3D? _body;
    private PlayerMovement? _movement;
    private string _currentAnimation = "";

    // Resolved animation names (mapped from available animations)
    private readonly Dictionary<string, string> _resolvedAnimations = new();

    public override void _Ready()
    {
        _body = GetNodeOrNull<CharacterBody3D>(_bodyPath);
        _movement = GetNodeOrNull<PlayerMovement>(_movementPath);

        // Try to find AnimationPlayer in the model hierarchy
        if (!string.IsNullOrEmpty(_animationPlayerPath))
        {
            _animationPlayer = GetNodeOrNull<AnimationPlayer>(_animationPlayerPath);
        }

        if (_animationPlayer == null)
        {
            // Try common paths for GLB imports
            _animationPlayer = GetNodeOrNull<AnimationPlayer>("../Model/LeprechaunModel/AnimationPlayer");
            _animationPlayer ??= GetNodeOrNull<AnimationPlayer>("../Model/AnimationPlayer");

            // Also try looking for AnimationTree which might have an AnimationPlayer reference
            var modelNode = GetNodeOrNull<Node3D>("../Model/LeprechaunModel");
            if (modelNode != null && _animationPlayer == null)
            {
                // Search children for AnimationPlayer
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

        if (_animationPlayer != null)
        {
            var animList = _animationPlayer.GetAnimationList();
            GD.Print($"PlayerAnimator: Found AnimationPlayer with {animList.Length} animations: {string.Join(", ", animList)}");

            // Resolve animation names - try to find matching animations
            ResolveAnimationNames(animList);
        }
        else
        {
            GD.Print("PlayerAnimator: No AnimationPlayer found - animations disabled");
        }

        // Connect to movement signals if available
        if (_movement != null)
        {
            _movement.Jumped += OnJumped;
            _movement.Landed += OnLanded;
        }
    }

    /// <summary>
    /// Try to find matching animations from the available list.
    /// Handles common naming conventions like "Armature|idle", "Idle", etc.
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
            { "fall", _jumpAnimation } // Use jump for fall since no fall animation exists
        };

        foreach (var (key, targetName) in namesToFind)
        {
            var resolved = FindMatchingAnimation(targetName, availableAnimations);
            if (!string.IsNullOrEmpty(resolved))
            {
                _resolvedAnimations[key] = resolved;
                GD.Print($"PlayerAnimator: Mapped '{key}' -> '{resolved}'");

                // Enable looping for ground-based animations
                if (key is "idle" or "walk" or "run" or "sprint")
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

    /// <summary>
    /// Enable looping for a specific animation.
    /// </summary>
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

        foreach (var anim in available)
        {
            var lowerAnim = anim.ToLowerInvariant();

            // Exact match (case-insensitive)
            if (lowerAnim == lowerTarget)
                return anim;

            // Check if animation ends with the target (e.g., "Armature|idle" matches "idle")
            if (lowerAnim.EndsWith(lowerTarget))
                return anim;

            // Check if animation contains the target
            if (lowerAnim.Contains(lowerTarget))
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
        }
    }

    public override void _Process(double delta)
    {
        if (_animationPlayer == null || _body == null) return;

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (_body == null || _animationPlayer == null) return;

        var velocity = _body.Velocity;
        var horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
        var isOnFloor = _body.IsOnFloor();

        string targetKey;

        if (!isOnFloor)
        {
            // Airborne
            targetKey = velocity.Y > 0 ? "jump" : "fall";
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

        // Get the resolved animation name
        if (!_resolvedAnimations.TryGetValue(animationKey, out var animationName))
        {
            return; // Animation not found/mapped
        }

        if (animationName == _currentAnimation) return;

        _animationPlayer.Play(animationName);
        _currentAnimation = animationName;
    }

    private void OnJumped()
    {
        PlayAnimationByKey("jump");
    }

    private void OnLanded()
    {
        // Will be handled by UpdateAnimation on next frame
    }
}
