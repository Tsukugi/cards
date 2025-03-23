using Godot;

public static class VectorUtils
{
    public static Vector3 WithX(this Vector3 src, float X) => new Vector3(X, src.Y, src.Z);
    public static Vector3 WithY(this Vector3 src, float Y) => new Vector3(src.X, Y, src.Z);
    public static Vector3 WithZ(this Vector3 src, float Z) => new Vector3(src.X, src.Y, Z);
}