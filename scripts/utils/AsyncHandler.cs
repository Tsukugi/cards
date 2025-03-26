using System;
using Godot;

public class AsyncHandler
{
    bool isLoading = false;
    Node callerNode;
    public AsyncHandler(Node node)
    {
        callerNode = node;
    }
    public void AwaitBefore(Action awaitedAction, float waitTime = 1f)
    {
        isLoading = true;
        _ = callerNode.Wait(waitTime, () =>
        {
            isLoading = false; awaitedAction();
        });
    }

    public void Debounce(Action debouncedAction, float waitTime = 1f)
    {
        if (isLoading)
        {
            GD.Print($"[Debounce] Debounced {debouncedAction.GetType()}");
            return;
        }
        debouncedAction();
        isLoading = true;
        _ = callerNode.Wait(waitTime, () => isLoading = false);
    }
}