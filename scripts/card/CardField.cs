using Godot;

public partial class CardField : Node3D
{
    [Export]
    public Vector2I positionInBoard = new();
    public static float cardSize = 4;

}
