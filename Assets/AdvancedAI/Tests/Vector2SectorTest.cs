using UnityEngine;

public class Vector2SectorTest : MonoBehaviour 
{
    public Vector2 origin;
    public Vector2 A;
    public Vector2 B;
    public Vector2 C;
    public void Update()
    {
        DebugUtilities.DebugDrawLine(origin, A, Color.yellow);
        DebugUtilities.DebugDrawLine(origin, B, Color.blue);
        bool is_inside = Geo3D.DoesVectorDLieInSectorAB(origin, A, B, C);
        DebugUtilities.DebugDrawLine(origin, C, is_inside ? Color.green : Color.red);
    }
}
