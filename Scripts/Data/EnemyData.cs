using Godot;

namespace TheJollyLeprechaun.Data;

/// <summary>
/// Resource defining enemy configuration.
/// Create .tres files to configure different enemy types.
/// </summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed { get; set; } = 3.0f;
    [Export] public float ChaseSpeed { get; set; } = 6.0f;
    [Export] public float FleeSpeed { get; set; } = 8.0f;
    [Export] public float RotationSpeed { get; set; } = 5.0f;

    [ExportGroup("Detection")]
    [Export] public float DetectionRange { get; set; } = 10.0f;
    [Export] public float DetectionAngle { get; set; } = 45.0f;
    [Export] public float LoseTargetTime { get; set; } = 3.0f;

    [ExportGroup("Behavior")]
    [Export] public float ScareThreshold { get; set; } = 0.5f;
    [Export] public float ScareRecoveryTime { get; set; } = 5.0f;
    [Export] public float IdleDuration { get; set; } = 3.0f;
    [Export] public float PatrolWaitTime { get; set; } = 2.0f;
}
