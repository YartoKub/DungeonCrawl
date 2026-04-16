using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class ArrayAndListToolbox
{
    /// <summary>
    /// Splits a looping array of values. <br/>
    /// It searches for a non-negative value as a start of a segment, and then counts following negative numbers <br/>
    /// Exumpel: <br/>
    /// Input: [-1, -1, 0, -1, -1, 3, 9, -1] <br/>
    /// Output: (2, 2), (5, 0), (6, 3)<br/>
    /// Intervals: [9, -1, -1, -1], [0, -1, -1], [3]
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static List<(int, int)> LoopedListSegmentation(int[] array)
    {   // Эту штуку вряд ли удастся реализовать через yield
        int interval_count = 0;
        for (int i = 0; i < array.Length; i++) if (array[i] >= 0) interval_count += 1;
        List<(int a, int b)> intervals = new (interval_count);

        for (int i = 0; i < array.Length; i++) if (array[i] >= 0) intervals.Add(new (i, -1));

        for (int i = 0; i < intervals.Count; i++)
            intervals[i] = (intervals[i].a, (intervals[(i + 1) % intervals.Count].a - intervals[i].a - 1 + array.Length) % array.Length);
        
        return intervals;
    }
}
