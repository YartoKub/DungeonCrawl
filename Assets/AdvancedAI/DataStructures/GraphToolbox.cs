using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class GraphToolbox
{

    // Бесполезная штука если нет цели
    public static void DepthFirstSearch(GraphDataStorage graph)
    {
        List<int> index = new List<int>();
        List<int> prev = new List<int>();
    }
    /// <summary>
    /// Finds path in a Graph from point A to point B. Naive implementation: picks all adjacent nodes, without relying on heuristics, and checks them
    /// </summary>
    public static List<int> FindPathNaive(GraphDataStorage graph, int start, int end)
    {
        if (start < 0 | start >= graph.vCount) return null;
        if (end < 0 | end >= graph.vCount) return null;
        List<int> index = new List<int>();
        List<int> prev = new List<int>();

        index.Add(start);
        prev .Add(-1);

        bool[] visited = new bool[graph.vCount];
        Debug.Log(DebugUtilities.DebugListString(visited) + "\n" + DebugUtilities.DebugListString(index.ToArray()) + "\n" + DebugUtilities.DebugListString(prev.ToArray()));

        int current_p = -1; // Эта переменная того чтобы отмечать где в списке кончается глубина
        int safety = 0; int safety_limit = 150;
        int answer = -1;
        while (safety < safety_limit) {
            safety += 1;
            current_p += 1;
            Debug.Log(DebugUtilities.DebugListString(visited) + "\n" + DebugUtilities.DebugListString(index.ToArray()) + "\n" + DebugUtilities.DebugListString(prev.ToArray()));
            if (current_p >= index.Count) { Debug.Log("Ran out of nodes, seems like there is no way between these two points!"); return null; }
            if (index[current_p] == end) { Debug.Log("End found successfully"); answer = current_p; break; }
            if (visited[index[current_p]]) { Debug.Log("Already visited point " + index[current_p]); continue; }

            foreach (var neigh in graph.GetSliceIDList(index[current_p]))
            {
                if (visited[neigh]) { continue; }
                index.Add(neigh);
                prev.Add(current_p);
            }
            visited[index[current_p]] = true;
        }
        if (answer == -1) return null;
        List<int> steps = new List<int>();

        Debug.Log("Answer node: " + answer + "\n" + DebugUtilities.DebugListString(visited) + "\n" + DebugUtilities.DebugListString(index.ToArray()) + "\n" + DebugUtilities.DebugListString(prev.ToArray()));
        safety = 0; safety_limit = 20;
        while (index[answer] != start && safety < safety_limit)
        {
            safety += 1;
            steps.Add(index[answer]);
            answer = prev[answer];
        }
        steps.Add(start);
        Debug.Log("ANSWER: " +  DebugUtilities.DebugListString(steps.ToArray()));
        return steps;

    }
}
