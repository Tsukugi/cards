
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public static class NodeUtils
{
    public static T TryFindParentNodeOfType<T>(this Node child)
    {
        Node currentNode = child;
        while (true)
        {
            currentNode = currentNode.GetParent();
            if (currentNode == null)  // We went to the root node
                throw new System.Exception("[TryFindParentNodeOfType] Could not find a direct or indirect parent of this node with the specified type");
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

    public static async System.Threading.Tasks.Task Wait(this Node caller, float seconds, System.Action callback = null)
    {
        await caller.ToSignal(caller.GetTree().CreateTimer(seconds), "timeout");
        if (callback is System.Action onTimeout) onTimeout();
    }
}