using System;
using System.Collections.Generic;
using Godot;

public partial class TestSceneController : Node
{
    public override void _EnterTree()
    {
        bool allTests = HasFlag("--all-tests") || HasFlag("-all-tests");
        string testFilter = GetValue("--test");
        if (string.IsNullOrWhiteSpace(testFilter))
        {
            testFilter = GetValue("-test");
        }

        if (allTests && !string.IsNullOrWhiteSpace(testFilter))
        {
            throw new InvalidOperationException("[TestSceneController] Use only one of --all-tests or --test.");
        }

        if (string.IsNullOrWhiteSpace(testFilter))
        {
            if (allTests)
            {
                return;
            }
            return;
        }

        ApplySingleTestFilter(testFilter);
    }

    void ApplySingleTestFilter(string filter)
    {
        string normalized = NormalizeFilter(filter);
        List<Node> testNodes = new();
        CollectTestNodes(this, testNodes);

        List<Node> matches = testNodes.FindAll(node => MatchesFilter(node, normalized));
        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"[TestSceneController] No tests matched --test='{filter}'.");
        }
        if (matches.Count > 1)
        {
            throw new InvalidOperationException($"[TestSceneController] Multiple tests matched --test='{filter}'.");
        }

        foreach (Node node in testNodes)
        {
            if (matches.Contains(node))
            {
                continue;
            }
            if (node.GetParent() is null)
            {
                continue;
            }
            node.GetParent().RemoveChild(node);
            node.QueueFree();
        }
    }

    static void CollectTestNodes(Node parent, List<Node> results)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (IsTestNode(child))
            {
                results.Add(child);
            }
            CollectTestNodes(child, results);
        }
    }

    static bool IsTestNode(Node node)
    {
        if (node is null)
        {
            throw new InvalidOperationException("[TestSceneController.IsTestNode] Node is required.");
        }
        Variant scriptValue = node.GetScript();
        if (scriptValue.VariantType == Variant.Type.Nil)
        {
            return false;
        }
        var script = scriptValue.As<Script>();
        if (script is null)
        {
            return false;
        }
        string path = script.ResourcePath ?? "";
        if (path.IndexOf("/tests/Test_", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }
        string nodeName = node.Name.ToString();
        return nodeName.StartsWith("Test", StringComparison.OrdinalIgnoreCase);
    }

    static bool MatchesFilter(Node node, string filter)
    {
        if (node is null)
        {
            throw new InvalidOperationException("[TestSceneController.MatchesFilter] Node is required.");
        }
        if (string.IsNullOrWhiteSpace(filter))
        {
            return false;
        }
        string nodeName = node.Name.ToString();
        if (string.Equals(nodeName, filter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        Variant scriptValue = node.GetScript();
        if (scriptValue.VariantType == Variant.Type.Nil)
        {
            return false;
        }
        var script = scriptValue.As<Script>();
        if (script is null)
        {
            return false;
        }
        string path = script.ResourcePath ?? "";
        if (string.Equals(path, filter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return path.EndsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    static string NormalizeFilter(string filter)
    {
        return filter.Replace('\\', '/').Trim();
    }

    static bool HasFlag(string flag)
    {
        return HasFlagInArgs(OS.GetCmdlineUserArgs(), flag)
            || HasFlagInArgs(OS.GetCmdlineArgs(), flag);
    }

    static bool HasFlagInArgs(string[] args, string flag)
    {
        if (args is null || args.Length == 0)
        {
            return false;
        }
        foreach (string arg in args)
        {
            if (arg is null)
            {
                continue;
            }
            if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (arg.StartsWith(flag + "=", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    static string GetValue(string key)
    {
        string value = GetValueFromArgs(OS.GetCmdlineUserArgs(), key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
        return GetValueFromArgs(OS.GetCmdlineArgs(), key);
    }

    static string GetValueFromArgs(string[] args, string key)
    {
        if (args is null || args.Length == 0)
        {
            return "";
        }
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg is null)
            {
                continue;
            }
            if (arg.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                return arg[(key.Length + 1)..];
            }
            if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }
        return "";
    }
}
