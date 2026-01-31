using Godot;
using TheJollyLeprechaun.Core.Interfaces;

namespace TheJollyLeprechaun.Entities.Interactables;

/// <summary>
/// A spot where the player can hide from enemies.
/// Implements both IHideable (for hiding logic) and IInteractable (for player interaction).
/// </summary>
public partial class HidingSpot : Area3D, IHideable, IInteractable
{
    [ExportGroup("Settings")]
    [Export] private string _interactionPrompt = "Press F to hide";

    [ExportGroup("References")]
    [Export] private Marker3D? _hiddenPosition;

    [Signal] public delegate void EntityHidEventHandler(Node entity);
    [Signal] public delegate void EntityUnhidEventHandler();

    private Node? _hiddenEntity;

    // IHideable implementation
    public bool IsOccupied => _hiddenEntity != null;

    public bool CanHide(Node entity)
    {
        return !IsOccupied;
    }

    public void Hide(Node entity)
    {
        _hiddenEntity = entity;
        EmitSignal(SignalName.EntityHid, entity);
    }

    public void Unhide()
    {
        var entity = _hiddenEntity;
        _hiddenEntity = null;
        if (entity != null)
        {
            EmitSignal(SignalName.EntityUnhid);
        }
    }

    public Vector3 GetHiddenPosition()
    {
        return _hiddenPosition?.GlobalPosition ?? GlobalPosition;
    }

    // IInteractable implementation
    public string InteractionPrompt => _interactionPrompt;

    public bool CanInteract(Node interactor)
    {
        return !IsOccupied;
    }

    public void Interact(Node interactor)
    {
        // The PlayerHiding component handles the actual hiding logic
        // This is just for the interface contract
    }
}
