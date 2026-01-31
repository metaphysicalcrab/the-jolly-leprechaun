using Godot;
using TheJollyLeprechaun.Core.Interfaces;

namespace TheJollyLeprechaun.Core.Base;

/// <summary>
/// Abstract base class for state machine states.
/// Provides default implementations for IState methods.
/// Derive from this for convenience, or implement IState directly.
/// </summary>
public abstract partial class BaseState : Node, IState
{
    /// <summary>Reference to the owning state machine.</summary>
    protected StateMachine? StateMachine { get; private set; }

    public override void _Ready()
    {
        // Try to find parent state machine
        var parent = GetParent();
        if (parent is StateMachine sm)
        {
            StateMachine = sm;
        }
    }

    /// <summary>Called when entering this state.</summary>
    public virtual void Enter() { }

    /// <summary>Called when exiting this state.</summary>
    public virtual void Exit() { }

    /// <summary>Called every frame while this state is active.</summary>
    public virtual void Update(double delta) { }

    /// <summary>Called every physics frame while this state is active.</summary>
    public virtual void PhysicsUpdate(double delta) { }

    /// <summary>Called for unhandled input while this state is active.</summary>
    public virtual void HandleInput(InputEvent @event) { }

    /// <summary>Request a transition to another state by name.</summary>
    protected void TransitionTo(string stateName)
    {
        StateMachine?.TransitionTo(stateName);
    }

    /// <summary>Request a transition to another state by node.</summary>
    protected void TransitionTo(Node stateNode)
    {
        StateMachine?.TransitionTo(stateNode);
    }
}
