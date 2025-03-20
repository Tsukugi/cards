using System.Collections.Generic;
using Godot;

public static class ListUtils
{

    public static T GetSafely<T>(this List<T> list, int index)
    {
        if (IsInsideBounds(list.Count, index)) return list[index];
        else if (index < 0) return list[^1];
        else return list[0];
    }
    // Applies circular selection.  size -> 0 || -1 -> size -1
    public static int ApplyCircularBounds(this int size, int index)
    {
        if (IsInsideBounds(size, index)) return index;
        else if (index < 0) return size - 1;
        else return 0;
    }
    public static bool IsInsideBounds(this int size, int index)
    {
        return index >= 0 && index < size;
    }


}