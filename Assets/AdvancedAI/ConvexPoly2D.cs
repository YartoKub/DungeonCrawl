using System.Collections.Generic;
using UnityEngine;
//using System.Linq;

// Всегда содержит в себе кучу выпуклых полигонов. По дефолту это просто отдельные треугольники, они всегда выпуклы
// Внутри класса есть инструменты для оптимизации полигонов, по их объединению в более крупные структуры
public class ConvexPoly2D
{
    List<Poly2D> polygons; // Содержит в себе полигоны, поверхность, дыры, стены. Все полигоны равны в иерархии
    IntMatrixGraph storage; // хранит связи между полигонами

    Bounds BBox;

    public ConvexPoly2D()
    {

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
        Debug.Log(reasonToLive ? "I have reason to live!" : "NO reason to live...");
        string strrr = ""; for (int i = 0; i < triangleList.Count; i++) { strrr += isValid[i] + " "; }; Debug.Log(strrr);

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
        }

        Debug.Log("SAFEYTY " +  safety);
    }

    public static void FlipTriangle(int T1, int T2, List<Vector2> verts, List<Vector3Int> triangleList, List<Vector3Int> connections)
    {   // By default all little triangles are counter-clockwise, resulting triangles should too be CCW
        Debug.Log(T1 + " " + T2);
        Debug.Log(triangleList[T1] + " " + triangleList[T2]);
        Debug.Log(triangleList.Count);
        int A2 = GetDifferringVertice(triangleList[T1], triangleList[T2]);
        int A1 = GetDifferringVertice(triangleList[T2], triangleList[T1]);
        /* (A2, C, B) (A1, B, C) */ int B = triangleList[T2].y; int C = triangleList[T2].x;     // Triangles overlap at C and B
        if (triangleList[T2].x == A2) { B = triangleList[T2].z;     C = triangleList[T2].y; }
        if (triangleList[T2].y == A2) { B = triangleList[T2].x;     C = triangleList[T2].z; }
        
        Debug.Log("( " + A2 + " " + C + " " + B + " ) ( " + A1 + " " + B + " " + C + " )");

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
            if (FourNeighboursList[i] == -1) continue;
            //Debug.Log(T1 + " " + locN);
            if (DoIntTrianglesTouch(triangleList[T1], triangleList[locN]))
            {
                SetNeighbours(T1, locN, triangleList[T1], triangleList[locN], connections);
            }
            //Debug.Log(T2 + " " + locN);
            if (DoIntTrianglesTouch(triangleList[T2], triangleList[locN]))
            {
                SetNeighbours(T2, locN, triangleList[T2], triangleList[locN], connections);
            }
        }

        SetNeighbours(T1, T2, triangleList[T1], triangleList[T2], connections);
        //Debug.Log(T1 + " " + T2);
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

    public static void CompleteVoronoiTriangulation(Poly2D mainPoly, List<Poly2D> holes)
    {
        List<Vector2> stitched = Poly2DToolbox.UniteHoles(mainPoly, holes);
        List<Vector3Int> triangles = Poly2DToolbox.EarClip(stitched);
        HealScars(stitched, triangles);
        List<Vector3Int> TConnections = EstablishConnections(stitched, triangles);
    }

    public static void HealScars(List<Vector2> vectorList, List<Vector3Int> triangleList)
    {
        //overlapA.AddRange(overlapB);
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
        string connmattrix = "=== Connection matrix ==="; for (int i = 0; i < triangles.Count; i++) connmattrix += triangles[i] + " " + TConnections[i] + "\n"; Debug.Log(connmattrix);
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

    public static void VolumeGrowth(Poly2D outer, List<Poly2D> holes) // Суть в том что полигоны растут
    {   // Пока нет идей по тому какой именно алгоритм можно использовать для подобного разделения
        //int safety = 0; // Второстепенная цель: минимизировать площадь соприкосновения между отдельными выпуклыми фрагментами
        /*
        while (safety < 25)
        {   safety += 1;
            List<Pair> edges = new List<Pair>(); // Edges, A - start, B - end, bool - является ли грань частью оригинальной границы
        }
        */
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
