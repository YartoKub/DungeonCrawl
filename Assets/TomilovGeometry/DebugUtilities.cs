using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
public static class DebugUtilities
{
    public static void DebugDrawCross(Vector2 point, Color color, float time = 0.01f) {
        float x = point.x; float y = point.y;
        Debug.DrawLine(new Vector3(x, y - 0.1f), new Vector3(x, y + 0.1f), color, time);
        Debug.DrawLine(new Vector3(x - 0.1f, y), new Vector3(x + 0.1f, y), color, time);
    }
    public static void HandlesDrawCross(Vector2 point, Color color, float time = 0.01f)
    {
        float x = point.x; float y = point.y;
        Color tmp_color = Handles.color;
        Handles.color = color;
        Handles.DrawLine(new Vector3(x, y - 0.1f), new Vector3(x, y + 0.1f));
        Handles.DrawLine(new Vector3(x - 0.1f, y), new Vector3(x + 0.1f, y));
        Handles.color = tmp_color;
    }

    public static void DebugDrawLine(Vector2 point1, Vector2 point2, Color color, float time = 0.01f, float coneLength = 0.2f) {
        Debug.DrawLine(point1, point2, color, time);
        Vector2 dir = (point2 - point1).normalized * coneLength;
        Vector2 leftHand = rotate(dir, 2.6f);
        Vector2 rightHand = rotate(dir, -2.6f);
        Debug.DrawLine(point2, point2 + leftHand, color, time);
        Debug.DrawLine(point2, point2 + rightHand, color, time);
    }
    public static void HandlesDrawLine(Vector2 point1, Vector2 point2, Color color, float coneLength = 0.2f)
    {
        Color tmp = Handles.color;
        Handles.color = color;
        Handles.DrawLine(point1, point2);
        Vector2 dir = (point2 - point1).normalized * coneLength;
        Vector2 leftHand = rotate(dir, 2.6f);
        Vector2 rightHand = rotate(dir, -2.6f);
        Handles.DrawLine(point2, point2 + leftHand);
        Handles.DrawLine(point2, point2 + rightHand);
        Handles.color = tmp;
    }


    public static Vector2 rotate(Vector2 v, float delta) {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }
    public static void DebugDrawSquare(Vector2 point, Color color, float size = 0.1f, float time = 0.01f) {
        float x = point.x; float y = point.y;
        Debug.DrawLine(new Vector3(x - size, y - size), new Vector3(x - size, y + size), color, time);
        Debug.DrawLine(new Vector3(x - size, y + size), new Vector3(x + size, y + size), color, time);
        Debug.DrawLine(new Vector3(x + size, y + size), new Vector3(x + size, y - size), color, time);
        Debug.DrawLine(new Vector3(x + size, y - size), new Vector3(x - size, y - size), color, time);
    }
    public static void DebugDrawSquare(Vector2 p1, Vector2 p2, Color color, float time = 0.01f)
    {
        Debug.DrawLine(p1, new Vector3(p1.x, p2.y), color, time);
        Debug.DrawLine(p1, new Vector3(p2.x, p1.y), color, time);
        Debug.DrawLine(new Vector3(p1.x, p2.y), p2, color, time);
        Debug.DrawLine(new Vector3(p2.x, p1.y), p2, color, time);
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
    public static void DebugUltraLineHedgehog(Vector3 point1, Vector3 point2, Color color, float time = 1f, float coneLength = 0.2f)
    {
        //time = time * 0.01f;
        Debug.DrawLine(point1, point2, color, time);
        if (coneLength == 0.0f) return;
        DebugUltraHedgehog(point2, color, time, coneLength);
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

    public static void DrawCube(Vector3 center, Vector3 size, Color color)
    {

        Vector3 pos = Vector3.zero;

        Vector3 xyz = new Vector3(-size.x, -size.y, -size.z) * 0.5f + pos + center;

        Vector3 ayz = new Vector3(size.x, -size.y, -size.z) * 0.5f + pos + center;
        Vector3 xbz = new Vector3(-size.x, size.y, -size.z) * 0.5f + pos + center;
        Vector3 xyc = new Vector3(-size.x, -size.y, size.z) * 0.5f + pos + center;

        Vector3 abc = new Vector3(size.x, size.y, size.z) * 0.5f + pos + center;

        Vector3 xbc = new Vector3(-size.x, size.y, size.z) * 0.5f + pos + center;
        Vector3 ayc = new Vector3(size.x, -size.y, size.z) * 0.5f + pos + center;
        Vector3 abz = new Vector3(size.x, size.y, -size.z) * 0.5f + pos + center;

        Debug.DrawLine(xyz, ayz, color);
        Debug.DrawLine(xyz, xbz, color);
        Debug.DrawLine(xyz, xyc, color);

        Debug.DrawLine(abc, xbc, color);
        Debug.DrawLine(abc, ayc, color);
        Debug.DrawLine(abc, abz, color);

        Debug.DrawLine(ayz, ayc, color);
        Debug.DrawLine(ayz, abz, color);

        Debug.DrawLine(xbz, xbc, color);
        Debug.DrawLine(xbz, abz, color);

        Debug.DrawLine(xyc, ayc, color);
        Debug.DrawLine(xyc, xbc, color);
    }
    public static void HandlesDrawCube(Vector3 center, Vector3 size, Color color)
    {
        Vector3 pos = Vector3.zero;

        Vector3 xyz = new Vector3(-size.x, -size.y, -size.z) * 0.5f + pos + center;

        Vector3 ayz = new Vector3(size.x, -size.y, -size.z) * 0.5f + pos + center;
        Vector3 xbz = new Vector3(-size.x, size.y, -size.z) * 0.5f + pos + center;
        Vector3 xyc = new Vector3(-size.x, -size.y, size.z) * 0.5f + pos + center;

        Vector3 abc = new Vector3(size.x, size.y, size.z) * 0.5f + pos + center;

        Vector3 xbc = new Vector3(-size.x, size.y, size.z) * 0.5f + pos + center;
        Vector3 ayc = new Vector3(size.x, -size.y, size.z) * 0.5f + pos + center;
        Vector3 abz = new Vector3(size.x, size.y, -size.z) * 0.5f + pos + center;

        Color tmp_color = Handles.color;
        Handles.color = color;
        Handles.DrawLine(xyz, ayz);
        Handles.DrawLine(xyz, xbz);
        Handles.DrawLine(xyz, xyc);

        Handles.DrawLine(abc, xbc);
        Handles.DrawLine(abc, ayc);
        Handles.DrawLine(abc, abz);

        Handles.DrawLine(ayz, ayc);
        Handles.DrawLine(ayz, abz);

        Handles.DrawLine(xbz, xbc);
        Handles.DrawLine(xbz, abz);

        Handles.DrawLine(xyc, ayc);
        Handles.DrawLine(xyc, xbc);
        Handles.color = tmp_color;
    }

    public static void HandlesDrawRectangle(Vector2 p1, Vector2 p2, Color color, float time = 0.01f)
    {
        Color tmp_color = Handles.color;
        Handles.color = color;
        Handles.DrawLine(p1, new Vector3(p1.x, p2.y));
        Handles.DrawLine(p1, new Vector3(p2.x, p1.y));
        Handles.DrawLine(new Vector3(p1.x, p2.y), p2);
        Handles.DrawLine(new Vector3(p2.x, p1.y), p2);
        Handles.color = tmp_color;
    }
    public enum GradientOption { RYG, Lerp, HSVGradient, Rainbow_Looped, Rainbow_Red2Violet };
    public static Color PickGradient(int current_depth, int max_depth, GradientOption option)
    {
        switch (option)
        {
            case DebugUtilities.GradientOption.RYG:
                return DebugUtilities.RYG_Gradient(current_depth, max_depth);

            case DebugUtilities.GradientOption.Rainbow_Looped:
                return DebugUtilities.RainbowGradient_Looped(current_depth, max_depth);

            case DebugUtilities.GradientOption.Rainbow_Red2Violet:
                return DebugUtilities.RainbowGradient_Red2Violet(current_depth, max_depth);
            default:
                return Color.black;
        }
    }
    public static Color RYG_Gradient(int current_depth, int max_depth)
    {
        //max_depth = max_depth - 1;
        float ratio = ((float)current_depth / max_depth);

        if (ratio < 0.5f)
            return new Color(1, ratio * 2, 0);
        else
            return new Color(1 - (ratio - 0.5f) * 2, 1, 0);
    }
    public static Color LerpGradient(Color a, Color b, int current_depth, int max_depth)
    {
        max_depth = max_depth - 1;
        float ratio = ((float)current_depth / max_depth);
        return Color.Lerp(a, b, ratio);
    }

    public static Color RainbowGradient_Looped(int current_depth, int max_depth)
    {
        //max_depth = max_depth;
        float ratio = ((float)current_depth / max_depth);
        return Color.HSVToRGB(ratio, 1, 1);
    }
    public static Color RainbowGradient_Red2Violet(int current_depth, int max_depth)
    {
        //max_depth = max_depth;
        float ratio = ((float)current_depth / max_depth);
        //Debug.Log(ratio + " " + max_depth + " " + current_depth);
        return Color.HSVToRGB(ratio * 0.833f, 1.0f, 1.0f);
    }
    /// <summary>
    ///  Этот градиент работает странно, но работает.
    ///  Я не знаю/мне лень делать его лучше
    /// </summary>
    public static Color HSVGradient(Color a, Color b, int current_depth, int max_depth)
    {
        Color.RGBToHSV(a, out float aH, out float aS, out float aV);
        Color.RGBToHSV(b, out float bH, out float bS, out float bV);
        max_depth = max_depth - 1;
        float ratio = ((float)current_depth / max_depth);
        float AB_distance = 0; float BA_distance = 0; float useable_distance;
        if (aH > bH) {
            AB_distance = 1.0f - aH + bH;
            BA_distance = aH - bH;
        } else {
            AB_distance = bH - aH;
            BA_distance = 1.0f - bH + aH;
        }
        if (AB_distance < BA_distance) useable_distance = AB_distance;
        else useable_distance = -BA_distance;

        float nH = aH + useable_distance * ratio;
        nH = nH < 0.0f ? nH + 1.0f : nH;
        float nS = aS + (bS - aS) * ratio;
        float nV = aV + (bV - aV) * ratio;
        //Debug.Log(aH + " " + aS + " " + aV);
        //Debug.Log(bH + " " + bS + " " + bV);
        //Debug.Log(nH + " " + nS + " " + nV);

        return Color.HSVToRGB(nH, nS, nV) ;
    }

    public static void DebugParams<T>(string separator = ", ", params T[] parpar)
    {
        string n = "";
        for (int i = 0; i < parpar.Length; i++) n += parpar[i] + separator;
        Debug.Log(n);
    }
    public static void DebugList<T>(List<T> list, string separator = ", ")
    {
        DebugList(list.ToArray(), separator);
    }
    public static void DebugList<T>(T[] list, string separator = ", ")
    {
        string n = ""; for (int i = 0; i < list.Length; i++) n += list[i] + separator; Debug.Log(n);
    }

}

