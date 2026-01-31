using Godot;
using TheJollyLeprechaun.Entities.Player;

namespace TheJollyLeprechaun.UI;

/// <summary>
/// In-game HUD showing scare cooldown and interaction prompts.
/// </summary>
public partial class GameHUD : CanvasLayer
{
	[ExportGroup("References")]
	[Export] private NodePath _scareCooldownBarPath = "MarginContainer/VBoxContainer/ScareCooldown/ProgressBar";
	[Export] private NodePath _scareCooldownLabelPath = "MarginContainer/VBoxContainer/ScareCooldown/Label";
	[Export] private NodePath _interactionPromptPath = "CenterContainer/InteractionPrompt";
	[Export] private NodePath _playerPath = "";

	private ProgressBar? _scareCooldownBar;
	private Label? _scareCooldownLabel;
	private Control? _interactionPrompt;
	private Label? _interactionPromptLabel;
	private PlayerScare? _playerScare;
	private PlayerHiding? _playerHiding;

	private bool _isNearHidingSpot;
	private string _currentPrompt = "";

	public override void _Ready()
	{
		_scareCooldownBar = GetNodeOrNull<ProgressBar>(_scareCooldownBarPath);
		_scareCooldownLabel = GetNodeOrNull<Label>(_scareCooldownLabelPath);
		_interactionPrompt = GetNodeOrNull<Control>(_interactionPromptPath);

		if (_interactionPrompt != null)
		{
			_interactionPromptLabel = _interactionPrompt.GetNodeOrNull<Label>("Label");
			_interactionPrompt.Visible = false;
		}

		// Find player components
		CallDeferred(nameof(FindPlayerComponents));
	}

	private void FindPlayerComponents()
	{
		// Guard against being called after node is removed from tree
		if (!IsInsideTree()) return;

		// Try to find player in scene
		var player = GetTree().GetFirstNodeInGroup("Player");
		if (player == null)
		{
			// Try finding by path or name
			player = GetTree().Root.FindChild("Player", true, false);
		}

		if (player != null)
		{
			_playerScare = player.GetNodeOrNull<PlayerScare>("PlayerScare");
			_playerHiding = player.GetNodeOrNull<PlayerHiding>("PlayerHiding");

			if (_playerScare != null)
			{
				_playerScare.ScareCooldownStarted += OnScareCooldownStarted;
				_playerScare.ScareCooldownEnded += OnScareCooldownEnded;
				_playerScare.ScareUsed += OnScareUsed;
			}

			if (_playerHiding != null)
			{
				_playerHiding.NearHidingSpotChanged += OnNearHidingSpotChanged;
				_playerHiding.HidingStateChanged += OnHidingStateChanged;
			}

			GD.Print("GameHUD: Found player components");
		}
		else
		{
			GD.PrintErr("GameHUD: Could not find player");
		}
	}

	public override void _ExitTree()
	{
		if (_playerScare != null)
		{
			_playerScare.ScareCooldownStarted -= OnScareCooldownStarted;
			_playerScare.ScareCooldownEnded -= OnScareCooldownEnded;
			_playerScare.ScareUsed -= OnScareUsed;
		}

		if (_playerHiding != null)
		{
			_playerHiding.NearHidingSpotChanged -= OnNearHidingSpotChanged;
			_playerHiding.HidingStateChanged -= OnHidingStateChanged;
		}
	}

	public override void _Process(double delta)
	{
		UpdateScareCooldown();
	}

	private void UpdateScareCooldown()
	{
		if (_scareCooldownBar == null || _playerScare == null) return;

		if (_playerScare.IsOnCooldown)
		{
			var progress = 1.0f - (_playerScare.CooldownRemaining / _playerScare.CooldownDuration);
			_scareCooldownBar.Value = progress * 100;

			if (_scareCooldownLabel != null)
			{
				_scareCooldownLabel.Text = $"Scare: {_playerScare.CooldownRemaining:F1}s";
			}
		}
		else
		{
			_scareCooldownBar.Value = 100;
			if (_scareCooldownLabel != null)
			{
				_scareCooldownLabel.Text = "Scare: Ready [E]";
			}
		}
	}

	private void OnScareCooldownStarted(float duration)
	{
		// Visual feedback when scare goes on cooldown
	}

	private void OnScareCooldownEnded()
	{
		// Visual feedback when scare is ready
	}

	private void OnScareUsed(int enemiesScared)
	{
		// Could show "Scared X enemies!" feedback
	}

	private void OnNearHidingSpotChanged(bool isNear, string prompt)
	{
		_isNearHidingSpot = isNear;
		UpdateInteractionPrompt(isNear ? prompt : "");
	}

	private void OnHidingStateChanged(bool isHiding)
	{
		if (isHiding)
		{
			UpdateInteractionPrompt("Press [F] to exit hiding spot");
		}
		else if (!_isNearHidingSpot)
		{
			UpdateInteractionPrompt("");
		}
	}

	private void UpdateInteractionPrompt(string prompt)
	{
		_currentPrompt = prompt;

		if (_interactionPrompt == null) return;

		if (string.IsNullOrEmpty(prompt))
		{
			_interactionPrompt.Visible = false;
		}
		else
		{
			_interactionPrompt.Visible = true;
			if (_interactionPromptLabel != null)
			{
				_interactionPromptLabel.Text = prompt;
			}
		}
	}
}
