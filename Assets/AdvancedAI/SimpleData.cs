using UnityEngine;
public struct Pair
{
    public int A; public int B; public bool doesExit;
    public Pair(int A, int B, bool doesExit)
    {
        this.A = A; this.B = B; this.doesExit = doesExit;
    }

}



public struct Edge2D
{
    public Vector2 A;
    public Vector2 B;

    public Vector2 sideCenter() {
        return A + (B - A) / 2;
    }
    public bool DoesIntersectLine(Edge2D other) {
        return Poly2DToolbox.AreCrossing(this.A, this.B, other.A, other.B, out Vector2 dumdum);
    }
    public bool DoesIntersectLine(Edge2D other, out Vector2 dumdum) {
        return Poly2DToolbox.AreCrossing(this.A, this.B, other.A, other.B, out dumdum);
    }
    public bool IsRight(Vector2 point) {
        return Poly2DToolbox.isRight(point, A, B);
    }
    public bool IsLeft(Vector2 point) {
        return Poly2DToolbox.isLeft(point, A, B);
    }

}