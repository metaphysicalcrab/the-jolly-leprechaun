using Godot;

namespace TheJollyLeprechaun.Entities.Enemies;

/// <summary>
/// Humanoid enemy type.
/// Uses the player_character model and has humanoid-specific behaviors.
/// </summary>
public partial class HumanoidEnemy : BaseEnemy
{
    // Add humanoid-specific behaviors here if needed
    // For now, it uses all the base enemy functionality

    public override void _Ready()
    {
        base._Ready();

        // Humanoid-specific initialization
        GD.Print($"HumanoidEnemy {Name} ready");
    }
}
