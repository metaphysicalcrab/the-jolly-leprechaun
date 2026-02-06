using Godot;
using TheJollyLeprechaun.Core.Autoloads;
using TheJollyLeprechaun.Core.Interfaces;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Player scare ability - scares nearby enemies when activated.
/// Has a cooldown between uses.
/// </summary>
public partial class PlayerScare : Area3D
{
    [ExportGroup("Scare Settings")]
    [Export] private float _scareIntensity = 1.0f;
    [Export] private float _scareCooldown = 2.0f;
    [Export] private float _scareRadius = 4.0f;

    [ExportGroup("References")]
    [Export] private NodePath _animatorPath = "../PlayerAnimator";

    [Signal] public delegate void ScareCooldownStartedEventHandler(float duration);
    [Signal] public delegate void ScareCooldownEndedEventHandler();
    [Signal] public delegate void ScareUsedEventHandler(int enemiesScared);

    private PlayerAnimator? _animator;
    private bool _canScare = true;
    private float _cooldownRemaining;
    private bool _enabled = true;

    public bool CanScare => _canScare && _enabled;
    public float CooldownRemaining => _cooldownRemaining;
    public float CooldownDuration => _scareCooldown;
    public bool IsOnCooldown => !_canScare;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public override void _Ready()
    {
        _animator = GetNodeOrNull<PlayerAnimator>(_animatorPath);

        // Configure the collision shape radius if we have one
        var shape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (shape?.Shape is SphereShape3D sphere)
        {
            sphere.Radius = _scareRadius;
        }
    }

    public override void _Process(double delta)
    {
        // Handle cooldown
        if (!_canScare)
        {
            _cooldownRemaining -= (float)delta;
            if (_cooldownRemaining <= 0)
            {
                _canScare = true;
                _cooldownRemaining = 0;
                EmitSignal(SignalName.ScareCooldownEnded);
            }
        }

        // Check for scare input
        if (_enabled && InputManager.Instance?.IsScareJustPressed() == true)
        {
            TryScare();
        }
    }

    /// <summary>Attempt to use the scare ability.</summary>
    public bool TryScare()
    {
        if (!CanScare) return false;

        // Start cooldown
        _canScare = false;
        _cooldownRemaining = _scareCooldown;
        EmitSignal(SignalName.ScareCooldownStarted, _scareCooldown);

        // Find and scare all IScareable in range
        var scaredCount = 0;
        var overlappingBodies = GetOverlappingBodies();

        foreach (var body in overlappingBodies)
        {
            if (body is IScareable scareable && !scareable.IsScared)
            {
                scareable.Scare(this, _scareIntensity);
                scaredCount++;
                GameEvents.Instance.EmitPlayerScaredEnemy(body);
            }
        }

        EmitSignal(SignalName.ScareUsed, scaredCount);

        _animator?.PlayActionAnimation("scare");

        GD.Print($"Scare used! Scared {scaredCount} enemies.");

        return true;
    }
}
