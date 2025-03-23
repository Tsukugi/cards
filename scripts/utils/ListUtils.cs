using System.Collections.Generic;
using System;
using Godot;

public static class ListUtils
{
    private static readonly Random rng = new();

    /// <summary>
    /// Safely retrieves an element from the list.  
    /// If the index is out of bounds, it returns the first or last element.
    /// </summary>
    /// <typeparam name="T">Type of elements in the list.</typeparam>
    /// <param name="list">The list to retrieve an element from.</param>
    /// <param name="index">The index of the desired element.</param>
    /// <returns>The element at the given index or the closest boundary element.</returns>
    public static T GetSafely<T>(this List<T> list, int index)
    {
        if (IsInsideBounds(list.Count, index)) return list[index];
        else if (index < 0) return list[^1]; // Return last element if index is negative.
        else return list[0]; // Return first element if index is out of range.
    }

    /// <summary>
    /// Applies circular indexing to wrap an index within valid bounds.
    /// If the index is out of bounds, it wraps around to the opposite boundary.
    /// </summary>
    /// <param name="size">Size of the range.</param>
    /// <param name="index">The index to apply circular bounds to.</param>
    /// <returns>A valid index within the given size. Example: index is 10, and size is 7, it will return 0.</returns>
    public static int ApplyCircularBounds(this int size, int index)
    {
        if (IsInsideBounds(size, index)) return index;
        else if (index < 0) return size - 1; // Wrap negative index to last position.
        else return 0; // Wrap overflow index to first position.
    }

    /// <summary>
    /// Checks whether the given index is within the valid bounds of a range.
    /// </summary>
    /// <param name="size">Size of the range.</param>
    /// <param name="index">Index to check.</param>
    /// <returns>True if the index is within bounds, otherwise false.</returns>
    public static bool IsInsideBounds(this int size, int index)
    {
        return index >= 0 && index < size;
    }

    /// <summary>
    /// Returns a shuffled version of the given list.
    /// </summary>
    /// <typeparam name="T">Type of elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    /// <returns>A new shuffled list.</returns>
    public static List<T> Shuffle<T>(this List<T> list)
    {
        var newList = new List<T>(list); // Create a copy of the original list.
        int n = newList.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (newList[n], newList[k]) = (newList[k], newList[n]); // Swap elements.
        }
        return newList;
    }

    /// <summary>
    /// Selects a random key from a list of strings.
    /// </summary>
    /// <param name="keyList">The list of keys to choose from.</param>
    /// <returns>A randomly selected key from the list. [0 -- list.Count -1] </returns>
    public static string GetRandKey(this List<string> keyList)
        => keyList[new Random().Next(keyList.Count)];
}