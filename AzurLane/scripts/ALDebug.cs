using Godot;

public partial class ALDebug
{
    ALGameMatchManager matchManager;

    public ALDebug(ALGameMatchManager _matchManager) => matchManager = _matchManager;

    bool ignoreCosts = true;

    public void ToggleIgnoreCosts()
    {
        ignoreCosts = !ignoreCosts;
        GD.Print($"[ToggleIgnoreCosts] {ignoreCosts}");
    }
    public bool GetIgnoreCosts() => ignoreCosts;
}