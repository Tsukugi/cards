

using Godot;


public partial class AxisInputHandler
{
    Vector2 moveDirection = Vector2.Zero;
    public AxisType InputAxisType = AxisType.EightAxis;
    public AxisKeys Keys = new();


    public bool GetAxisChange()
    {
        Vector2 axis = GetAxis();
        return moveDirection != axis;
    }

    public Vector2 GetAxis()
    {
        Vector2 axis = InputAxisType switch
        {
            AxisType.XAxis => GetXAxis(),
            AxisType.YAxis => GetYAxis(),
            AxisType.FourAxis => GetFourAxis(),
            AxisType.EightAxis => GetFullAxis(),
            _ => Vector2.Zero,
        };
        return axis;
    }

    bool IsPressed(string key)
    {
        return Input.IsActionJustPressed(key);
    }

    Vector2 GetXAxis()
    {
        Vector2 axis = Vector2.Zero;
        if (IsPressed(Keys.Right)) axis.X++;
        if (IsPressed(Keys.Left)) axis.X--;
        return axis;
    }
    Vector2 GetYAxis()
    {
        Vector2 axis = Vector2.Zero;
        if (IsPressed(Keys.Down)) axis.Y++;
        if (IsPressed(Keys.Up)) axis.Y--;
        return axis;
    }
    Vector2 GetFourAxis()
    {
        Vector2 axis = Vector2.Zero;
        if (IsPressed(Keys.Right)) return Vector2.Right;
        if (IsPressed(Keys.Left)) return Vector2.Left;
        if (IsPressed(Keys.Down)) return Vector2.Down;
        if (IsPressed(Keys.Up)) return Vector2.Up;
        return axis;
    }
    Vector2 GetFullAxis()
    {
        Vector2 axis = Vector2.Zero;
        if (IsPressed(Keys.Right)) axis.X++;
        if (IsPressed(Keys.Left)) axis.X--;
        if (IsPressed(Keys.Down)) axis.Y++;
        if (IsPressed(Keys.Up)) axis.Y--;
        return axis;
    }
}

public class AxisKeys
{
    public string Right = "ui_right";
    public string Left = "ui_left";
    public string Down = "ui_down";
    public string Up = "ui_up";
}

public enum AxisType
{
    XAxis,
    YAxis,
    FourAxis,
    EightAxis,
}