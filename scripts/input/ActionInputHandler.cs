using Godot;


public partial class ActionInputHandler
{
    public ActionKeys Keys = new();

    public InputAction GetAction()
    {
        if (IsPressed(Keys.Cancel)) return InputAction.Cancel;
        if (IsPressed(Keys.Ok)) return InputAction.Ok;
        if (IsPressed(Keys.Details)) return InputAction.Details;
        if (IsPressed(Keys.Special)) return InputAction.Special;
        return InputAction.None;
    }

    bool IsPressed(string key)
    {
        return Input.IsActionJustPressed(key);
    }
}

public class ActionKeys
{
    public string Details = "ui_select";
    public string Cancel = "ui_cancel";
    public string Ok = "ui_accept";
    public string Special = "ui_home";
}

public enum InputAction
{
    None,
    Ok,
    Cancel,
    Details,
    Special,
}