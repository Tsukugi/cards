using Godot;

public partial class ALPhaseButton : Card
{
    [Export]
    double speed = 1f;
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!isSelected) return;
        RotationDegrees += Vector3.Zero.WithY((float)(speed * delta));
    }
}