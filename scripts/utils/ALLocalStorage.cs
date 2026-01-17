using System;
using Godot;
using Newtonsoft.Json;

public static class ALLocalStorage
{
    const string SaveDir = "user://saves";
    const string ConnectionSettingsPath = "user://saves/connection_settings.json";
    const string MatchDebugSettingsPath = "user://saves/match_debug.json";

    public static void SaveConnectionSettings(ALConnectionSettings settings)
    {
        if (settings is null) throw new InvalidOperationException("[ALLocalStorage.SaveConnectionSettings] Settings are required.");
        EnsureSaveDir();
        WriteJson(ConnectionSettingsPath, settings);
    }

    public static ALConnectionSettings LoadConnectionSettings()
    {
        if (!FileAccess.FileExists(ConnectionSettingsPath)) return null;
        return ReadJson<ALConnectionSettings>(ConnectionSettingsPath);
    }

    public static void SaveMatchDebugSettings(ALMatchDebugSettings settings)
    {
        if (settings is null) throw new InvalidOperationException("[ALLocalStorage.SaveMatchDebugSettings] Settings are required.");
        EnsureSaveDir();
        WriteJson(MatchDebugSettingsPath, settings);
    }

    public static ALMatchDebugSettings LoadMatchDebugSettings()
    {
        if (!FileAccess.FileExists(MatchDebugSettingsPath)) return null;
        return ReadJson<ALMatchDebugSettings>(MatchDebugSettingsPath);
    }

    static void EnsureSaveDir()
    {
        var error = DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SaveDir));
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"[ALLocalStorage.EnsureSaveDir] Failed to create save directory. Error: {error}");
        }
    }

    static void WriteJson<T>(string path, T data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            var error = FileAccess.GetOpenError();
            throw new InvalidOperationException($"[ALLocalStorage.WriteJson] Failed to open file for write. Error: {error}");
        }
        file.StoreString(json);
    }

    static T ReadJson<T>(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            var error = FileAccess.GetOpenError();
            throw new InvalidOperationException($"[ALLocalStorage.ReadJson] Failed to open file for read. Error: {error}");
        }
        string json = file.GetAsText();
        T data = JsonConvert.DeserializeObject<T>(json);
        if (data is null)
        {
            throw new InvalidOperationException($"[ALLocalStorage.ReadJson] Failed to deserialize data at {path}.");
        }
        return data;
    }
}

public sealed class ALConnectionSettings
{
    public string Address { get; set; } = Network.DefaultServerIP;
    public int Port { get; set; } = Network.DefaultPort;
}

public sealed class ALMatchDebugSettings
{
    public bool IgnoreCosts { get; set; } = true;
}
