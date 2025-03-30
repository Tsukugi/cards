using Godot;


public partial class AxisInputHandler
{
    bool inverted = false;
    public AxisType InputAxisType = AxisType.EightAxis;
    public AxisKeys Keys = new();

    public void SetInverted(bool value)
    {
        inverted = value;
    }
    public Vector2I GetAxis()
    {
        Vector2I axis = InputAxisType switch
        {
            AxisType.XAxis => GetXAxis(),
            AxisType.YAxis => GetYAxis(),
            AxisType.FourAxis => GetFourAxis(),
            AxisType.EightAxis => GetFullAxis(),
            _ => Vector2I.Zero,
        };
        return inverted ? axis * -1 : axis;
    }

    bool IsPressed(string key)
    {
        return Input.IsActionJustPressed(key);
    }

    Vector2I GetXAxis()
    {
        Vector2I axis = Vector2I.Zero;
        if (IsPressed(Keys.Right)) axis.X++;
        if (IsPressed(Keys.Left)) axis.X--;
        return axis;
    }
    Vector2I GetYAxis()
    {
        Vector2I axis = Vector2I.Zero;
        if (IsPressed(Keys.Down)) axis.Y++;
        if (IsPressed(Keys.Up)) axis.Y--;
        return axis;
    }
    Vector2I GetFourAxis()
    {
        Vector2I axis = Vector2I.Zero;
        if (IsPressed(Keys.Right)) return Vector2I.Right;
        if (IsPressed(Keys.Left)) return Vector2I.Left;
        if (IsPressed(Keys.Down)) return Vector2I.Down;
        if (IsPressed(Keys.Up)) return Vector2I.Up;
        return axis;
    }
    Vector2I GetFullAxis()
    {
        Vector2I axis = Vector2I.Zero;
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