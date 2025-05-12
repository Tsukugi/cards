using System.Threading.Tasks;
using Godot;

public partial class ALDebug
{
    ALGameMatchManager matchManager;

    public ALDebug(ALGameMatchManager _matchManager) => matchManager = _matchManager;

    bool ignoreCosts = true;

    public void ToggleIgnoreCosts()
    {
        ignoreCosts = !ignoreCosts;
        GD.Print($"[Debug.ToggleIgnoreCosts] {ignoreCosts}");
    }
    public async Task DrawCard()
    {
        GD.Print($"[Debug.DrawCard]");
        await matchManager.GetControlledPlayer().DrawCardToHand();
    }
    public async Task DrawCubeCard()
    {
        GD.Print($"[Debug.DrawCubeCard]");
        await matchManager.GetControlledPlayer().TryDrawCubeToBoard();
    }
    public bool GetIgnoreCosts() => ignoreCosts;

    public async Task InflictDamage()
    {
        GD.Print($"[Debug.InflictDamage]");
        var player = matchManager.GetPlayerPlayingTurn();
        await player.ApplyDurabilityDamage(player.GetPlayerBoard<ALBoard>().GetFlagship());
    }
}