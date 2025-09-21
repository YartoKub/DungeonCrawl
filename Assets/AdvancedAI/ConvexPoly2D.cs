using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Всегда содержит в себе кучу выпуклых полигонов. По дефолту это просто отдельные треугольники, они всегда выпуклы
// Внутри класса есть инструменты для оптимизации полигонов, по их объединению в более крупные структуры
public class ConvexPoly2D
{
    List<Poly2D> polygons; // Содержит в себе полигоны, поверхность, дыры, стены. Все полигоны равны в иерархии
    IntMatrixGraph myMatrix; // хранит связи между полигонами

    Bounds BBox;

    public ConvexPoly2D()
    {

    }

    private struct TMPConvexPoly
    {
        public List<int> vertices;
        public List<int> absorbedTriangles;
        //public List<int> neighbours;
        public TMPConvexPoly(bool dummy)
        {
            vertices = new List<int>();
            absorbedTriangles = new List<int>();
            //neighbours = new List<int>();
        }
    }

    public ConvexPoly2D(List<Vector2> vectorList, List<Vector3Int> triangleList, List<Vector3Int> connections, Vector3Int debug)
    {
        List<TMPConvexPoly> polys = VolumeGrowth(vectorList, triangleList, connections, debug);

        myMatrix = new IntMatrixGraph(polys.Count);
        int[] triangleIDList = new int[triangleList.Count];
        for (int i = 0; i < polys.Count; i++) {
            for (int j = 0; j < polys[i].absorbedTriangles.Count; j++)
            {
                triangleIDList[polys[i].absorbedTriangles[j]] = i;
            }
        }

        for (int i = 0; i < polys.Count; i++)
        {
            for (int j = 0; j < polys[i].absorbedTriangles.Count; j++)
            {
                int Tid = polys[i].absorbedTriangles[j];
                Vector3Int CCon = connections[Tid];
                //Debug.Log(i + " " + CCon + " " + j);
                if (CCon.x != -1 & CCon.x < triangleIDList.Length) myMatrix.SetValue(true, i, triangleIDList[CCon.x]);
                if (CCon.y != -1 & CCon.y < triangleIDList.Length) myMatrix.SetValue(true, i, triangleIDList[CCon.y]);
                if (CCon.z != -1 & CCon.z < triangleIDList.Length) myMatrix.SetValue(true, i, triangleIDList[CCon.z]);
            }
        }

        this.polygons = new List<Poly2D>();
        for (int i = 0; i < polys.Count; i++)
        {
            List<Vector2> loc_vertices = new List<Vector2>(polys[i].vertices.Count);
            for (int j = 0; j < polys[i].vertices.Count; j++)
            {
                loc_vertices.Add(vectorList[polys[i].vertices[j]]);
            }
            Poly2D newPoly = new Poly2D(loc_vertices);
            newPoly.CalculateBBox();
            newPoly.convex = true;
            polygons.Add(newPoly);
        }
    }

    public void DebugDrawSelf()
    {
        myMatrix.DumpSelf();

        foreach (Poly2D item in this.polygons)
        {
            item.DebugDrawSelf(Color.purple);
        }

        for (int i = 0; i < myMatrix.vCount; i++)
        {
            for (int j = i + 1; j < myMatrix.vCount; j++)
            {
                if (myMatrix.GetValue(i, j))
                {
                    Vector2 A = this.polygons[i].AveragePoint();
                    Vector2 B = this.polygons[j].AveragePoint();
                    DebugUtilities.DebugDrawLine(A, B, Color.yellow);
                    //DebugUtilities.DebugDrawLine(B, A, Color.yellow);
                }
            }
        }
    }

    public static IntMatrixGraph TmpPolyToConnections()
    {
        return null;
    }

    private static List<TMPConvexPoly> VolumeGrowth(List<Vector2> vectorList, List<Vector3Int> triangleList, List<Vector3Int> connections, Vector3Int debug) // Суть в том что полигоны растут
    {   // Желательно исползовать полигоны, прошедшие через триангуляцию Дюлонея/Вороного, т.к. эта триангуляция уменьшает максимальную грань,
        // Это приводит к тому что полигоны получаются более округлыми и менее вытянутыми
        int safety = 0;
        bool[] isOccupied = new bool[triangleList.Count];       // False by default
        List< TMPConvexPoly > polys = new List< TMPConvexPoly >();
        while (safety < 25) { safety += 1;
            int currT = -1;
            for (int i = 0; i < triangleList.Count; i++) { if (!isOccupied[i]) { currT = i; break; } }
            if (currT == -1) break;

            //List<Pair> edges = new List<Pair>(); // Edges, A - start, B - end, bool - является ли грань частью оригинальной границы
            polys.Add(GrowBigPolygon(currT, isOccupied, vectorList, triangleList, connections, debug));
        }
        //Debug.Log("   BROKEN OUT!   ");
        return polys;
    }
    private static TMPConvexPoly GrowBigPolygon(int seedT, bool[] isOccupied, List<Vector2> vectorList, List<Vector3Int> triangleList, List<Vector3Int> connections, Vector3Int debug)
    {
        //Debug.Log("BEGAN BUILDING POLY");
        int safety = 0;
        TMPConvexPoly bigPoly = new TMPConvexPoly(true);
        //bigPoly.absorbedTriangles.Add(seedT);
        List<int> order = new List<int>();
        List<float> area = new List<float>();
        bigPoly.vertices.Add(triangleList[seedT].x);
        bigPoly.vertices.Add(triangleList[seedT].y);
        bigPoly.vertices.Add(triangleList[seedT].z);
        bigPoly.absorbedTriangles.Add(seedT);
        int currT = seedT;
        while (safety < 25) { safety += 1;
            // Identify neighbours and add them to queue
            isOccupied[currT] = true;
            //Debug.Log((connections[currT].x != -1) + " " + CanConsume(currT, connections[currT].x, vectorList, triangleList));
            if (connections[currT].x != -1 && CanConsume(currT, connections[currT].x, bigPoly, vectorList, triangleList) && !isOccupied[connections[currT].x])
            {
                Vector3Int XT = triangleList[connections[currT].x];
                order.Add(connections[currT].x);
                area.Add(Poly2DToolbox.AreaTriangle(vectorList[XT.x], vectorList[XT.y], vectorList[XT.z]));
            }
            //Debug.Log((connections[currT].y != -1) + " " + CanConsume(currT, connections[currT].y, vectorList, triangleList));
            if (connections[currT].y != -1 && CanConsume(currT, connections[currT].y, bigPoly, vectorList, triangleList) && !isOccupied[connections[currT].y])
            {
                Vector3Int YT = triangleList[connections[currT].y];
                order.Add(connections[currT].y);
                area.Add(Poly2DToolbox.AreaTriangle(vectorList[YT.x], vectorList[YT.y], vectorList[YT.z]));
            }
            //Debug.Log((connections[currT].z != -1) + " " + CanConsume(currT, connections[currT].z, vectorList, triangleList));
            if (connections[currT].z != -1 && CanConsume(currT, connections[currT].z, bigPoly, vectorList, triangleList) && !isOccupied[connections[currT].z])
            {
                Vector3Int ZT = triangleList[connections[currT].z];
                order.Add(connections[currT].z);
                area.Add(Poly2DToolbox.AreaTriangle(vectorList[ZT.x], vectorList[ZT.y], vectorList[ZT.z]));
            }
            //Debug.Log(order.Count == 0 ? "NO MORE TO ADD" : "Have " + order.Count + " triangles in quee");
            if (order.Count == 0) break;

            // SELECT biggest triangle and SELECT it's Neighbour inside of BigPoly
            int biggest = 0;    // Picks largest area of all areas as next block
            for (int i = 0; i < order.Count; i++) biggest = area[biggest] <= area[i] ? i : biggest;

            int absorbedT = -1; // neighbour that shares an edge with biggest
            for (int i = 0; i < bigPoly.absorbedTriangles.Count; i++)
            {
                //Debug.Log(triangleList[bigPoly.absorbedTriangles[i]] + " " + triangleList[order[biggest]]);
                //Debug.Log(DoIntTrianglesTouch(triangleList[bigPoly.absorbedTriangles[i]], triangleList[order[biggest]]));
                if (DoIntTrianglesTouch(triangleList[bigPoly.absorbedTriangles[i]], triangleList[order[biggest]]))
                {
                    absorbedT = i;
                    break;
                }
            }
            if (absorbedT == -1) break;
            int thisTid = bigPoly.absorbedTriangles[absorbedT];
            int otherTid = order[biggest];
            // Get vertices involved in operation
            //Debug.Log(otherTid + " " + thisTid + " " + triangleList[otherTid] + " " + triangleList[thisTid]);
            Vector3Int dirrerent = GetDifferringVerticeAndOverlap(triangleList[otherTid], triangleList[thisTid]);  // T that belongs to BigPoly
            int C = GetDifferringVertice(triangleList[thisTid], triangleList[otherTid]);                        // New point to add
            int Bc = dirrerent.y;
            int Bn = bigPoly.vertices[GetPrevious(bigPoly.vertices, Bc)];
            int Dc = dirrerent.z;
            int Dn = bigPoly.vertices[GetNext(bigPoly.vertices, Dc)];
            //Debug.Log(dirrerent + " C " + C + " Bc " +Bc + " Bn " + Bn + " Dc " + Dc + " Dn " + Dn);
            //Debug.Log(vectorList[Bc] + " " + vectorList[Bn] + " " + vectorList[Dc] + " " + vectorList[Dn]);

            // If angles are not reflex, absorbs triangle
            float Bangle = Poly2DToolbox.SignedAngle(vectorList[Bn], vectorList[Bc], vectorList[C]);
            float Dangle = Poly2DToolbox.SignedAngle(vectorList[C], vectorList[Dc], vectorList[Dn]);
            //Debug.Log(Bangle + " " + Dangle);
            /*
            if (debug.x != -1 && debug.x == order[biggest])
            {
                DebugUtilities.DebugDrawLine(vectorList[Bn], vectorList[Bc], Color.red);
                DebugUtilities.DebugDrawLine(vectorList[Bc], vectorList[C], Color.red);
                DebugUtilities.DebugDrawLine(vectorList[C], vectorList[Dc], Color.blue);
                DebugUtilities.DebugDrawLine(vectorList[Dc], vectorList[Dn], Color.blue);
            }*/


            if (Bangle < 180 && Dangle < 180)
            {
                //Debug.Log("Triangle injection " + order[biggest]);
                InjectVertice(bigPoly.vertices, C, Dc);
                bigPoly.absorbedTriangles.Add(order[biggest]);
                currT = order[biggest]; // sets newly added triangle as next triangle
            }
            order.RemoveAt(biggest);    // removes next block from neighbours
            area .RemoveAt(biggest);    // as well as it's area
        }
        //Debug.Log(bigPoly.absorbedTriangles.Count);
        //string absT1 = ""; for (int i = 0; i < bigPoly.absorbedTriangles.Count; i++) absT1 += bigPoly.absorbedTriangles[i] + " "; Debug.Log(absT1);

        //Debug.Log("FINISHED BUIL:DING POLY");
        return bigPoly;
    }

    private static bool CanConsume(int thisTid, int otherTid, TMPConvexPoly bigPoly, List<Vector2> vectorList, List<Vector3Int> triangleList)
    {
        // Get vertices involved in operation
        //Debug.Log(otherTid + " " + thisTid + " " + triangleList[otherTid] + " " + triangleList[thisTid]);
        Vector3Int dirrerent = GetDifferringVerticeAndOverlap(triangleList[otherTid], triangleList[thisTid]);  // T that belongs to BigPoly
        int C = GetDifferringVertice(triangleList[thisTid], triangleList[otherTid]);                        // New point to add
        int Bc = dirrerent.y;
        int Bn = bigPoly.vertices[GetPrevious(bigPoly.vertices, Bc)];
        int Dc = dirrerent.z;
        int Dn = bigPoly.vertices[GetNext(bigPoly.vertices, Dc)];

        float Bangle = Poly2DToolbox.SignedAngle(vectorList[Bn], vectorList[Bc], vectorList[C]);
        float Dangle = Poly2DToolbox.SignedAngle(vectorList[C], vectorList[Dc], vectorList[Dn]);

        return Bangle < 180 && Dangle < 180;
    }


    // Injects a new vertex after previous vertex
    public static void InjectVertice(List<int> bigPolyVerts, int newVert, int prevVert)
    {
        if (bigPolyVerts.Count < 2) return;
        for (int i = 0; i < bigPolyVerts.Count; i++)
        {
            if (bigPolyVerts[i] != prevVert) continue;
            //Debug.Log(i + " " + bigPolyVerts[i] + " " + prevVert + " " + newVert);
            bigPolyVerts.Insert(i, newVert);
            return;
        }
        return;
    }
    public static int GetPrevious(List<int> bigPolyVerts, int vid)
    {
        for (int i = 0; i < bigPolyVerts.Count; i++)
        {
            if (bigPolyVerts[i] == vid)
            {
                return (i - 1 + bigPolyVerts.Count) % bigPolyVerts.Count;
            }
        }
        return -1;
    }

    public static int GetNext(List<int> bigPolyVerts, int vid)
    {
        for (int i = 0; i < bigPolyVerts.Count; i++)
        {
            if (bigPolyVerts[i] == vid)
            {
                return (i + 1) % bigPolyVerts.Count;
            }
        }
        return -1;
    }


    // Picks next triangle to add to the Polygon
    private static int PickOneToConsume(TMPConvexPoly bigP, List<Vector2> vectorList, List<Vector3Int> triangleList, List<Vector3Int> connections)
    {
        return -1;
    }

    public static bool CanConsume(int T1, int T2, List<Vector2> vectorList, List<Vector3Int> triangleList)
    {
        if (T1 == -1) return false;
        if (T2 == -1) return false;

        Vector3Int A1BC = GetDifferringVerticeAndOverlap(triangleList[T2], triangleList[T1]);
        int A1 = A1BC.x;
        int B = A1BC.y;
        int C = A1BC.z;
        int A2 = GetDifferringVertice(triangleList[T1], triangleList[T2]);
        /*
        DebugUtilities.DebugDrawCross(avgPoint(vectorList[triangleList[T1].x], vectorList[triangleList[T1].y], vectorList[triangleList[T1].z]), Color.red);
        DebugUtilities.DebugDrawLine(vectorList[A1], vectorList[B], Color.yellow);
        DebugUtilities.DebugDrawLine(vectorList[B], vectorList[A2], Color.yellow);
        DebugUtilities.DebugDrawCross(vectorList[B], Color.orange);
        DebugUtilities.DebugDrawLine(vectorList[A2], vectorList[C], Color.cyan);
        DebugUtilities.DebugDrawLine(vectorList[C], vectorList[A1], Color.cyan);
        DebugUtilities.DebugDrawCross(vectorList[C], Color.purple);*/
        //Debug.Log(angleB + " " + angleC);
        float angleB = Poly2DToolbox.SignedAngle(vectorList[A1], vectorList[B], vectorList[A2]);
        float angleC = Poly2DToolbox.SignedAngle(vectorList[A2], vectorList[C], vectorList[A1]);
        return (angleB <= 180 && angleC <= 180);
    }

    public static float GetAreaAndConsumability(int T1, int T2, List<Vector2> vectorList, List<Vector3Int> triangleList)
    {
        if (CanConsume(T1, T2, vectorList, triangleList)) 
            return Poly2DToolbox.AreaTriangle(
            vectorList[triangleList[T2].x], 
            vectorList[triangleList[T2].y], 
            vectorList[triangleList[T2].z]);
        return -1.0f;
    }

    public static void IterativeVoronoi(List<Vector2> vectorList, List<Vector3Int> triangleList, List<Vector3Int> connections)
    {   // Voronoi triangulation, uses a triangulation that is already finished 
        bool[] isValid = new bool[triangleList.Count]; // This list contains whether a triangle is voronoi complete
        bool reasonToLive = false;

        for (int i = 0; i < triangleList.Count; i++)
        {
            int voronoiIssue = VoronoiCheck(i, connections[i], vectorList, triangleList);
            if (voronoiIssue == -1) continue;
            isValid[i] = true;
            reasonToLive = true;
        }
        //Debug.Log(reasonToLive ? "I have reason to live!" : "NO reason to live...");
        //string strrr = ""; for (int i = 0; i < triangleList.Count; i++) { strrr += isValid[i] + " "; }; Debug.Log(strrr);

        int safety = 0;
        while (reasonToLive & safety < 50) { safety += 1;
            int currT = FirstTrue(isValid);     // current triangle
            if (currT == -1)
            {
                reasonToLive = false;
                break;
            }

            int voronoiIssue = VoronoiCheck(currT, connections[currT], vectorList, triangleList);
            if (voronoiIssue == -1) { isValid[currT] = false; continue; }    // If no voronoi issue, then this triangle is correct

            FlipTriangle(currT, voronoiIssue, vectorList, triangleList, connections);
            isValid[currT] = true;             // Both triangles have been flipped, it is unknown whether they follow Voronoi rule or not
            isValid[voronoiIssue] = true;
        } //Debug.Log("SAFEYTY " +  safety);
    }

    public static void FlipTriangle(int T1, int T2, List<Vector2> verts, List<Vector3Int> triangleList, List<Vector3Int> connections)
    {   // By default all little triangles are counter-clockwise, resulting triangles should too be CCW
        int A2 = GetDifferringVertice(triangleList[T1], triangleList[T2]);
        int A1 = GetDifferringVertice(triangleList[T2], triangleList[T1]);
        /* (A2, C, B) (A1, B, C) */ int B = triangleList[T2].y; int C = triangleList[T2].x;     // Triangles overlap at C and B
        if (triangleList[T2].x == A2) { B = triangleList[T2].z;     C = triangleList[T2].y; }
        if (triangleList[T2].y == A2) { B = triangleList[T2].x;     C = triangleList[T2].z; }
        //Debug.Log("( " + A2 + " " + C + " " + B + " ) ( " + A1 + " " + B + " " + C + " )");

        // Connections
        // Both triangles will remain connected to each other
        // Old triangles" (A1, B,  C) (A2, C,  B), they overlap at CB
        // New triangles: (A1, A2, C) (A2, A1, B), they overlap at A1 A2
        // A1's CB n

        Vector3Int nT1 = new Vector3Int(A1, A2, C);
        Vector3Int nT2 = new Vector3Int(A2, A1, B);
        triangleList[T1] = nT1;
        triangleList[T2] = nT2;

        // purges connections to T1 and T2
        List<int> FourNeighboursList = new List<int>(4);    // Connected triangles that are not T1 or T2
        if (connections[T1].x != T2) FourNeighboursList.Add(connections[T1].x);
        if (connections[T1].y != T2) FourNeighboursList.Add(connections[T1].y);
        if (connections[T1].z != T2) FourNeighboursList.Add(connections[T1].z);

        if (connections[T2].x != T1) FourNeighboursList.Add(connections[T2].x);
        if (connections[T2].y != T1) FourNeighboursList.Add(connections[T2].y);
        if (connections[T2].z != T1) FourNeighboursList.Add(connections[T2].z);

        //Debug.Log(FourNeighboursList.Count);
        for (int i = 0; i < FourNeighboursList.Count; i++)
        {
            if (FourNeighboursList[i] == -1) continue;
            Vector3Int con = connections[FourNeighboursList[i]];
            Vector3Int newCon = new Vector3Int(
                ((con.x == T1) | (con.x == T2)) ? -1 : con.x,
                ((con.y == T1) | (con.y == T2)) ? -1 : con.y,
                ((con.z == T1) | (con.z == T2)) ? -1 : con.z);
            connections[FourNeighboursList[i]] = newCon;
            //Debug.Log(con + " -> " + newCon);
        }

        // Reestablishes connections to nT1 and nT2
        connections[T1] = -Vector3Int.one; connections[T2] = -Vector3Int.one;
        for (int i = 0; i < FourNeighboursList.Count; i++)
        {
            int locN = FourNeighboursList[i];
            if (FourNeighboursList[i] == -1) continue;  // Rechecks whether there is a connection for an updates T1 and T2
            if (DoIntTrianglesTouch(triangleList[T1], triangleList[locN])) SetNeighbours(T1, locN, triangleList[T1], triangleList[locN], connections);
            if (DoIntTrianglesTouch(triangleList[T2], triangleList[locN])) SetNeighbours(T2, locN, triangleList[T2], triangleList[locN], connections);
        }
        SetNeighbours(T1, T2, triangleList[T1], triangleList[T2], connections);     // And finally sets T1 and T2 as neighbours
    }

    private static int FirstTrue(bool[] list)
    {
        for (int i = 0; i < list.Length; i++)
            if (list[i]) return i;
        return -1;
    }

    public static int VoronoiCheck(int T1, Vector3Int connection, List<Vector2> vectorList, List<Vector3Int> triangleList)
    {
        int[] ids = new int[] {connection.x, connection.y, connection.z};

        // Triangle 1
        Vector3Int triangle1 = triangleList[T1];
        Vector2 circle = Geo3D.CircumCenter(vectorList[triangle1[0]], vectorList[triangle1[1]], vectorList[triangle1[2]]);
        float radius = Vector2.Distance(circle, vectorList[triangle1[0]]);

        for (int i = 0; i < ids.Length; i++)        // Checks each connection
        {
            if (ids[i] == -1) continue;
            Vector3Int triangle2 = triangleList[ids[i]];
            int vid = GetDifferringVertice(triangle1, triangle2);
            if (Vector2.Distance(circle, vectorList[vid]) > radius) continue;
            return ids[i];
        }
        return -1;
    }

    private static int GetDifferringVertice(Vector3Int A, Vector3Int B)
    {
        if (B.x != A.x && B.x != A.y && B.x != A.z) return B.x;
        if (B.y != A.x && B.y != A.y && B.y != A.z) return B.y;
        if (B.z != A.x && B.z != A.y && B.z != A.z) return B.z;
        return -1;
    }

    private static Vector3Int GetDifferringVerticeAndOverlap(Vector3Int A, Vector3Int B)
    {   // First value is a difference, second two values are overlap
        if (B.x != A.x && B.x != A.y && B.x != A.z) return new Vector3Int(B.x, B.y, B.z); 
        if (B.y != A.x && B.y != A.y && B.y != A.z) return new Vector3Int(B.y, B.z, B.x);
        if (B.z != A.x && B.z != A.y && B.z != A.z) return new Vector3Int(B.z, B.x, B.y);
        return -Vector3Int.one;
    }

    public static void CompleteVoronoiTriangulation(Poly2D mainPoly, List<Poly2D> holes)
    {
        List<Vector2> stitched = Poly2DToolbox.UniteHoles(mainPoly, holes);
        List<Vector3Int> triangles = Poly2DToolbox.EarClip(stitched);
        HealScars(stitched, triangles);
        List<Vector3Int> TConnections = EstablishConnections(stitched, triangles);
        IterativeVoronoi(stitched, triangles, TConnections);

    }

    // Каждая ранка от объединения полигона с его дыркой оставляет дегенеративную грань, состоящую из двух наложеных друг на друга граней.
    // Эти грании должны быть объединены в одну, а треугольники корректно переформированы
    public static void HealScars(List<Vector2> vectorList, List<Vector3Int> triangleList)
    {
        List<Vector2Int> all_overlaps = DetectOverlaps(vectorList);
        //for (int i = 0; i < all_overlaps.Count; i++) Debug.Log(all_overlaps[i] + " " + vectorList[all_overlaps[i][0]] + " " + vectorList[all_overlaps[i][1]] );

        for (int i = all_overlaps.Count - 1; i >= 0; i--)
        {
            Vector2Int localOverlap = all_overlaps[i]; //Debug.Log("===" +  localOverlap + " " + vectorList[localOverlap[0]] + " " + vectorList[localOverlap[1]]);
            all_overlaps.RemoveAt(i);
            HealScar(vectorList, triangleList, localOverlap, all_overlaps);
        }
    }

    public static void HealScar(List<Vector2> vectorList, List<Vector3Int> triangleList, Vector2Int overlap, List<Vector2Int> overlaps)
    {
        // What it does it removes a vertice from list.
        // Triangle is a list of vertice ids, so if it had an id equal to vertice, then it replaces it with an analogue
        // If triangle has vertices that go after vertice, then their ids are id - 1
        // overlaps are also modified
        int a = overlap[0];
        int b = overlap[1]; // upper bound, every value equal or above is lovered
        vectorList.RemoveAt(b);

        for (int i = 0; i < triangleList.Count; i++) // Degenerate edge can only have a single triangle neighbour
        {
            if (triangleList[i].x == b) { triangleList[i] = new Vector3Int(a, triangleList[i].y, triangleList[i].z); /*Debug.Log("X downscale");*/ continue; }
            if (triangleList[i].y == b) { triangleList[i] = new Vector3Int(triangleList[i].x, a, triangleList[i].z); /*Debug.Log("Y downscale");*/ continue; }
            if (triangleList[i].z == b) { triangleList[i] = new Vector3Int(triangleList[i].x, triangleList[i].y, a); /*Debug.Log("Z downscale");*/ continue; }
            continue;
        }

        for (int i = 0; i < triangleList.Count; i++)
        {
            Vector3Int originT = triangleList[i];
            Vector3Int triangle = new Vector3Int(
                originT.x <= b ? originT.x : originT.x - 1,
                originT.y <= b ? originT.y : originT.y - 1,
                originT.z <= b ? originT.z : originT.z - 1);
            triangleList[i] = triangle; //Debug.Log(originT + " -> " + triangle);
        }

        for (int i = 0; i < overlaps.Count; i++)
        {
            Vector2Int pair = overlaps[i];
            Vector2Int newPair = new Vector2Int(
                pair.x <= b ? pair.x : pair.x - 1,
                pair.y <= b ? pair.y : pair.y - 1);
            overlaps[i] = newPair; //Debug.Log(pair + " -> " + newPair);
        }

    }

    public static List<Vector2Int> DetectOverlaps(List<Vector2> vertices)
    {
        List<Vector2Int> overlaps = new List<Vector2Int>();
        for (int a = 0; a < vertices.Count; a++)
            for (int b = a + 1; b < vertices.Count; b++)
                if (Poly2DToolbox. PointSimilarity(vertices[a], vertices[b]))
                    overlaps.Add(new Vector2Int(a, b));

        return overlaps;
    }

    public static List<Vector3Int> EstablishConnections(List<Vector2> vertices, List<Vector3Int> triangles) 
    {
        List<Vector3Int> TConnections = new List<Vector3Int>(triangles.Count); // Каждый треугольник может соседствовать только с тремя треугольниками
        for (int i = 0; i < triangles.Count; i++) TConnections.Add(new Vector3Int(-1, -1, -1));

        for (int a = 0; a < triangles.Count; a++) {
            for (int b = a + 1; b < triangles.Count; b++) {
                if (!DoIntTrianglesTouch(triangles[a], triangles[b])) continue;
                SetNeighbours(a, b, triangles[a], triangles[b], TConnections);
            }
        }
        //string connmattrix = "=== Connection matrix ==="; for (int i = 0; i < triangles.Count; i++) connmattrix += triangles[i] + " " + TConnections[i] + "\n"; Debug.Log(connmattrix);
        return TConnections;
    }   

    public static void DrawPolygonConnections(List<Vector3Int> TConnections, List<Vector3Int> triangles, List<Vector2> vertices)
    {
        for (int i = 0; i < TConnections.Count; i++)
        {
            if (TConnections[i].x != -1)
            {
                Vector3Int t1 = triangles[i];
                Vector3Int t2 = triangles[TConnections[i].x];
                DebugUtilities.DebugDrawLine(avgPoint(vertices[t1.x], vertices[t1.y], vertices[t1.z]), avgPoint(vertices[t2.x], vertices[t2.y], vertices[t2.z]), Color.red);
            }
            if (TConnections[i].y != -1)
            {
                Vector3Int t1 = triangles[i];
                Vector3Int t2 = triangles[TConnections[i].y];
                DebugUtilities.DebugDrawLine(avgPoint(vertices[t1.x], vertices[t1.y], vertices[t1.z]), avgPoint(vertices[t2.x], vertices[t2.y], vertices[t2.z]), Color.red);
            }
            if (TConnections[i].z != -1)
            {
                Vector3Int t1 = triangles[i];
                Vector3Int t2 = triangles[TConnections[i].z];
                DebugUtilities.DebugDrawLine(avgPoint(vertices[t1.x], vertices[t1.y], vertices[t1.z]), avgPoint(vertices[t2.x], vertices[t2.y], vertices[t2.z]), Color.red);
            }
        }
    }

    private static Vector2 avgPoint(Vector2 A, Vector2 B, Vector2 C)
    {
        return (A + B + C) / 3;
    }

    public static bool DoesTriangleContainPoint(Vector3Int A, int pointId)
    {
        return (A.x == pointId) | (A.y == pointId) | (A.z == pointId);
    }
    // Returns a point from triangle B, that is not present in A


    private static bool DoIntTrianglesTouch(Vector3Int A, Vector3Int b)
    {
        int counter = 0;
        if (A.x == b.x || A.x == b.y || A.x == b.z) counter += 1;
        if (A.y == b.x || A.y == b.y || A.y == b.z) counter += 1;
        if (A.z == b.x || A.z == b.y || A.z == b.z) counter += 1;
        return counter == 2; // If 2 then there is a shared edge, if 3 then they are equivalent, if 1 they share a vertice
    }


    private static void SetNeighbours(int Aid, int Bid, Vector3Int A, Vector3Int B, List<Vector3Int> TConnections)
    {   // Triangles are must be known to have a valid connection
        SetSingleNeighbour(Aid, Bid, A, B, TConnections);
        SetSingleNeighbour(Bid, Aid, B, A, TConnections);
    }

    private static void SetSingleNeighbour(int Aid, int Bid, Vector3Int A, Vector3Int B, List<Vector3Int> TConnections)
    {   // Checks which coordinate is different in A and B. The side opposite to it is where triangles intersects.
        // If A.x != B -> y,z shared edge
        // If A.y != B -> z,x shared edge
        // If A.z != B -> x,y shared edge
        if (!(A.x == B.x | A.x == B.y | A.x == B.z))    // Checks if A.x is different from all B.xyz
        {
            TConnections[Aid] = new Vector3Int(Bid, TConnections[Aid].y, TConnections[Aid].z);
            return;
        }
        if (!(A.y == B.x | A.y == B.y | A.y == B.z))    // Checks if A.y is different from all B.xyz
        {
            TConnections[Aid] = new Vector3Int(TConnections[Aid].x, Bid, TConnections[Aid].z);
            return;
        }
        if (!(A.z == B.x | A.z == B.y | A.z == B.z))    // Checks if A.z is different from all B.xyz
        {
            TConnections[Aid] = new Vector3Int(TConnections[Aid].x, TConnections[Aid].y, Bid);
            return;
        }
        return;
    }


}
