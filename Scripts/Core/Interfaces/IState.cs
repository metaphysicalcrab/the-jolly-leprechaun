using Godot;

namespace TheJollyLeprechaun.Core.Interfaces;

/// <summary>
/// Contract for state machine states.
/// States handle Enter/Exit lifecycle and per-frame updates.
/// </summary>
public interface IState
{
    /// <summary>Called when transitioning INTO this state.</summary>
    void Enter();

    /// <summary>Called when transitioning OUT OF this state.</summary>
    void Exit();

    /// <summary>Called every frame (_Process).</summary>
    void Update(double delta);

    /// <summary>Called every physics frame (_PhysicsProcess).</summary>
    void PhysicsUpdate(double delta);

    /// <summary>Called for unhandled input events.</summary>
    void HandleInput(InputEvent @event);
}
