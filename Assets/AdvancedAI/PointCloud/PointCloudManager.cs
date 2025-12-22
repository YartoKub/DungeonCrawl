using System.Collections.Generic;
using UnityEngine;


public class PointCloudManager : MonoBehaviour
{
    public List<Vector2> points;
    public List<Vector2> pn_points;
    // Use this for initialization
    public Vector2 all_move_limits;
    public Vector2 loc_move_limits;
    public bool all_move, all_rotate, all_scale, move;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        if (   points != null) for (int i = 0; i <    points.Count; i++) DebugUtilities.HandlesDrawCross(   points[i], Color.yellow);
        if (pn_points != null) for (int i = 0; i < pn_points.Count; i++) DebugUtilities.HandlesDrawCross(pn_points[i], new Color(1, 0.5f, 0, 1));

    }

    public void AddPoint(Vector2 p)
    {
        points.Add(p);
        Geo3D.SortPoints(this.points);
    }

    public void RemovePoint(int i)
    {
        if (i < 0 | i >= points.Count) return;
        points.RemoveAt(i);
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
    public void DebugHighLightPoint(int index) { DebugUtilities.DrawCube(points[index], Vector3.one * 0.2f, Color.white); }
    public void HandlesHighLightPoint(int index) { DebugUtilities.HandlesDrawCube(points[index], Vector3.one * 0.2f, Color.yellow); }

    public void PurgePNPoints()
    {
        this.pn_points.Clear();
    }
    public void GeneratePseudoNewPoints() => GeneratePseudoNewPoints(all_move, all_rotate, all_scale, move);
    public void GeneratePseudoNewPoints(bool all_move, bool all_rotate, bool all_scale, bool move)
    {
        pn_points = new List<Vector2>(); // Двигаю их чуть чуть для шума
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 xyd = Vector2.zero;
            if (move) xyd = new Vector2(Random.Range(loc_move_limits.x, loc_move_limits.y), Random.Range(loc_move_limits.x, loc_move_limits.y));
            pn_points.Add(points[i] + xyd);
        }

        Vector2 global_movement = all_move ? new Vector2(Random.Range(all_move_limits.x, all_move_limits.y), Random.Range(all_move_limits.x, all_move_limits.y)) :  Vector2.zero;

        float rpi =  Random.Range(0.0f, Mathf.PI * 2);
        float cos_a = Mathf.Cos(rpi);
        float sin_a = Mathf.Sin(rpi);
        if (all_rotate)
        {
            for (int i = 0; i < pn_points.Count; i++)
            {
                Vector2 xy = pn_points[i];
                pn_points[i] = new Vector2(xy.x * cos_a - xy.y * sin_a, xy.x * sin_a + xy.y * cos_a);
            }
        }

        for (int i = 0; i < pn_points.Count; i++) pn_points[i] += global_movement;
    }

    public void ClosestPointMatching()
    {
        SingularValueDecomposition.SVDProgram.Main();
    }
}
