
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;

public static class NodeUtils
{
    public static T TryFindParentNodeOfType<T>(this Node child)
    {
        Node currentNode = child;
        while (true)
        {
            currentNode = currentNode.GetParent();
            if (currentNode == null)  // We went to the root node
                throw new System.Exception($"[TryFindParentNodeOfType] Could not find a direct or indirect parent of this node with the specified type {typeof(T).Name}");
            if (currentNode is T typedNode) return typedNode;
        }
    }

    public static List<T> TryGetAllChildOfType<T>(this Node parent, bool includeInternal = false)
    {
        Array<Node> children = parent.GetChildren();
        List<T> typedChildren = [];
        foreach (Node child in children)
        {
            if (child is not T typedChild)
            {
                if (!includeInternal) continue;
                typedChildren.AddRange(child.TryGetAllChildOfType<T>());
            }
            else
            {
                typedChildren.Add(typedChild);
            }
        }
        //GD.Print($"[TryGetAllChildOfType] {parent.Name} -> {typedChildren.Count} nodes matching type");
        return typedChildren;
    }

    public static async Task Wait(this Node caller, float seconds, Action callback = null)
    {
        await caller.ToSignal(caller.GetTree().CreateTimer(seconds), "timeout");
        if (callback is Action onTimeout) onTimeout();
    }

    public static T ConvertObject<T>(object M) where T : class
    {
        // Serialize the original object to json
        // Desarialize the json object to the new type 
        var obj = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(M));
        return obj;
    }

    public static void ChangeScene(this Node caller, string scenePath)
    {
        SceneTree tree = caller.GetTree();

        PackedScene newScene = (PackedScene)ResourceLoader.Load(scenePath);
        if (newScene != null)
        {
            tree.ChangeSceneToPacked(newScene);
        }
        else
        {
            GD.PrintErr($"[ChangeScene]: Scene couldn't be loaded {scenePath}");
        }
    }
}