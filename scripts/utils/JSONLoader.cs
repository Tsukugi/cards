
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;

public class JSONLoader
{
    string databasePath = "./Database/";

    public JSONLoader() { }
    public JSONLoader(string databasePath)
    {
        this.databasePath = databasePath;
    }

    string LoadJson(string name)
    {
        using StreamReader reader = new($"{databasePath}{name}.json");
        string json = reader.ReadToEnd();
        return json;
    }

    public List<T> GetListFromJson<T>(string name)
    {
        string json = LoadJson(name);
        List<T> items = JsonConvert.DeserializeObject<List<T>>(json);
        return items;
    }

    public static Dictionary<string, T> Deserialize<T>(string data)
    {
        Dictionary<string, T> items = JsonConvert.DeserializeObject<Dictionary<string, T>>(data);
        return items;
    }

    public void UpdateDBPath(string newPath)
    {
        GD.Print("[UpdateDBPath] New path is " + newPath);
        databasePath = newPath;
    }

    public static T ForceToType<T>(object obj)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
    }
}
