using Godot;

namespace TheJollyLeprechaun.Core.Interfaces;

/// <summary>
/// Contract for objects the player can interact with (hiding spots, items, etc.).
/// </summary>
public interface IInteractable
{
    /// <summary>Text to display when player is near (e.g., "Press F to hide").</summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Check if the interactor can currently interact with this object.
    /// </summary>
    /// <param name="interactor">The entity trying to interact.</param>
    /// <returns>True if interaction is possible.</returns>
    bool CanInteract(Node interactor);

    /// <summary>
    /// Perform the interaction.
    /// </summary>
    /// <param name="interactor">The entity performing the interaction.</param>
    void Interact(Node interactor);
}
