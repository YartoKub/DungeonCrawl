using System.Collections.Generic;
using UnityEngine;

public class NavPoly3D : MonoBehaviour
{
    public List<Vector3> myPoints;
    public List<Vector3> myPoints2;
    public Vector3 planePos;
    public Vector3 planeNormal;

    public List<Vector3> flatPoints = new List<Vector3>();
    public List<Vector3> allignedPoints = new List<Vector3>();
    public List<Vector3> restoredPoints = new List<Vector3>();

    
    public List<Vector3> flatPoints2 = new List<Vector3>();
    public List<Vector3> allignedPoints2 = new List<Vector3>();
    // Этот код тестирует функцию ориентации плоскости с точками относительно иси XY,
    // что эквивалентно лишению группы точек их Z координаты без нарушения их относительной позиции друг от друга
    // Желтый - изначальное положение точки
    // Оранжевый - ее проекция на плоскость
    // Желтый - вращение точки до оси XY, Z координата у всех точек одинакова, а потмоу ее можно игнорировать в вычислениях
    // Фиолетовый - возврат точек в исходное положение. Небольшой отступ добавлен искусственно потому что иначе желтый и фиолетовый пересекаются.
    // По итогу все работае ткак надо.ж 
    private void Start()
    {

        
    }

    private void Update()
    {

        LineINtersectionTest();


    }

    private void LineINtersectionTest()
    {
        Plane plane = new Plane();
        plane.SetNormalAndPosition(planeNormal, planePos);
        flatPoints.Clear();
        allignedPoints.Clear();
        restoredPoints.Clear();
        flatPoints2.Clear();
        allignedPoints2.Clear();

        // ==== FLATTEN ====
        for (int i = 0; i < myPoints.Count; i++) flatPoints.Add(Geo3D.ProjectPointOnPlane(myPoints[i], plane));
        for (int i = 0; i < myPoints2.Count; i++) flatPoints2.Add(Geo3D.ProjectPointOnPlane(myPoints2[i], plane));
        // ==== Z REMOVAL ====
        Matrix3x3 rotationMatrix = Geo3D.RotatePlane(plane.normal, new Vector3(0, 0, -1));

        for (int i = 0; i < myPoints.Count; i++)  {
            allignedPoints.Add(rotationMatrix.multiply(flatPoints[i]));
            allignedPoints[i] = new Vector3(allignedPoints[i].x, allignedPoints[i].y, 0);
        }
        for (int i = 0; i < myPoints2.Count; i++)
        {
            allignedPoints2.Add(rotationMatrix.multiply(flatPoints2[i]));
            allignedPoints2[i] = new Vector3(allignedPoints2[i].x, allignedPoints2[i].y, 0);
        }
        for (int i = 0; i < myPoints.Count; i++) DebugUtilities.DebugUltraHedgehog(allignedPoints[i], Color.red);
        for (int i = 0; i < myPoints2.Count; i++) DebugUtilities.DebugUltraHedgehog(allignedPoints2[i], Color.purple);
        // ==== COUNTER CLOCKWISE ====
        allignedPoints = Geo3D.ArrangeCounterClockwise(allignedPoints);
        for (int i = 0; i < allignedPoints.Count; i++)
        {
            DebugUtilities.DebugDrawLine(allignedPoints[i], allignedPoints[(i + 1) % allignedPoints.Count], Color.red);
        }
        allignedPoints2 = Geo3D.ArrangeCounterClockwise(allignedPoints2);
        for (int i = 0; i < allignedPoints2.Count; i++)
        {
            DebugUtilities.DebugDrawLine(allignedPoints2[i], allignedPoints2[(i + 1) % allignedPoints2.Count], Color.purple);
        }





    }

    private void TEST()
    {
        Plane plane = new Plane();
        plane.SetNormalAndPosition(planeNormal, planePos);
        flatPoints.Clear();
        allignedPoints.Clear();
        restoredPoints.Clear();
        // ==== FLATTEN ====
        for (int i = 0; i < myPoints.Count; i++)
        {
            DebugUtilities.DebugUltraHedgehog(myPoints[i], Color.yellow);
            flatPoints.Add(Geo3D.ProjectPointOnPlane(myPoints[i], plane));
        }
        for (int i = 0; i < myPoints.Count; i++)
        {
            DebugUtilities.DebugUltraHedgehog(flatPoints[i], Color.orange);
            DebugUtilities.DebugUltraLine(myPoints[i], flatPoints[i], Color.orange);
        }
        // ==== Z REMOVAL ====
        Matrix3x3 rotationMatrix = Geo3D.RotatePlane(plane.normal, new Vector3(0, 0, -1));

        for (int i = 0; i < myPoints.Count; i++)
        {
            allignedPoints.Add(rotationMatrix.multiply(flatPoints[i]));
        }

        for (int i = 0; i < myPoints.Count; i++)
        {
            DebugUtilities.DebugUltraHedgehog(allignedPoints[i], Color.red);
            DebugUtilities.DebugUltraLine(flatPoints[i], allignedPoints[i], Color.red);
        }
        allignedPoints = Geo3D.ArrangeCounterClockwise(allignedPoints);

        for (int i = 0; i < allignedPoints.Count; i++)
        {
            DebugUtilities.DebugDrawLine(allignedPoints[i], allignedPoints[(i + 1) % allignedPoints.Count], Color.green);
        }

        // ==== RESTORE ORIGINAL ====
        Matrix3x3 restorationMatrix = Geo3D.RotatePlane(new Vector3(0, 0, -1), plane.normal);
        for (int i = 0; i < myPoints.Count; i++)
        {
            restoredPoints.Add(restorationMatrix.multiply(allignedPoints[i]));
        }

        for (int i = 0; i < myPoints.Count; i++)
        {
            DebugUtilities.DebugUltraHedgehog(restoredPoints[i] + new Vector3(0, 0, 0.2f), Color.purple);
            DebugUtilities.DebugUltraLine(allignedPoints[i] + new Vector3(0, 0, 0.2f), restoredPoints[i] + new Vector3(0, 0, 0.2f), Color.purple);
        }
    }


}
