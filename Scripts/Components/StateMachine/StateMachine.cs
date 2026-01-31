using Godot;
using System.Linq;
using TheJollyLeprechaun.Core.Interfaces;

namespace TheJollyLeprechaun.Core.Base;

/// <summary>
/// Generic state machine that manages IState nodes.
/// Add state nodes as children, and this machine will handle transitions and updates.
/// </summary>
public partial class StateMachine : Node
{
    [Export] private Node? _initialState;

    [Signal] public delegate void StateChangedEventHandler(string previousStateName, string newStateName);

    private IState? _currentState;
    private Node? _currentStateNode;

    /// <summary>The currently active state.</summary>
    public IState? CurrentState => _currentState;

    /// <summary>The name of the current state.</summary>
    public string CurrentStateName => _currentStateNode?.Name ?? "";

    public override void _Ready()
    {
        // Wait a frame to ensure all children are ready
        CallDeferred(MethodName.InitializeStateMachine);
    }

    private void InitializeStateMachine()
    {
        var parentName = GetParent()?.Name ?? "unknown";
        GD.Print($"StateMachine ({parentName}): Initializing with {GetChildCount()} states");

        if (_initialState != null && _initialState is IState)
        {
            TransitionTo(_initialState);
        }
        else if (GetChildCount() > 0)
        {
            // Default to first child if no initial state specified
            var firstChild = GetChild(0);
            if (firstChild is IState)
            {
                GD.Print($"StateMachine ({parentName}): Starting with {firstChild.Name}");
                TransitionTo(firstChild);
            }
            else
            {
                GD.PrintErr($"StateMachine ({parentName}): First child {firstChild.Name} is not IState!");
            }
        }
        else
        {
            GD.PrintErr($"StateMachine ({parentName}): No states found!");
        }
    }

    public override void _Process(double delta)
    {
        _currentState?.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _currentState?.PhysicsUpdate(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        _currentState?.HandleInput(@event);
    }

    /// <summary>Transition to a state by node reference.</summary>
    public void TransitionTo(Node newStateNode)
    {
        if (newStateNode is not IState newState)
        {
            GD.PrintErr($"StateMachine: Cannot transition to {newStateNode.Name} - not an IState");
            return;
        }

        var previousStateName = _currentStateNode?.Name ?? "";

        // Exit current state
        _currentState?.Exit();

        // Enter new state
        _currentStateNode = newStateNode;
        _currentState = newState;
        _currentState.Enter();

        EmitSignal(SignalName.StateChanged, previousStateName, newStateNode.Name);
    }

    /// <summary>Transition to a state by name.</summary>
    public void TransitionTo(string stateName)
    {
        var stateNode = GetNodeOrNull(stateName);
        if (stateNode == null)
        {
            var parentName = GetParent()?.Name ?? "unknown";
            GD.PrintErr($"StateMachine ({parentName}): State '{stateName}' not found! Available: {string.Join(", ", GetChildren().Select(c => c.Name))}");
            return;
        }

        TransitionTo(stateNode);
    }

    /// <summary>Check if a state exists by name.</summary>
    public bool HasState(string stateName)
    {
        return GetNodeOrNull(stateName) is IState;
    }

    /// <summary>Get a state node by name.</summary>
    public T? GetState<T>(string stateName) where T : class, IState
    {
        return GetNodeOrNull(stateName) as T;
    }
}
