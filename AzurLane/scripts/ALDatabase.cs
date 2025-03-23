using System.Collections.Generic;
using Godot;
public class ALDatabase
{
    public Dictionary<string, ALCardDTO> cards = new();

    protected JSONLoader loader = new("./AzurLane/database/");


    public void LoadData()
    {
        cards = Load<ALCardDTO>("Cards");
    }

    public Dictionary<string, T> Load<T>(string name) where T : BaseDTO
    {
        Dictionary<string, T> values = new();
        loader.GetListFromJson<T>(name).ForEach(value =>
        {
            values.Add(value.id, value); GD.Print($"[ALDatabase.Load] {value.id} loaded");
        });
        return values;
    }

}

