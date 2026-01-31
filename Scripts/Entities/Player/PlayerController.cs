using Godot;
using TheJollyLeprechaun.Core.Interfaces;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Main player controller that coordinates all player components.
/// Implements IDetectable so enemies can track the player.
/// </summary>
public partial class PlayerController : CharacterBody3D, IDetectable
{
	[ExportGroup("Components")]
	[Export] private PlayerMovement _movement = null!;
	[Export] private Node3D _model = null!;

	// Will be added in later phases
	// [Export] private PlayerScare _scare = null!;
	// [Export] private PlayerHiding _hiding = null!;

	private bool _isHidden;

	// IDetectable implementation
	public bool IsHidden => _isHidden;
	public Vector3 LastKnownPosition { get; private set; }

	public bool CanBeDetected() => !_isHidden;

	public override void _Ready()
	{
		LastKnownPosition = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Update last known position when not hidden
		if (!_isHidden)
		{
			LastKnownPosition = GlobalPosition;
		}
	}

	/// <summary>Called by PlayerHiding when player enters a hiding spot.</summary>
	public void SetHidden(bool hidden)
	{
		_isHidden = hidden;

		// Make model invisible when hidden (optional, could also shrink/fade)
		if (_model != null)
		{
			_model.Visible = !hidden;
		}
	}

	/// <summary>Disable player controls (for cutscenes, death, etc.).</summary>
	public void SetControlsEnabled(bool enabled)
	{
		_movement?.SetEnabled(enabled);
	}
}
