using UnityEngine;

public static class BoundsMathHelper
{

    public static BoundsInt Intersect(BoundsInt roomA, BoundsInt roomB)
    {
        Vector3Int myA = roomA.min; Vector3Int myB = roomA.max;
        Vector3Int otherA = roomB.min; Vector3Int otherB = roomB.max;

        Vector3Int[] preview = new Vector3Int[2];
        preview[0] = new Vector3Int(
            (int)Mathf.Clamp(myB.x, otherA.x, otherB.x),
            (int)Mathf.Clamp(myB.y, otherA.y, otherB.y),
            (int)Mathf.Clamp(myB.z, otherA.z, otherB.z)
            );
        preview[1] = new Vector3Int(
            (int)Mathf.Clamp(otherA.x, myA.x, myB.x),
            (int)Mathf.Clamp(otherA.y, myA.y, myB.y),
            (int)Mathf.Clamp(otherA.z, myA.z, myB.z)
            );

        Vector3Int[] toReturn = new Vector3Int[2];
        toReturn[0] = new Vector3Int(
            (int)Mathf.Min(preview[0].x, preview[1].x),
            (int)Mathf.Min(preview[0].y, preview[1].y),
            (int)Mathf.Min(preview[0].z, preview[1].z)
            );
        toReturn[1] = new Vector3Int(
            (int)Mathf.Max(preview[0].x, preview[1].x),
            (int)Mathf.Max(preview[0].y, preview[1].y),
            (int)Mathf.Max(preview[0].z, preview[1].z)
            );
        BoundsInt intBounds = new BoundsInt();
        intBounds.SetMinMax(toReturn[0], toReturn[1]);
        return intBounds;
    }
    public static Bounds Intersect(Bounds roomA, Bounds roomB)
    {
        Vector3 myA = roomA.min; Vector3 myB = roomA.max;
        Vector3 otherA = roomB.min; Vector3 otherB = roomB.max;

        Vector3[] preview = new Vector3[2];
        preview[0] = new Vector3(
            Mathf.Clamp(myB.x, otherA.x, otherB.x),
            Mathf.Clamp(myB.y, otherA.y, otherB.y),
            Mathf.Clamp(myB.z, otherA.z, otherB.z)
            );
        preview[1] = new Vector3(
            Mathf.Clamp(otherA.x, myA.x, myB.x),
            Mathf.Clamp(otherA.y, myA.y, myB.y),
            Mathf.Clamp(otherA.z, myA.z, myB.z)
            );

        Vector3[] toReturn = new Vector3[2];
        toReturn[0] = new Vector3(
            Mathf.Min(preview[0].x, preview[1].x),
            Mathf.Min(preview[0].y, preview[1].y),
            Mathf.Min(preview[0].z, preview[1].z)
            );
        toReturn[1] = new Vector3(
            Mathf.Max(preview[0].x, preview[1].x),
            Mathf.Max(preview[0].y, preview[1].y),
            Mathf.Max(preview[0].z, preview[1].z)
            );
        Bounds intBounds = new Bounds();
        intBounds.SetMinMax(toReturn[0], toReturn[1]);
        return intBounds;
    }


    public static BoundsInt ExpandToInclude(BoundsInt roomA, BoundsInt roomB)
    {
        Vector3Int newMin = new Vector3Int(
            (int)Mathf.Min(roomA.xMin, roomB.xMin),
            (int)Mathf.Min(roomA.yMin, roomB.yMin),
            (int)Mathf.Min(roomA.zMin, roomB.zMin)
            );
        Vector3Int newMax = new Vector3Int(
            (int)Mathf.Max(roomA.xMax, roomB.xMax),
            (int)Mathf.Max(roomA.yMax, roomB.yMax),
            (int)Mathf.Max(roomA.zMax, roomB.zMax)
            );
        BoundsInt toReturn = new BoundsInt();
        toReturn.SetMinMax(newMin, newMax);
        return toReturn;
    }

    public static Vector3[] Get_8_Corners(Bounds bounds)
    {
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);

        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        return corners;
    }
    public static Vector3Int[] Get_8_Corners(BoundsInt bounds)
    {
        Vector3Int[] corners = new Vector3Int[8];
        corners[0] = new Vector3Int(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3Int(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3Int(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3Int(bounds.min.x, bounds.max.y, bounds.max.z);

        corners[4] = new Vector3Int(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3Int(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3Int(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = new Vector3Int(bounds.max.x, bounds.max.y, bounds.max.z);

        return corners;
    }
    public static float CalculateSurfaceArea(Bounds box)
    {
        float width = box.min.x - box.max.x;
        float height = box.min.y - box.max.y;
        float depth = box.min.z - box.max.z;

        float surfaceArea = 2 * (width * height + width * depth + height * depth);
        return surfaceArea;
    }
    public static int CalculateSurfaceArea(BoundsInt box) // INT
    {
        int width = box.min.x - box.max.x;
        int height = box.min.y - box.max.y;
        int depth = box.min.z - box.max.z;

        int surfaceArea = 2 * (width * height + width * depth + height * depth);
        return surfaceArea;
    }
    public static int CalculateVolume(BoundsInt box)
    {
        return box.size.x * box.size.y * box.size.z;
    }

    public static int Voluminosity(BoundsInt box) // Возвращает количество измерений не равных 0
    {
        return ((box.size.x == 0) ? 0 : 1) + ((box.size.y == 0) ? 0 : 1) + ((box.size.z == 0) ? 0 : 1);
    }
    public static bool IsDot(BoundsInt box)
    {
        return Voluminosity(box) == 0;
    }
    public static bool IsLine(BoundsInt box)
    {
        return Voluminosity(box) == 1;
    }
    public static bool IsSquare(BoundsInt box)
    {
        return Voluminosity(box) == 2;
    }
    public static bool IsBox(BoundsInt box)
    {
        return Voluminosity(box) == 3;
    }

    public static int Voluminosity(Bounds box) // Возвращает количество измерений не равных 0
    {
        return box.size.x == 0 ? 0 : 1 + box.size.y == 0 ? 0 : 1 + box.size.z == 0 ? 0 : 1;
    }
    public static bool IsDot(Bounds box)
    {
        return Voluminosity(box) == 0;
    }
    public static bool IsLine(Bounds box)
    {
        return Voluminosity(box) == 1;
    }
    public static bool IsSquare(Bounds box)
    {
        return Voluminosity(box) == 2;
    }
    public static bool IsBox(Bounds box)
    {
        return Voluminosity(box) == 3;
    }

    public static void DebugDrawBox(Vector3 min, Vector3 size)
    {
        Vector3 xyz = min;

        Vector3 ayz = min + new Vector3(size.x, 0, 0);
        Vector3 xbz = min + new Vector3(0, size.y, 0);
        Vector3 xyc = min + new Vector3(0, 0, size.z);

        Vector3 xbc = min + new Vector3(0, size.y, size.z);
        Vector3 ayc = min + new Vector3(size.x, 0, size.z);
        Vector3 abz = min + new Vector3(size.x, size.y, 0);

        Vector3 abc = min + new Vector3(size.x, size.y, size.z);

        Debug.DrawLine(xyz, ayz);
        Debug.DrawLine(xyz, xbz);
        Debug.DrawLine(xyz, xyc);

        Debug.DrawLine(abc, xbc);
        Debug.DrawLine(abc, ayc);
        Debug.DrawLine(abc, abz);

        Debug.DrawLine(ayz, ayc);
        Debug.DrawLine(ayz, abz);

        Debug.DrawLine(xbz, xbc);
        Debug.DrawLine(xbz, abz);

        Debug.DrawLine(xyc, ayc);
        Debug.DrawLine(xyc, xbc);
    }
    public static void DebugDrawBox(Vector3 min, Vector3 size, Color color)
    {
        Vector3 xyz = min;

        Vector3 ayz = min + new Vector3(size.x, 0, 0);
        Vector3 xbz = min + new Vector3(0, size.y, 0);
        Vector3 xyc = min + new Vector3(0, 0, size.z);

        Vector3 xbc = min + new Vector3(0, size.y, size.z);
        Vector3 ayc = min + new Vector3(size.x, 0, size.z);
        Vector3 abz = min + new Vector3(size.x, size.y, 0);

        Vector3 abc = min + new Vector3(size.x, size.y, size.z);

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



    public static bool Intersects(BoundsInt a, BoundsInt b)
    { // Почему у boundsint нет intersect-а?
        return (a.xMin <= b.xMax) && (a.xMax >= b.xMin) &&
            (a.yMin <= b.yMax) && (a.yMax >= b.yMin) &&
            (a.zMin <= b.zMax) && (a.zMax >= b.zMin);
    }

    public static bool DoesContainPoint(BoundsInt a, Vector3 point)
    {
        return
            point.x <= a.xMax && point.x >= a.xMin &&
            point.y <= a.yMax && point.y >= a.yMin &&
            point.z <= a.zMax && point.z >= a.zMin;
    }
    public static float CenterDistance(Bounds a, Bounds b)
    {
        return (a.center - b.center).magnitude;
    }
    public static float CenterDistance(BoundsInt a, BoundsInt b)
    {
        return (a.center - b.center).magnitude;
    }

    public static int GetCutArea(BoundsInt bounds, Vector3Int checkPlane) {
        if (checkPlane.x != 0) return bounds.size.y * bounds.size.z;
        if (checkPlane.y != 0) return bounds.size.x * bounds.size.z;
        if (checkPlane.z != 0) return bounds.size.x * bounds.size.y;
        return 0;
    }

    public static bool CanGetCut(BoundsInt bounds, Vector3Int checkPlane, Vector3Int valuePlane) {
        if (checkPlane.x != 0) return bounds.xMin < valuePlane.x && bounds.xMax > valuePlane.x;
        if (checkPlane.y != 0) return bounds.yMin < valuePlane.y && bounds.yMax > valuePlane.y;
        if (checkPlane.z != 0) return bounds.zMin < valuePlane.z && bounds.zMax > valuePlane.z;
        return false;
    }

    public static int GetCutAreaSafe(BoundsInt bounds, Vector3Int checkPlane, Vector3Int valuePlane)
    { // Вычисляет площадь разреза, в случае непрохождения проверки на разрез возвращает 0
        if (checkPlane.x != 0) return (bounds.xMin < valuePlane.x && bounds.xMax > valuePlane.x) ? bounds.size.y * bounds.size.z : 0;
        if (checkPlane.y != 0) return (bounds.yMin < valuePlane.y && bounds.yMax > valuePlane.y) ? bounds.size.x * bounds.size.z : 0;
        if (checkPlane.z != 0) return (bounds.zMin < valuePlane.z && bounds.zMax > valuePlane.z) ? bounds.size.x * bounds.size.y : 0;
        return 0;
    }

    public static BoundsInt[] GetCut(BoundsInt bounds, Vector3Int checkPlane, Vector3Int valuePlane)
    { // Может дать некорректные ответы если не провести проверку на возможность разреза
        BoundsInt[] splits = new BoundsInt[2];
        if (checkPlane.x != 0)
        {
            splits[0].SetMinMax(bounds.min, new Vector3Int(valuePlane.x, bounds.yMax, bounds.zMax));
            splits[1].SetMinMax(new Vector3Int(valuePlane.x, bounds.yMin, bounds.zMin), bounds.max);
        }
        if (checkPlane.y != 0)
        {
            splits[0].SetMinMax(bounds.min, new Vector3Int(bounds.xMax, valuePlane.y, bounds.zMax));
            splits[1].SetMinMax(new Vector3Int(bounds.xMin, valuePlane.y, bounds.zMin), bounds.max);
        }
        if (checkPlane.z != 0)
        {
            splits[0].SetMinMax(bounds.min, new Vector3Int(bounds.xMax, bounds.yMax, valuePlane.z));
            splits[1].SetMinMax(new Vector3Int(bounds.xMin, bounds.yMin, valuePlane.z), bounds.max);
        }
        return splits;
    }

    // Note: Не возвращает точку пересечения
    public static bool DoesLineIntersectBoundingBox2D(Vector2 L1, Vector2 L2, Bounds BBox)
    {
        Vector2 B1 = BBox.min; Vector2 B2 = BBox.max;
        if (L2.x < B1.x && L1.x < B1.x) return false;
        if (L2.x > B2.x && L1.x > B2.x) return false;
        if (L2.y < B1.y && L1.y < B1.y) return false;
        if (L2.y > B2.y && L1.y > B2.y) return false;
        if (L1.x > B1.x && L1.x < B2.x && L1.y > B1.y && L1.y < B2.y) return true;

        return BBoxLineEquation2D(BBox, GetLineEquation(L1, L2));
    }

    public static bool DoesPlaneIntersectBox(Plane plane, Bounds BBox)
    {
        Vector3[] corvers = Get_8_Corners(BBox);
        Poly3D.Type opposite = Poly3D.Type.SamePlane;
        for (int i = 0; i < corvers.Length; i++)
        {
            Poly3D.Type type = Poly3D.PlaneSide(plane, corvers[i]);
            if (type == Poly3D.Type.SamePlane) return true;
            opposite |= type;
            if (opposite == Poly3D.Type.Intersects) return true;

        }

        return false;
    }


    // 
    public static Vector3 GetLineEquation(Vector2 a, Vector2 b)
    {
        if (a.x == b.x && a.y == b.y) return Vector3.zero;

        float A = a.y - b.y;
        float B = b.x - a.x;
        float C = a.x * b.y - b.x * a.y;

        return new Vector3(A, B, C);
    }


    public static bool BBoxLineEquation2D(Bounds BBox, Vector3 lineEq)
    {
        Vector2 a = BBox.min;               Vector2 c = BBox.max; 
        Vector2 b = new Vector2(a.x, c.y);  Vector2 d = new Vector2(c.x, a.y);

        if (lineEq == Vector3.zero) return false;
        // 0 точное попадание в точку
        // -n точка ниже
        // +n точка выше
        // Если есть разница между точками по знаку то есть пересечение
        //float signA = Mathf.Sign(lineEq.x * a.x + lineEq.y * a.y + lineEq.z);
        float signA = Mathf.Sign(PointAboveOrBelowLine(lineEq, a));
        if (signA == 0) return true;
        float signB = Mathf.Sign(PointAboveOrBelowLine(lineEq, b));
        if (signB == 0) return true;
        float signC = Mathf.Sign(PointAboveOrBelowLine(lineEq, c));
        if (signC == 0) return true;
        float signD = Mathf.Sign(PointAboveOrBelowLine(lineEq, d));
        if (signD == 0) return true;
        //Debug.Log(signA.ToString() + " " + signB.ToString() + " " + signC.ToString() + " " + signD.ToString());
        // если они все одинаковы, значит коробка расположена по ту сторону прямой
        if ((signA == signB) && (signB == signC) && (signC == signD)) return false;
        return true;
    }

    private static float PointAboveOrBelowLine(Vector3 lineEq, Vector2 p)
    {
        //float y = -lineEq.x / lineEq.y * p.x - lineEq.z / lineEq.y;
        //Debug.Log(y.ToString() + " " + p.y.ToString() + " " + (y < p.y).ToString());
        return lineEq.x * p.x + lineEq.y * p.y + lineEq.z;
    }






}
