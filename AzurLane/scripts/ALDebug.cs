using System.Threading.Tasks;
using Godot;

public partial class ALDebug
{
    ALGameMatchManager matchManager;

    public ALDebug(ALGameMatchManager _matchManager)
    {
        matchManager = _matchManager;
        LoadSavedSettings();
    }

    bool ignoreCosts = true;

    public void ToggleIgnoreCosts()
    {
        ignoreCosts = !ignoreCosts;
        SaveSettings();
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
    public async Task TestRetaliation()
    {
        GD.Print($"[Debug.TestRetaliation]");
        var player = matchManager.GetPlayerPlayingTurn();
        await player.AddDurabilityCard(matchManager.GetDatabase().cards["SD01-016"]);
        await InflictDamage();
    }

    void LoadSavedSettings()
    {
        var settings = ALLocalStorage.LoadMatchDebugSettings();
        if (settings is null) return;
        ignoreCosts = settings.IgnoreCosts;
    }

    void SaveSettings()
    {
        var settings = ALLocalStorage.LoadMatchDebugSettings() ?? new ALMatchDebugSettings();
        settings.IgnoreCosts = ignoreCosts;
        ALLocalStorage.SaveMatchDebugSettings(settings);
    }
}
