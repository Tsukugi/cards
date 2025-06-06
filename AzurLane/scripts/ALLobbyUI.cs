using System;
using Godot;

public partial class ALLobbyUI : Control
{
    [Export]
    Button cancelBtn;
    [Export]
    ItemList playersList;

    public event Action OnExitLobby;

    public override void _Ready()
    {
        base._Ready();
        Network.Instance.PlayerConnected += OnPlayerConnected;
        Network.Instance.PlayerDisconnected += OnPlayerDisconnected;
        cancelBtn.Pressed += OnCancelHandler;
    }

    public void OnCancelHandler()
    {
        playersList.Clear();
        Network.Instance.CloseConnection();
        if (OnExitLobby is not null) OnExitLobby();
    }

    public void OnPlayerConnected(int peerId, Godot.Collections.Dictionary<string, string> playerInfo)
    {
        GD.Print(playerInfo);
        var name = playerInfo.TryGetValue("Name", out string value) ? value : null;
        playersList.AddItem($"{peerId} - {name}");
        playersList.SortItemsByText();
    }

    public void OnPlayerDisconnected(int peerId)
    {
        for (int i = 0; i < playersList.GetItemCount(); i++)
        {
            if (playersList.GetItemText(i).Contains(peerId.ToString())) playersList.RemoveItem(i);
        }
    }
}