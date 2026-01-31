using Godot;

namespace TheJollyLeprechaun.Core.Interfaces;

/// <summary>
/// Contract for objects that can hide the player (bushes, barrels, etc.).
/// </summary>
public interface IHideable
{
    /// <summary>Whether something is currently hiding in this spot.</summary>
    bool IsOccupied { get; }

    /// <summary>
    /// Check if an entity can hide in this spot.
    /// </summary>
    /// <param name="entity">The entity trying to hide.</param>
    /// <returns>True if hiding is possible.</returns>
    bool CanHide(Node entity);

    /// <summary>
    /// Hide the entity in this spot.
    /// </summary>
    /// <param name="entity">The entity to hide.</param>
    void Hide(Node entity);

    /// <summary>
    /// Release the hidden entity from this spot.
    /// </summary>
    void Unhide();

    /// <summary>
    /// Get the world position where the hidden entity should be placed.
    /// </summary>
    Vector3 GetHiddenPosition();
}
