using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public class AsyncHandler(Node node)
{
    bool isLoading = false;
    readonly Node callerNode = node;

    public delegate bool SimpleCheck();

    void SetIsLoading(bool value) { GD.Print($"[{callerNode.Name}.SetIsLoading] {value}"); isLoading = value; }

    public bool GetIsLoading() { GD.Print($"[{callerNode.Name}.GetIsLoading] {isLoading}"); return isLoading; }

    public async Task AwaitBefore(Action awaitedAction, float waitTime = 1f)
    {
        SetIsLoading(true);
        await callerNode.Wait(waitTime, () =>
        {
            SetIsLoading(false);
            awaitedAction();
        });
    }
    public async Task AwaitBefore(Func<Task> awaitedAction, float waitTime = 1f)
    {
        SetIsLoading(true);
        await callerNode.Wait(waitTime);
        SetIsLoading(false);
        await awaitedAction();
    }
    public async Task AwaitForCheck(Action awaitedAction, SimpleCheck check, float timeoutInSeconds = 10f, int intervalCheckMs = 25)
    {
        SetIsLoading(true);
        float elapsedTime = 0;

        while (!check())
        {
            if (elapsedTime >= (timeoutInSeconds * 1000) && timeoutInSeconds != -1) // -1 disables the timeout
            {
                GD.PushWarning($"[AwaitForCheck] Timeout of {timeoutInSeconds}s reached");
                SetIsLoading(false);
                return; // Return on timeout
            }
            await Task.Delay(intervalCheckMs);
            elapsedTime += intervalCheckMs;
            // GD.Print($"[AwaitForCheck] Elapsed time: {elapsedTime / 1000}");
        }
        SetIsLoading(false);
        awaitedAction();
    }

    public void Debounce(Action debouncedAction, float waitTime = 1f)
    {
        if (isLoading)
        {
            var msg = $"[Debounce] Debounced {debouncedAction.GetMethodInfo().Name}";
            GD.PrintErr(msg);
            GD.PushError(msg);
            return;
        }
        debouncedAction();
        SetIsLoading(true);
        _ = callerNode.Wait(waitTime, () => SetIsLoading(false));
    }
    public static async Task RunAsyncFunctionsSequentially(List<Func<Task>> asyncFunctions)
    {
        foreach (var asyncFunction in asyncFunctions)
        {
            await asyncFunction(); // Await each function in sequence
        }
    }
}
