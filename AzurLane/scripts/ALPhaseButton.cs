using Godot;

public partial class ALPhaseButton : Card
{
    [Export]
    double speed = 1f;
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!isSelected) RotationDegrees += Vector3.Zero.WithY((float)(speed / 2 * delta));
        else RotationDegrees += Vector3.Zero.WithY((float)(speed * delta));
    }
}