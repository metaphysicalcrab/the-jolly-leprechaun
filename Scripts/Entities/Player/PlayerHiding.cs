using Godot;
using TheJollyLeprechaun.Core.Autoloads;
using TheJollyLeprechaun.Core.Interfaces;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Handles player hiding behavior - entering and exiting hiding spots.
/// </summary>
public partial class PlayerHiding : Node
{
    [ExportGroup("Settings")]
    [Export] private float _hideTransitionTime = 0.3f;

    [ExportGroup("References")]
    [Export] private NodePath _playerPath = "..";
    [Export] private NodePath _movementPath = "../PlayerMovement";
    [Export] private NodePath _interactionRayPath = "../InteractionRayCast";

    private PlayerController? _player;
    private PlayerMovement? _movement;
    private RayCast3D? _interactionRay;

    [Signal] public delegate void HidingStartedEventHandler(Node hidingSpot);
    [Signal] public delegate void HidingEndedEventHandler();
    [Signal] public delegate void NearHidingSpotChangedEventHandler(bool isNear, string prompt);
    [Signal] public delegate void HidingStateChangedEventHandler(bool isHiding);

    public bool IsCurrentlyHidden { get; private set; }
    public IHideable? CurrentHidingSpot { get; private set; }
    public bool IsNearHidingSpot { get; private set; }

    private bool _enabled = true;
    private IHideable? _nearbyHidingSpot;

    public override void _Ready()
    {
        _player = GetNodeOrNull<PlayerController>(_playerPath);
        _movement = GetNodeOrNull<PlayerMovement>(_movementPath);
        _interactionRay = GetNodeOrNull<RayCast3D>(_interactionRayPath);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public override void _Process(double delta)
    {
        if (!_enabled) return;

        // Check for nearby hiding spots for UI prompt
        CheckNearbyHidingSpot();

        if (InputManager.Instance?.IsInteractJustPressed() == true)
        {
            if (IsCurrentlyHidden)
            {
                TryUnhide();
            }
            else
            {
                TryFindAndHide();
            }
        }
    }

    private void CheckNearbyHidingSpot()
    {
        if (_interactionRay == null || IsCurrentlyHidden) return;

        _interactionRay.ForceRaycastUpdate();

        IHideable? foundSpot = null;
        string prompt = "";

        if (_interactionRay.IsColliding())
        {
            var collider = _interactionRay.GetCollider();
            if (collider is IHideable hideable && collider is IInteractable interactable)
            {
                if (_player != null && interactable.CanInteract(_player))
                {
                    foundSpot = hideable;
                    prompt = interactable.InteractionPrompt;
                }
            }
        }

        // Check if state changed
        bool wasNear = IsNearHidingSpot;
        IsNearHidingSpot = foundSpot != null;
        _nearbyHidingSpot = foundSpot;

        if (wasNear != IsNearHidingSpot)
        {
            EmitSignal(SignalName.NearHidingSpotChanged, IsNearHidingSpot, prompt);
        }
    }

    /// <summary>Try to hide in a nearby hiding spot.</summary>
    public void TryFindAndHide()
    {
        // Use raycast to find interactable hiding spot
        if (_interactionRay == null) return;

        _interactionRay.ForceRaycastUpdate();

        if (!_interactionRay.IsColliding()) return;

        var collider = _interactionRay.GetCollider();
        if (collider is IHideable hideable && collider is IInteractable interactable)
        {
            if (_player != null && interactable.CanInteract(_player))
            {
                TryHide(hideable);
            }
        }
    }

    /// <summary>Attempt to hide in a specific hiding spot.</summary>
    public bool TryHide(IHideable hidingSpot)
    {
        if (_player == null || !hidingSpot.CanHide(_player)) return false;

        CurrentHidingSpot = hidingSpot;
        IsCurrentlyHidden = true;
        hidingSpot.Hide(_player);

        // Disable movement
        _movement?.SetEnabled(false);

        // Tell the player controller we're hidden
        _player.SetHidden(true);

        // Move player to hidden position with tween
        var targetPos = hidingSpot.GetHiddenPosition();
        var tween = CreateTween();
        tween.TweenProperty(_player, "global_position", targetPos, _hideTransitionTime);

        EmitSignal(SignalName.HidingStarted, (Node)hidingSpot);
        EmitSignal(SignalName.HidingStateChanged, true);
        GameEvents.Instance?.EmitPlayerHid((Node)hidingSpot);

        GD.Print("Player is now hiding");
        return true;
    }

    /// <summary>Exit the current hiding spot.</summary>
    public bool TryUnhide()
    {
        if (!IsCurrentlyHidden || CurrentHidingSpot == null) return false;

        CurrentHidingSpot.Unhide();
        CurrentHidingSpot = null;
        IsCurrentlyHidden = false;

        // Re-enable movement
        _movement?.SetEnabled(true);

        // Tell the player controller we're visible
        _player?.SetHidden(false);

        EmitSignal(SignalName.HidingEnded);
        EmitSignal(SignalName.HidingStateChanged, false);
        GameEvents.Instance?.EmitPlayerUnhid();

        GD.Print("Player exited hiding");
        return true;
    }
}
