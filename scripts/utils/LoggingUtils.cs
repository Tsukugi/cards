using System.Collections.Generic;
using System.Text;
using Godot;

public static class LoggingUtils
{
    public static string DictionaryToString(Dictionary<string, int> dict)
    {
        StringBuilder sb = new();
        sb.Append("{ ");

        foreach (var kvp in dict)
        {
            sb.Append($"\"{kvp.Key}\": {kvp.Value}, ");
        }

        // Remove the last comma and space
        if (sb.Length > 2)
        {
            sb.Length -= 2;
        }

        sb.Append(" }");
        return sb.ToString();
    }

    public static void Log(this Node node, string message)
    {
        GD.Print($"[{node.GetType()}] {message}");
    }
}