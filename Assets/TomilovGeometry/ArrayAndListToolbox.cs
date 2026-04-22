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
    /// <summary>
    /// It expects that inputs are intervals, that do not overlap, but may have the same start or end point.  <br/>
    /// Intervals may loop, in this case, the breakpoint of max start and min end is found. It will be found either at the start or at the end. <br/>
    /// Tries to link elements that have matching A.end and B.start or B.end and A.start. <br/>
    /// If there is no match, a comparison of A.end and B.start is made, and they are placed in a way so bigger value is next, and small value is before. <br/>
    /// If your intervals are looping, expect that first or last element will have both positive and negative numbers. <br/>
    /// Under the hood, it uses Bubble Sort, because i do not have time to use smart algorythms.
    /// </summary>
    /// <param name="intervals"></param>
    /// <returns></returns>
    public static List<int> NonOverlappingIntervalLinker(List<(float a, float b, int origin)> intervals)
    {
        int min_break_point = 0;
        int max_break_point = 0;
        float min_value = intervals[0].b;
        float max_value = intervals[0].a;
        for (int i = 1; i < intervals.Count; i++)
        {
            if (intervals[i].a >= max_value) { max_value = intervals[i].a; max_break_point = i; }
            if (intervals[i].b <= min_value) { min_value = intervals[i].b; min_break_point = i; }
        }

        int n = intervals.Count;
        if (max_break_point == min_break_point) (intervals[min_break_point], intervals[0]) = (intervals[0], intervals[min_break_point]);
        
        for (int i = 0; i < n; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - i - 1; j++)
            {
                if (!(intervals[j].a >= intervals[j + 1].b)) continue;
                (intervals[j], intervals[j + 1]) = (intervals[j + 1], intervals[j]);
                swapped = true;
            }
            if (!swapped) break;
        }
        List<int> order = new List<int>(intervals.Count);
        for (int i = 0; i < intervals.Count; i++) order.Add(intervals[i].origin);
        return order;
    }
    public static List<int> NonOverlappingIntervalLinker(List<(float, float, int)> intervals, bool boool)
    {
        List<(float a, float b, int origin)> sorted = new(intervals.Count);
        sorted.Add(intervals[0]);
        for (int i = 1; i < intervals.Count; i++) 
            Position(sorted, intervals[i]);
        
        List<int> order = new List<int>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++) order.Add(sorted[i].origin);
        return order;

        void Position(List<(float a, float b, int origin)>  sorted, (float a, float b, int origin) new_value)
        {
            int insert = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                Debug.Log(new_value.b + " " + sorted[i].a);
                if (new_value.b <= sorted[i].a) break;
                if (new_value.a >= sorted[i].b) { insert = i + 1; }

            }

            sorted.Insert(insert, new_value);
        }
    }

}
