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
            if (elapsedTime > timeoutInSeconds * 1000) { isLoading = false; return; } // Return on timeout
            await Task.Delay(intervalCheckMs);
            elapsedTime += intervalCheckMs;
        }
        isLoading = false;
        awaitedAction();
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
    public async Task<IEnumerable<T>> RunSequentiallyAsync<T>(Func<Task<T>>[] operations)
    {
        var results = new List<T>();
        foreach (var op in operations)
        {
            results.Add(await op());
        }
        return results;
    }


}

public static class AsyncExtensions
{
    public static async IAsyncEnumerable<T> AwaitAll<T>(this IEnumerable<Func<Task<T>>> tasks)
    {
        foreach (var task in tasks)
            yield return await task();
    }
}