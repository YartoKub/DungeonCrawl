using UnityEngine;
public struct Pair
{
    public int A; public int B; public bool doesExit;
    public Pair(int A, int B, bool doesExit)
    {
        this.A = A; this.B = B; this.doesExit = doesExit;
    }
    public override string ToString()
    {
        return "(" + A.ToString() + " " + B.ToString() + " " + doesExit + ")";
    }
    public static bool PairEquivalence(Pair A, Pair B)
    {   // Просто проверка в случае если B отзеркаленная A.
        if (A.A == B.A & A.B == B.B) return true;
        if (A.B == B.A & A.A == B.B) return true;
        return false;   
    }
}
public struct PairPair {
    public int A, B, a, b;
    public PairPair(int A, int B, int a, int b)
    {
        this.A = A; this.B = B; this.a = a; this.b = b;
    }
    public override string ToString()
    {
        return "(A " + A.ToString() + " B " + B.ToString() + " a " + a.ToString() + " b " + b.ToString() + ")";
    }
}

public struct Level2IntersectionRatio
{
    public int A, B, a, b;
    public float Aratio; public float Bratio;
    public Level2IntersectionRatio(int A, int B, int a, int b, float Ar, float Br)
    {
        this.A = A; this.B = B; this.a = a; this.b = b; this.Aratio = Ar; this.Bratio = Br;
    }
    public override string ToString()
    {
        return "(A " + A.ToString() + " B " + B.ToString() + " a " + a.ToString() + " b " + b.ToString() + " a_ratio " + Aratio.ToString() + " b_ratio " + Bratio.ToString() + ")";
    }
}


public struct Edge2D
{
    public Vector2 A;
    public Vector2 B;
    public Edge2D(Vector2 A, Vector2 B)
    {
        this.A = A;
        this.B = B;
    }

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