using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugUtilities
{
    public static void DebugDrawCross(Vector2 point, Color color, float time = 0.01f) {
        float x = point.x; float y = point.y;
        Debug.DrawLine(new Vector3(x, y - 0.1f), new Vector3(x, y + 0.1f), color, time);
        Debug.DrawLine(new Vector3(x - 0.1f, y), new Vector3(x + 0.1f, y), color, time);
    }

    public static void DebugDrawLine(Vector2 point1, Vector2 point2, Color color, float time = 0.01f, float coneLength = 0.2f) {
        Debug.DrawLine(point1, point2, color, time);
        Vector2 dir = (point2 - point1).normalized * coneLength;
        Vector2 leftHand = rotate(dir, 2.6f);
        Vector2 rightHand = rotate(dir, -2.6f);
        Debug.DrawLine(point2, point2 + leftHand, color, time);
        Debug.DrawLine(point2, point2 + rightHand, color, time);
    }

    public static Vector2 rotate(Vector2 v, float delta) {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }
    public static void DebugDrawSquare(Vector2 point, Color color, float time = 0.01f) {
        float x = point.x; float y = point.y;
        Debug.DrawLine(new Vector3(x - 0.1f, y - 0.1f), new Vector3(x - 0.1f, y + 0.1f), color, time);
        Debug.DrawLine(new Vector3(x - 0.1f, y + 0.1f), new Vector3(x + 0.1f, y + 0.1f), color, time);
        Debug.DrawLine(new Vector3(x + 0.1f, y + 0.1f), new Vector3(x + 0.1f, y - 0.1f), color, time);
        Debug.DrawLine(new Vector3(x + 0.1f, y - 0.1f), new Vector3(x - 0.1f, y - 0.1f), color, time);
    }
    public static void DrawPoopyCircle(Vector2 center, float R, int p, Color color)
    {
        float angle = Mathf.PI * 2 / p;
        for (int i = 0; i < p; i++)
        {
            Vector2 p1 = new Vector2(Mathf.Cos(angle * i), Mathf.Sin(angle * i));
            Vector2 p2 = new Vector2(Mathf.Cos(angle * (i+1)), Mathf.Sin(angle * (i+1)));
            Debug.DrawLine(center + p1 * R, center + p2 * R, color );
        }
    }

    public static void DebugUltraLine(Vector3 point1, Vector3 point2, Color color, float time = 1f, float coneLength = 0.2f)
    {
        //time = time * 0.01f;
        Debug.DrawLine(point1, point2, color, time);
        Vector3 dir = (point2 - point1).normalized * coneLength;
        Vector3 leftHand = rotate(dir, 2.6f);
        Vector3 rightHand = rotate(dir, -2.6f);
        Debug.DrawLine(point2, point2 + leftHand, color, time);
        Debug.DrawLine(point2, point2 + rightHand, color, time);
    }

    public static void DebugUltraHedgehog(Vector3 centerPoint, Color color, float time = 1f, float size = 0.2f)
    {
        Debug.DrawLine(centerPoint + new Vector3(-1, -1,-1) * size, centerPoint + new Vector3(1, 1,  1) * size, color, time);
        Debug.DrawLine(centerPoint + new Vector3(-1, -1, 1) * size, centerPoint + new Vector3(1, 1, -1) * size, color, time);
        Debug.DrawLine(centerPoint + new Vector3(1, -1, -1) * size, centerPoint + new Vector3(-1, 1, 1) * size, color, time);
        Debug.DrawLine(centerPoint + new Vector3(-1, 1, -1) * size, centerPoint + new Vector3(1, -1, 1) * size, color, time);
    }


    public static void DrawPolygon(List<Vector3> points, Color color, float time)
    {
        if (points.Count  < 2) return;
        for (int i = 0; i < points.Count - 1; i++) DebugUltraLine(points[i], points[i + 1], color, time);
        DebugUltraLine(points[points.Count - 1], points[0], color, time);
    }




}
