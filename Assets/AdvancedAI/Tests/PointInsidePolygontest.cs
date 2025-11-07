using UnityEngine;
using System.Collections.Generic;
public class PointInsidePolygontest : MonoBehaviour
{
    public List<Vector2> points;
    public Vector2 point;

    public bool isConvex;
    public bool isHole;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Poly2D poly = new Poly2D(points);

        poly.Orient(isHole);


        poly.DebugDrawSelf(isHole ? Color.red : Color.green);


        bool isInside = poly.IsInsidePolygon(point);
        DebugUtilities.DebugDrawCross(point, isInside ? Color.green : Color.red);

    }
}
