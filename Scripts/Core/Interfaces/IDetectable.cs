using Godot;

namespace TheJollyLeprechaun.Core.Interfaces;

/// <summary>
/// Contract for entities that can be detected by enemies.
/// The player implements this to allow enemies to track them.
/// </summary>
public interface IDetectable
{
    /// <summary>Whether this entity is currently hidden from detection.</summary>
    bool IsHidden { get; }

    /// <summary>The last known position of this entity (for chase behavior after losing sight).</summary>
    Vector3 LastKnownPosition { get; }

    /// <summary>
    /// Check if this entity can currently be detected.
    /// Returns false if hidden or otherwise invisible to enemies.
    /// </summary>
    bool CanBeDetected();
}
