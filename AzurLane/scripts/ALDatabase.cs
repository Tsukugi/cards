using System.Collections.Generic;
using Godot;
public class ALDatabase
{
    public Dictionary<string, ALCardDTO> cards = [];
    public Dictionary<string, ALDeckDTO> decks = [];

    protected JSONLoader loader = new("./AzurLane/database/");


    public void LoadData()
    {
        cards = Load<ALCardDTO>("Cards");
        decks = Load<ALDeckDTO>("Decks");
    }

    public Dictionary<string, T> Load<T>(string name) where T : BaseDTO
    {
        Dictionary<string, T> values = [];
        loader.GetListFromJson<T>(name).ForEach(value =>
        {
            values.Add(value.id, value); //GD.Print($"[ALDatabase.Load] {value.id} loaded");
        });
        return values;
    }
}

