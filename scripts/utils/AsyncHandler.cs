using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class AsyncHandler(Node node)
{
    bool isLoading = false;
    readonly Node callerNode = node;

    public delegate bool SimpleCheck();

    public void AwaitBefore(Action awaitedAction, float waitTime = 1f)
    {
        isLoading = true;
        _ = callerNode.Wait(waitTime, () =>
        {
            isLoading = false; awaitedAction();
        });
    }
    public async Task AwaitForCheck(Action awaitedAction, SimpleCheck check, float timeoutInSeconds = 10f, int intervalCheckMs = 25)
    {
        isLoading = true;
        float elapsedTime = 0;

        while (!check())
        {
            if (elapsedTime > timeoutInSeconds * 10000) { isLoading = false; return; } // Return on timeout
            await Task.Delay(intervalCheckMs);
            elapsedTime += intervalCheckMs;
            // GD.Print($"[AwaitForCheck] Elapsed time: {elapsedTime}");
        }
        isLoading = false;
        awaitedAction();
    }

    public void Debounce(Action debouncedAction, float waitTime = 1f)
    {
        if (isLoading)
        {
            // GD.PushWarning($"[Debounce] Debounced {debouncedAction.GetType()}");
            return;
        }
        debouncedAction();
        isLoading = true;
        _ = callerNode.Wait(waitTime, () => isLoading = false);
    }
    public static async Task RunAsyncFunctionsSequentially(List<Func<Task>> asyncFunctions)
    {
        foreach (var asyncFunction in asyncFunctions)
        {
            await asyncFunction(); // Await each function in sequence
        }
    }
}
