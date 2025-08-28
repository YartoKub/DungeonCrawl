using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Rect3D
{
    public Bounds bounds;

    public Vector3Int A;
    public Vector3Int B;
    public Vector3Int size { get { return B - A; } }

    //public Rect3D[] children;


    public Vector3 Center { get { return bounds.center; } }

    public Rect3D(Vector3Int A, Vector3Int B, int parentID = -1)
    {
        bounds = new Bounds(A, B);

        this.A = A;
        this.B = B;
        //this.parentID = parentID;
    }



    public override string ToString()
    {
        return "Rect3D " + bounds.min.ToString() + " " + bounds.max.ToString();
    }

    public Vector3 MoveTowardsPoint(Vector3 targetPoint, Rect3D rectToCheck, float Distance)
    {
        Vector3[] corners = Get_8_Corners();

        Vector3 direction = (this.Center - targetPoint).normalized;

        Debug.Log("MoveTowardsPoint функция не реализована");

        for (int i = 0; i < 8; i++)
        {
            float distance = float.NegativeInfinity;
            rectToCheck.bounds.IntersectRay(new Ray(corners[i], direction), out distance);
        }

        

        return Vector3.zero;
    }

    public Vector3[] Get_8_Corners()
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

    public int CalculateSurfaceArea()
    {
        int width = this.B.x - this.A.x;
        int height = this.B.y - this.A.y;
        int depth = this.B.z - this.A.z;

        int surfaceArea = 2 * (width * height + width * depth + height * depth);
        return surfaceArea;
    }

    public int Voluminosity() // Возвращает количество измерений не равных 0
    {
        return this.size.x == 0 ? 0 : 1 + this.size.y == 0 ? 0 : 1 + this.size.z == 0 ? 0 : 1;
    }
    public bool IsDot()
    {
        return this.Voluminosity() == 0;
    }
    public bool IsLine()
    {
        return this.Voluminosity() == 1;
    }
    public bool IsSquare()
    {
        return this.Voluminosity() == 2;
    }
    public bool IsBox()
    {
        return this.Voluminosity() == 3;
    }

    // Пересечения:
    // С лучами осуществляется в структуре Bounds
    // С другими коробками тоже в bounds
    // Проверка наличия точки также работает через bounds




}
