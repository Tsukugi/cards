using System;
using System.Threading.Tasks;
using Godot;

public partial class ALLobbyUI : Control
{
    ALNetwork network;
    [Export]
    Button cancelBtn;
    [Export]
    ItemList playersList;

    public event Action OnExitLobby;

    public override void _Ready()
    {
        base._Ready();
        network = GetNode<ALNetwork>("/root/Network");
        network.PlayerConnected += OnPlayerConnected;
        network.PlayerDisconnected += OnPlayerDisconnected;
        cancelBtn.Pressed += OnCancelHandler;
    }

    public Error JoinGame(string address = "")
    {
        return network.JoinGame(address);
    }

    public Error CreateGame()
    {
        return network.CreateGame();
    }

    public void OnCancelHandler()
    {
        playersList.Clear();
        network.CloseConnection();
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

    public static void StartMatch(string path)
    {
        Network.Instance.Rpc(Network.MethodName.StartMatch, [path]);
    }
}