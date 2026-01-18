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

    public static void SaveMatchDebugSettings(ALMatchDebugSettings settings, string profileName)
    {
        if (settings is null) throw new InvalidOperationException("[ALLocalStorage.SaveMatchDebugSettings] Settings are required.");
        EnsureSaveDir();
        WriteJson(GetMatchDebugSettingsPath(profileName), settings);
    }

    public static ALMatchDebugSettings LoadMatchDebugSettings()
    {
        if (!FileAccess.FileExists(MatchDebugSettingsPath)) return null;
        return ReadJson<ALMatchDebugSettings>(MatchDebugSettingsPath);
    }

    public static ALMatchDebugSettings LoadMatchDebugSettings(string profileName)
    {
        string path = GetMatchDebugSettingsPath(profileName);
        if (!FileAccess.FileExists(path)) return null;
        return ReadJson<ALMatchDebugSettings>(path);
    }

    public static void SavePlayerSettings(ALPlayerSettings settings, string profileName)
    {
        if (settings is null) throw new InvalidOperationException("[ALLocalStorage.SavePlayerSettings] Settings are required.");
        EnsureSaveDir();
        WriteJson(GetPlayerSettingsPath(profileName), settings);
    }

    public static ALPlayerSettings LoadPlayerSettings(string profileName)
    {
        string path = GetPlayerSettingsPath(profileName);
        if (!FileAccess.FileExists(path)) return null;
        return ReadJson<ALPlayerSettings>(path);
    }

    static void EnsureSaveDir()
    {
        var error = DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SaveDir));
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"[ALLocalStorage.EnsureSaveDir] Failed to create save directory. Error: {error}");
        }
    }

    static string GetMatchDebugSettingsPath(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return MatchDebugSettingsPath;
        }

        string safeName = NormalizeProfileName(profileName);
        return $"user://saves/match_debug_{safeName}.json";
    }

    static string GetPlayerSettingsPath(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new InvalidOperationException("[ALLocalStorage.GetPlayerSettingsPath] Profile name is required.");
        }

        string safeName = NormalizeProfileName(profileName);
        return $"user://saves/player_settings_{safeName}.json";
    }

    static string NormalizeProfileName(string profileName)
    {
        var builder = new System.Text.StringBuilder(profileName.Length);
        foreach (char character in profileName)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
                continue;
            }
            builder.Append('_');
        }
        string result = builder.ToString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException("[ALLocalStorage.NormalizeProfileName] Profile name must include alphanumeric characters.");
        }
        return result;
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
    public bool EnableAutoHostMatch { get; set; } = false;
    public bool EnableAutoJoinMatch { get; set; } = false;
}

public sealed class ALPlayerSettings
{
    public string Name { get; set; } = "PlayerName";
}
