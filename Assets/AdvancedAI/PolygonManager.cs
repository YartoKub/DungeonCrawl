using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class PolygonManager : MonoBehaviour
{
    // Singleton  ласс, хран€щий в себе поле с полигонами.
    // ќбеспечивает доступ агентов к полигонам
    // ѕредоставл€ет инструменты дл€ редактировани€ полигонов
    public enum actions { none, knife, grab, select}
    public static PolygonManager manager;
    private PolygonManager() {}
    public PolygonManager GetManager()
    {
        if (manager == null) manager = new PolygonManager();
        return manager;
    }

    public List<Vector2> points;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < points.Count; i++)
        {
            DebugUtilities.HandlesDrawCross(points[i], Color.cyan);
        }
    }

    public void AddPoint(Vector2 p)
    {
        this.points.Add(p);
    }
    public void RemovePoint(int p_index)
    {
        if (p_index < 0 || p_index >= this.points.Count) return;
        this.points.RemoveAt(p_index);
    }

    public (int, float) ClosestPoint(Vector2 p)
    {
        int min = -1; float min_d = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float d = (p - points[i]).magnitude;
            if (d < min_d)
            {
                min = i;
                min_d = d;
            }
        }
        return (min, min_d);
    }

    public void HighLightPoint(int index)
    {
        //Handles.DrawWireCube(points[index], Vector3.one * 0.2f);
        DebugUtilities.DrawCube(points[index], Vector3.one * 0.2f, Color.white);
    }
    public void HighLightPointHandles(int index)
    {
        DebugUtilities.HandlesDrawCube(points[index], Vector3.one * 0.2f, Color.yellow);
    }
}

