using Godot;

namespace TheJollyLeprechaun.Core.Interfaces;

/// <summary>
/// Contract for entities that can be scared by the player.
/// Enemies implement this to react to the player's scare ability.
/// </summary>
public interface IScareable
{
    /// <summary>Whether this entity is currently in a scared state.</summary>
    bool IsScared { get; }

    /// <summary>
    /// Apply scare effect to this entity.
    /// </summary>
    /// <param name="source">The node that caused the scare (usually player).</param>
    /// <param name="intensity">How intense the scare is (affects flee distance/duration).</param>
    void Scare(Node source, float intensity);

    /// <summary>
    /// Called when the entity recovers from being scared.
    /// </summary>
    void RecoverFromScare();
}
