using UnityEngine;

public class TestBBoxLine : MonoBehaviour
{
    public Vector2 BBoxA;
    public Vector2 BBoxB;

    public Vector2 A;
    public Vector2 B;

    private void Update()
    {
        Bounds bounds = new Bounds();
        bounds.SetMinMax(BBoxA, BBoxB);

        bool myBool = BoundsMathHelper.DoesLineIntersectBoundingBox2D(A, B, bounds);

        Color myColor = myBool ? Color.green : Color.red;
        DebugUtilities.DebugDrawLine(BBoxA, new Vector2(BBoxA.x, BBoxB.y), myColor);
        DebugUtilities.DebugDrawLine(new Vector2(BBoxA.x, BBoxB.y), BBoxB, myColor);
        DebugUtilities.DebugDrawLine(BBoxB, new Vector2(BBoxB.x, BBoxA.y), myColor);
        DebugUtilities.DebugDrawLine(new Vector2(BBoxB.x, BBoxA.y), BBoxA, myColor);

        DebugUtilities.DebugDrawLine(A, B, Color.blue);
    }

}
