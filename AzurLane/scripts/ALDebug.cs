using Godot;

public partial class ALDebug
{
    ALGameMatchManager matchManager;

    public ALDebug(ALGameMatchManager _matchManager) => matchManager = _matchManager;

    bool ignoreCosts = false;

    public void ToggleIgnoreCosts()
    {
        ignoreCosts = !ignoreCosts;
        GD.Print($"[Debug.ToggleIgnoreCosts] {ignoreCosts}");
    }
    public void DrawCard()
    {
        GD.Print($"[Debug.DrawCard]");
        matchManager.GetControlledPlayer().DrawCardToHand();
    }
    public void DrawCubeCard()
    {
        GD.Print($"[Debug.DrawCubeCard]");
        matchManager.GetControlledPlayer().TryDrawCubeToBoard();
    }
    public bool GetIgnoreCosts() => ignoreCosts;

    public void InflictDamage()
    {
        GD.Print($"[Debug.InflictDamage]");
        var player = matchManager.GetPlayerPlayingTurn();
        player.ApplyDurabilityDamage(player.GetPlayerBoard<ALBoard>().GetFlagship());
    }
}