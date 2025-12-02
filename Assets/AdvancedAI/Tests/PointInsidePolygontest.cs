using UnityEngine;
using System.Collections.Generic;
using System;
public class PointInsidePolygontest : MonoBehaviour
{
    public List<Vector2> PolyPoints;
    public List<Vector2> pointsToCheck;
    public Vector2 point;

    public bool isConvex;
    public bool isHole;

    public bool GridTest;

    public Vector2Int grid_pos;
    public Vector2Int grid_size;
    public float grid_step;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    // Тут я попытался найти алгоритм для определеяния находится ли точка внутри полигона.
    // Мне нужно было чтобы точки на границах и совпадающие с вершинами полигона стабильно отмечались как снаружи или как внутренние
    // Идеального я не нашел.
    // Самый лучший вариант: WindingNumberAngle, использующий углы. Но у него жуткая проблемма с точностью и округлением
    void Update()
    {
        Poly2D poly = new Poly2D(PolyPoints);

        poly.Orient(isHole);


        poly.DebugDrawSelf(isHole ? Color.red : Color.green);


        for (int i = 0; i < pointsToCheck.Count; i++)
        {
            PointWWCheck(pointsToCheck[i]);
        }

        if (GridTest)
        {
            for (int x = grid_pos.x; x < grid_size.x; x++)
            {
                for (int y = grid_pos.y; y < grid_size.y; y++)
                {
                    PointJavaWWCheck(new Vector2(x * grid_step, y * grid_step));
                }
            }
        }
        bool isInside = WindingNumber(point, PolyPoints);
        DebugUtilities.DebugDrawCross(point, isInside ? Color.green : Color.red);
    }

    public void PointWWCheck(Vector2 p)
    {
        bool isInside = WindingNumber(p, PolyPoints);
        DebugUtilities.DebugDrawCross(p, isInside ? Color.green : Color.red);
    }
    public void PointJavaWWCheck(Vector2 p)
    {
        bool isInside = Poly2DToolbox.IsPointInsidePolygon(p, PolyPoints);
        DebugUtilities.DebugDrawCross(p, isInside ? Color.green : Color.red);
    }

    public float WindingNumberAngle(Vector2 position, List<Vector2> points)
    {
        double windingNumber = 0.0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];

            Vector2 pointA = position - a;
            Vector2 pointB = position - b;

            Debug.Log(Poly2DToolbox.AreaTriangle(Vector2.zero, pointA, pointB));

            double atan = Mathf.Atan2(pointA.x * pointB.y - pointA.y * pointB.x, pointA.x * pointB.x - pointA.y * pointB.y);
            //Debug.Log(atan);

            windingNumber += atan;
        }
        Debug.Log(windingNumber);
        windingNumber = windingNumber * Mathf.Rad2Deg;
        Debug.Log(Math.Round(windingNumber, 5));
        Debug.Log(windingNumber < Geo3D.epsilon);
        return 0;
    }


    // Нестабильный алгоритм, так и сяк его менял, лень чинить
    public bool WindingNumber(Vector2 position, List<Vector2> points)
    {   // Отсюда: https://github.com/blenderfan/AdvancedGamedevTutorials/blob/main/AdvancedGamedev-WindingNumbers/Polygon2D.cs
        float windingNumber = 0.0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];

            Vector2 pointA = position - a;
            Vector2 pointB = position - b;
            float d = 0;
            //if (Mathf.Abs(Poly2DToolbox.AreaTriangle(Vector2.zero, pointA, pointB)) < Geo3D.epsilon) d = 0;
            if (pointA.y * pointB.y < 0.0f)
            {
                float r = pointA.x + (pointA.y * (pointB.x - pointA.x)) / (pointA.y - pointB.y);
                Debug.Log("Intersect " + r.ToString());
                if (r < 0) d = (pointA.y < 0.0f) ? 1.0f : -1.0f;
            }
            else if (pointA.y == 0.0f)
            {
                Debug.Log("pointA > 0"); d = (pointB.y > 0.0f) ? 0.5f : -0.5f;
            }
            else if (pointB.y == 0.0f)
            {
                Debug.Log("pointB > 0"); d = (pointA.y < 0.0f) ? 0.5f : -0.5f;
            }
            Debug.Log(d);
            windingNumber += d;
        }
        Debug.Log("FINAL WINDING NUMBER: " + windingNumber);
        return ((int)windingNumber % 2) != 0;
    }


    // Граничные точки не однороднымси выходят
    public static float StupidCross(Vector2 x, Vector2 y, Vector2 z)
    {
        return (y.x - x.x) * (z.y - x.y) - (z.x - x.x) * (y.y - x.y);
    }
    public static bool StupidPointInPolygon(Vector2 point, List<Vector2> points)
    {
        float wn = 0;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];
            if (a.y <= point.y)
            {
                if (b.y > point.y && StupidCross(a, b, point) > 0)
                {
                    wn += 1;
                }
            }
            else if (b.y <= point.y && StupidCross(a, b, point) < 0)
            {
                wn -= 1;
            }
        }
        Debug.Log(wn);
        return wn != 0;
    }

    public static bool Alciatore(Vector2 point, List<Vector2> points)
    {   // Вообще не работает, переписан из псевдокода из https://www.researchgate.net/publication/325830117_An_Extension_to_Winding_Number_and_Point-in-Polygon_Algorithm
        float w = 0.0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i] - point;
            Vector2 b = points[(i + 1) % points.Count] - point;

            if (a.y * b.y >= 0)
            {
                continue;
            }
            float rx = a.x + (a.y * (b.x - a.x)) / (a.y - b.y);
            Debug.Log(rx);
            if (rx > 0)
            {   // NEGATIVE AXIS
                if (a.y < 0) w = addW(w, 1);
                if (a.y > 0) w = addW(w, -1);
                if (a.y == 0 && a.x > 0)
                {
                    if (b.y > 0) w = addW(w, 1);
                    if (b.y < 0) w = addW(w, -1);
                }
                if (b.y == 0 && b.x > 0)
                {
                    if (a.y < 0) w = addW(w, 1);
                    if (a.y > 0) w = addW(w, -1);
                }
            } 
            if (rx < 0)
            {   // POSITIVE AXIS
                if (a.y > 0) w = addW(w, -1);
                if (a.y < 0) w = addW(w, 1);
                if (a.y == 0 && a.x < 0)
                {
                    if (b.y < 0) w = addW(w, 0.5f);
                    if (b.y > 0) w = addW(w, -0.5f);
                }
                if (b.y == 0 && b.x < 0)
                {
                    if (a.y > 0) w = addW(w, 0.5f);
                    if (a.y < 0) w = addW(w, -0.5f);
                }
            }
        }
        Debug.Log(w);
        return false;
    }
    private static float addW(float w, float add)
    {
        Debug.Log(add);
        return w + add;
    }

}

