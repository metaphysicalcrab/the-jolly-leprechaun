using Godot;

namespace TheJollyLeprechaun.Core.Autoloads;

/// <summary>
/// Global event bus for decoupled communication between systems.
/// Use sparingly - prefer direct signals for local communication.
/// </summary>
public partial class GameEvents : Node
{
    public static GameEvents Instance { get; private set; } = null!;

    // Player events
    [Signal] public delegate void PlayerHidEventHandler(Node hidingSpot);
    [Signal] public delegate void PlayerUnhidEventHandler();
    [Signal] public delegate void PlayerScaredEnemyEventHandler(Node enemy);
    [Signal] public delegate void PlayerCaughtEventHandler(Node enemy);

    // Enemy events
    [Signal] public delegate void EnemyDetectedPlayerEventHandler(Node enemy);
    [Signal] public delegate void EnemyLostPlayerEventHandler(Node enemy);
    [Signal] public delegate void EnemyScaredEventHandler(Node enemy);
    [Signal] public delegate void EnemyRecoveredEventHandler(Node enemy);

    // Game state events
    [Signal] public delegate void GamePausedEventHandler();
    [Signal] public delegate void GameResumedEventHandler();
    [Signal] public delegate void LevelRestartRequestedEventHandler();
    [Signal] public delegate void LevelCompletedEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    // Convenience methods for emitting common events
    public void EmitPlayerHid(Node hidingSpot) =>
        EmitSignal(SignalName.PlayerHid, hidingSpot);

    public void EmitPlayerUnhid() =>
        EmitSignal(SignalName.PlayerUnhid);

    public void EmitPlayerScaredEnemy(Node enemy) =>
        EmitSignal(SignalName.PlayerScaredEnemy, enemy);

    public void EmitPlayerCaught(Node enemy) =>
        EmitSignal(SignalName.PlayerCaught, enemy);

    public void EmitEnemyDetectedPlayer(Node enemy) =>
        EmitSignal(SignalName.EnemyDetectedPlayer, enemy);

    public void EmitEnemyLostPlayer(Node enemy) =>
        EmitSignal(SignalName.EnemyLostPlayer, enemy);

    public void EmitEnemyScared(Node enemy) =>
        EmitSignal(SignalName.EnemyScared, enemy);

    public void EmitEnemyRecovered(Node enemy) =>
        EmitSignal(SignalName.EnemyRecovered, enemy);

    public void EmitGamePaused() =>
        EmitSignal(SignalName.GamePaused);

    public void EmitGameResumed() =>
        EmitSignal(SignalName.GameResumed);

    public void EmitLevelRestartRequested() =>
        EmitSignal(SignalName.LevelRestartRequested);

    public void EmitLevelCompleted() =>
        EmitSignal(SignalName.LevelCompleted);
}
