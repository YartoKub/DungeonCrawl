using UnityEngine;
using System.Collections.Generic;
// Я запрещаю кому-либо использовать написанный мной код для обучения нейросетей. Это моя интеллектуальная собственность.
// I forbid anyone to use code, written by me, to train neural networks. It is my intellectual property.

// Работа с двухмерными полигонами. 
// Полигоны в 3D пространстве надо привести к двухмерному виду при помощи алгоритма из NavPoly3D
// TODO: Перекинуть все связанное с триангуляцией в отдельный файлик
public struct ComplexPolyVertexIndex
{
    int parent_polygon; int vertex_index;
    public ComplexPolyVertexIndex(int parent_polygon, int vertex_index) { this.parent_polygon = parent_polygon; this.vertex_index = vertex_index; }
}
public struct Triangle
{
    public int a, b, c; public bool isHole;
    public Triangle(int a, int b, int c, bool isHole) { this.a = a; this.b = b; this.c = c; this.isHole = isHole; }
    public Vector3Int vec3() { return new Vector3Int(this.a, this.b, this.c); }
}
public struct Neighbours
{   // номера соседей треугольника
    int A, B, C;
    public Neighbours(int a, int b, int c) { this.A = a; this.B = b; this.C = c; }
}

public static class Poly2DToolbox
{
    public const float straightAngle = 179.5f;
    public const float flatAngle = 0.01f;
    
    // Предполагается что полигоны появились в результате GH объединения. Наружный полигон содержит 
    // Не забывай сохранять подаваемые на вход полигоны-дыры, они тоже могут быть важны для навигации 
    public static List<Triangle> EarClip(List<Vector2> vectorList, bool isHole)
    {   // Я хочу получить integer-список, 
        List<Triangle> triangles = new List<Triangle>(vectorList.Count - 2); // Количество треугольников равно количеству вершин -2
        List<int> indices = new List<int>(vectorList.Count);
        for (int i = 0; i < vectorList.Count; i++) indices.Add(i);


        if (isHole)
        {
            indices.Reverse();
        }

        int safety = 0;
        while (safety < 100 && indices.Count != 3) { safety += 1;
            for (int i = 0; i < indices.Count; i++) 
            {
                //Debug.Log("inside");
                int A = indices[wrapAround(i, -1, indices.Count)];
                int B = indices[i];
                int C = indices[wrapAround(i, 1, indices.Count)];
                float currAngle = Get_ABC_angle(vectorList[A], vectorList[B], vectorList[C]);
                //Debug.Log(currAngle + " " + (currAngle >= straightAngle | currAngle <= flatAngle));
                if (currAngle >= straightAngle | currAngle <= flatAngle) continue;  // Тут могту появиться проблемы на очень больших полигонах, где все углы приближаются к 180. Но Это маловероятно
                if (MassContainPoint(A, B, C, vectorList)) continue;
                //if (ReturnFirstIntersectingEdge(vectorList, A, C)) { continue; }
                triangles.Add(new Triangle(A, B, C, isHole));
                //Debug.Log(A + " " + B + " " + C + " " + isHole);
                indices.RemoveAt(i);
                break;
            } 
        }
        // Добавление последних трех оставшихся вершин
        triangles.Add(new Triangle(indices[0], indices[1], indices[2], isHole));
        //Debug.Log(indices[0] + " " + indices[1] + " " + indices[2] + " " + isHole);
        /*
        Vector3Int lt = new Vector3Int(indices[0], indices[1], indices[2]);
        Vector3Int pt = triangles[triangles.Count - 1];
        float lastAngle = Get_ABC_angle(vectorList[lt.x], vectorList[lt.y], vectorList[lt.z]);
        Debug.Log(lastAngle + " " + (lastAngle >= straightAngle | lastAngle <= flatAngle));
        if (lastAngle >= straightAngle | lastAngle <= flatAngle)
        {           
            Vector3Int A2 = ConvexPoly2D.GetDifferringVerticeAndOverlap(lt, pt);
            Vector3Int A1 = ConvexPoly2D.GetDifferringVerticeAndOverlap(pt, lt);

            triangles[triangles.Count - 1] = new Vector3Int(A2.x, A2.y, A1.x);
            triangles.Add(new Vector3Int(A1.x, A1.y, A2.x));
        }*/

        //string sterrrrng = "";for (int i = 0; i < triangles.Count; i++) sterrrrng+= triangles[i].ToString() + " "; Debug.Log(sterrrrng);
        return triangles;
    }

    public static List<Vector3Int> EarClipLimited(List<Vector2> vectorList, int limit)
    {
        List<Vector3Int> triangles = new List<Vector3Int>(vectorList.Count - 2);
        List<int> indices = new List<int>(vectorList.Count);
        for (int i = 0; i < vectorList.Count; i++) indices.Add(i);

        int safety = 0;
        while (safety < limit && indices.Count != 3)
        {
            safety += 1;
            for (int i = 0; i < indices.Count; i++)
            {
                int A = indices[wrapAround(i, -1, indices.Count)];
                int B = indices[i];
                int C = indices[wrapAround(i, 1, indices.Count)];
                float currAngle = Get_ABC_angle(vectorList[A], vectorList[B], vectorList[C]);
                if (currAngle >= straightAngle | currAngle <= flatAngle) continue;
                if (MassContainPoint(A, B, C, vectorList)) continue;
                triangles.Add(new Vector3Int(A, B, C));
                indices.RemoveAt(i);
                break;
            }
        }
        if (safety < limit) triangles.Add(new Vector3Int(indices[0], indices[1], indices[2]));
        return triangles;
    }
    // Каждая ранка от объединения полигона с его дыркой оставляет дегенеративную грань, состоящую из двух наложеных друг на друга граней.
    // Эти грании должны быть объединены в одну, а треугольники корректно переформированы
    public static void HealScars(List<Vector2> vectorList, List<Triangle> triangleList)
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

    public static void HealScar(List<Vector2> vectorList, List<Triangle> triangleList, Vector2Int overlap, List<Vector2Int> overlaps)
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
            if (triangleList[i].a == b) { triangleList[i] = new Triangle(a, triangleList[i].b, triangleList[i].c, triangleList[i].isHole); continue; }
            if (triangleList[i].b == b) { triangleList[i] = new Triangle(triangleList[i].a, a, triangleList[i].c, triangleList[i].isHole); continue; }
            if (triangleList[i].c == b) { triangleList[i] = new Triangle(triangleList[i].a, triangleList[i].b, a, triangleList[i].isHole); continue; }
            continue;
        }

        for (int i = 0; i < triangleList.Count; i++)
        {
            Triangle originT = triangleList[i];
            Triangle triangle = new Triangle(
                originT.a <= b ? originT.a : originT.a - 1,
                originT.b <= b ? originT.b : originT.b - 1,
                originT.c <= b ? originT.c : originT.c - 1, originT.isHole);
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
                if (Poly2DToolbox.PointSimilarity(vertices[a], vertices[b]))
                    overlaps.Add(new Vector2Int(a, b));

        return overlaps;
    }


    public static List<Vector2> UniteHoles(Poly2D A, List<Poly2D> B)
    {
        List<Vector2> combined = new List<Vector2>(A.vertices);

        for (int i = 0; i < B.Count; i++)
        {
            (int ai, int bi) = UniteHole(combined, B[i].vertices);
            combined = StitchHole(combined, B[i].vertices, ai, bi);
        }
        return combined;
    }

    public static int wrapAround(int curr, int diff, int max)
    {
        return (curr + diff + max) % max;
    }

    // Возвращает градус от 0 до 360
    public static float Get_ABC_angle(Vector2 A, Vector2 B, Vector2 C)
    {
        Vector2 vecA = A - B;
        Vector2 vecC = C - B;

        float angle = Mathf.Atan2(vecA.y, vecA.x) - Mathf.Atan2(vecC.y, vecC.x);
        float degree_angle = (angle * Mathf.Rad2Deg + 360.0f) % 360.0f;
        return degree_angle;
    }

    // Присоединяет дырку 
    // Наивная имплементация, нет оптимизации проверки
    // TODO: make binary space partitioning optimization
    public static (int, int) UniteHole(List<Vector2> A, List<Vector2> B, int currAoverride = 0, int currBoverride = 0)
    {   // Алгоритм прыжковый, если есть пересечение с A, то перепрыгивает на грань с которой было пересечение
        // Если есть пересечение с самим собой, то перепрыгивает на грань где было пересечение
        int currA = currAoverride; int currB = currBoverride;
        int safety = 0; bool hasChanged = true;
        while (safety < 25)
        { safety += 1;
            //Vector2 locA = A[currA]; Vector2 locB = B[currB];
            if (!hasChanged) break; // если не было изменений значит не было пересечений
            hasChanged = false;//DebugUtilities.DebugDrawLine(A[currA], B[currB], Color.red);
            //Debug.Log(currA + " " + currB + " " + A.Count);
            if (ReturnFirstIntersectingEdge(A[currA], B, currB, out int newB))
            {
                hasChanged = true;
                //DebugUtilities.DebugDrawLine(B[currB], B[newB ], Color.blue);
                currB = newB;
            }
            
            if (ReturnFirstIntersectingEdge(B[currB], A, currA, out int newA))
            {
                hasChanged = true;
                //DebugUtilities.DebugDrawLine(A[currA], A[newA ], Color.red );
                currA = newA;
            }
            // Прыжок на соседнюю вершину, если она ближе чем текущая. Производится дополнительная проверка на пересечение для новой точки.
            if (JumpCloserToTarget(A[currA], B, currB, out int jumpB) && ReturnFirstIntersectingEdge(A[currA], B, jumpB, out int dummyB))
            {
                hasChanged = true;
                //DebugUtilities.DebugDrawLine(B[currB], B[jumpB], Color.cyan);
                currB = jumpB;
            }
            if (JumpCloserToTarget(B[currB], A, currA, out int jumpA) && ReturnFirstIntersectingEdge(B[currB], A, jumpA, out int dummyA))
            {
                hasChanged = true;
                //DebugUtilities.DebugDrawLine(A[currA], A[jumpA], Color.pink);
                currA = jumpA;
            }
        }
        //Debug.Log(safety);
        //DebugUtilities.DebugDrawLine(A[currA], B[currB], Color.green);
        return (currA, currB);
    }
    public static (int, int) UniteHoleOptimization(List<Vector2> A, List<Vector2> B, Bounds B_BBox)
    {
        int smallA = -1; float smallAdistance = float.PositiveInfinity;
        Vector2 center = B_BBox.center;
        for (int i = 0; i < A.Count; i++)
        {
            float dist = (A[i] - center).magnitude;
            if (dist < smallAdistance)
            {
                smallA = i;
                smallAdistance = dist;
            }
        }
        if (smallA == -1) return UniteHole(A, B);
        int smallB = -1; float smallBdistance = float.PositiveInfinity;
        Vector2 small_A_vertice = A[smallA];
        for (int i = 0; i < B.Count; i++)
        {
            float dist = (B[i] - small_A_vertice).magnitude;
            if (dist < smallAdistance)
            {
                smallB = i;
                smallBdistance = dist;
            }
        }
        //DebugUtilities.DebugDrawLine(A[smallA], B[smallB], Color.yellow); Debug.Log("Debug draw here");
        if (smallB == -1) return UniteHole(A, B);
        return UniteHole(A, B, smallA, smallB);
    }

    // Если грань между полигонами А и B пересекается с одним из obstacles, значит есть более короткая грань с этим obstacle
    public static int LinePolyIntersection(Vector2 A, Vector2 B, List<Poly2D> obstacles)
    {   
        for (int i = 0; i < obstacles.Count; i++) if (LinePolyIntersection(A, B, obstacles[i])) return i;
        return -1;
    }
    public static bool LinePolyIntersection(Vector2 A, Vector2 B, Poly2D obstacle)
    {
        return LinePolyIntersection(A, B, obstacle.vertices, obstacle.BBox);
    }
    public static bool LinePolyIntersection(Vector2 A, Vector2 B, List<Vector2> obstacle, Bounds BBox)
    {
        bool BBoxCheck = BoundsMathHelper.DoesLineIntersectBoundingBox2D(A, B, BBox);
        if (!BBoxCheck) return false;
        for (int i = 0; i < obstacle.Count; i++)
        {
            Vector2 v1 = obstacle[i];
            Vector2 v2 = obstacle[(i + 1) % obstacle.Count];
            if (LineLineIntersection(A, B, v1, v2, out Vector2 dummy)) return true;
        }
        return false;
    }

    public static bool DoesContainPoint(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float lowLine = 0.0f; float highLine = 1.0f;
        Vector2 v0 = B - A, v1 = C - A, v2 = P - A;
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float denom = d00 * d11 - d01 * d01;
        if (denom == 0.0f) return true;
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float v = (d11 * d20 - d01 * d21) / denom;
        if (v < lowLine || v > highLine) return false;
        float w = (d00 * d21 - d01 * d20) / denom;
        if (w < lowLine || w > highLine) return false;
        float u = 1.0f - v - w;
        if (u < lowLine || u > highLine) return false;
        return true;
    }

    public static bool IsPointInside(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {   // Этот кусочек кода - дубликат DoesContainPoint, был создан когда я подумал что DoesContainPoint сломана. (Проблема оказалась в качестве функции UnityHole)
        float AB = Vector2.SignedAngle(B - A, P - A);
        float BC = Vector2.SignedAngle(C - B, P - B);
        float CA = Vector2.SignedAngle(A - C, P - C);
        //Debug.Log(AB + " " + BC + " " + CA);
        if (AB <= -1.0f) return false;
        if (BC <= -1.0f) return false;
        if (CA <= -1.0f) return false;
        return true;
    }


    public static bool MassContainPoint(int A, int B, int C, List<Vector2> Points)
    {
        for (int i = 0; i < Points.Count; i++) {
            //Debug.Log(Points[A] + " " + Points[B] + " " + Points[C] + " " + Points[i]);
            if (PointSimilarity(Points[A], Points[i]) || PointSimilarity(Points[B], Points[i]) || PointSimilarity(Points[C], Points[i])) continue;
            //if (DoesContainPoint(Points[A], Points[B], Points[C], Points[i])) return true;
            if (DoesContainPoint(Points[A], Points[B], Points[C], Points[i])) return true;
        }
        //Debug.Log(A + " " + B + " " + C + " False");
        return false;
    }

    // Stitches A and B, new list will contain A+B+2 points. There wil be a duplicate point in both A and B to create a degenerate line.
    // It is possible to, instead of adding two degenerate vertices, represent combined polygon as a list of integer indices, same as what i do with Triangles.
    // benefit of approach above is no need to search for duplicate nodes to eliminate, which will provide a speedup
    public static List<Vector2> StitchHole(List<Vector2> A, List<Vector2> B, int Aindex, int Bindex)
    {
        Vector2[] stitched = new Vector2[A.Count + B.Count + 2];
        //string newLoop = "";
        int newIndex;
        for (int a = 0; a < Aindex; a++) {
            newIndex = a;
            stitched[newIndex] = A[a];              //newLoop += "I" + newIndex + " A" + a + "\n";
        }
        stitched[Aindex] = A[Aindex];
        //newLoop += "I" + Aindex + "_AI_" + Aindex + "\n"; Debug.Log(newLoop);
        for (int b = 0; b < B.Count - Bindex; b++) {
            newIndex = Aindex + b + 1;
            stitched[newIndex] = B[b + Bindex];
            //newLoop += "I" + newIndex + " B" + (b + Bindex) + "\n";
        }
        //Debug.Log(newLoop);
        for (int b = 0; b < Bindex; b++) {
            newIndex = Aindex + (B.Count - Bindex + 1) + b;
            stitched[newIndex] = B[b];
            //newLoop += "I" + newIndex + " B" + b + "\n";
        }

        stitched[Aindex + B.Count + 1] = B[Bindex];
        //newLoop += "I" + (Aindex + B.Count + 1) + "_BI_" + Bindex + "\n"; Debug.Log(newLoop);

        for (int a = 0; a < A.Count - Aindex; a++) {
            newIndex = Aindex + B.Count + 2 + a;
            stitched[newIndex] = A[a + Aindex];      //newLoop += "I" + newIndex + " A" + (a + Aindex) + "\n";
        }
        //Debug.Log(newLoop);
        /*
        Vector2Int overlapA = new Vector2Int(Aindex, Aindex + B.Count + 2);
        Vector2Int overlapB = new Vector2Int(Aindex + 1, Aindex + B.Count + 1);
        IncrementOverlap(overlaps, overlapA);
        IncrementOverlap(overlaps, overlapB);
        Debug.Log(overlapA + " " + stitched[overlapA[0]] + " " + stitched[overlapA[1]] + "\n" + overlapB + " " + stitched[overlapB[0]] + " " + stitched[overlapB[1]]);
        overlaps.Add(overlapA); overlaps.Add(overlapB);*/
        return new List<Vector2>(stitched);
    }

    // Outsider is a point that does not belong to a Poly
    // Poly - is plygon
    // Pvert - vertex ID of some vertex that belongs to List P

    public static bool ReturnFirstIntersectingEdge(Vector2 Outsider, List<Vector2> Poly, int Pvert, out int Pa /*, out int Pb*/) // Pb = Pa +1
    { 
        for (int i = 0; i < Poly.Count; i++) 
        {
            int j = (i + 1) % Poly.Count;
            if (Pvert == i | Pvert == j) continue; // I do not need an intersection with an edge that contains Pvert 
            if (LineLineIntersection(Outsider, Poly[Pvert], Poly[i], Poly[j], out Vector2 dumdum))
            {
                Pa = i;
                return true;
            }
        }
        Pa = -1;
        return false;
    }
    // Делает ровно один прыжок на одну из соседних точек если та находится ближе к целевой точке
    public static bool JumpCloserToTarget(Vector2 Outsider, List<Vector2> Poly, int prev_vert, out int new_vert /*, out int Pb*/) // Pb = Pa +1
    {
        int nextID = (prev_vert + 1) % Poly.Count;
        int prevID = (prev_vert - 1 + Poly.Count) % Poly.Count;
        Vector2 currP = Poly[prev_vert];
        Vector2 nextP = Poly[nextID];
        Vector2 prevP = Poly[prevID];
        float currM = (currP - Outsider).magnitude;
        float nextM = (nextP - Outsider).magnitude;
        float prevM = (prevP - Outsider).magnitude;
        new_vert = prev_vert;
        /*Debug.Log(currM + " " + nextM + " " + prevM);
        DebugUtilities.DebugDrawLine(currP, Outsider, Color.red);
        DebugUtilities.DebugDrawLine(nextP, Outsider, Color.orange);
        DebugUtilities.DebugDrawLine(prevP, Outsider, Color.yellow);*/
        if (nextM < prevM)  {
            if (nextM < currM)
            {
                new_vert = nextID;
                return true;
            }
        } else {
            if (prevM < currM)
            {
                new_vert = prevID;
                return true;
            }
        }
        return false;
    }

    // Returns an true if ther is an intersection between an edge built on top of 2 polygon vertices and native poligon edges
    public static bool ReturnFirstIntersectingEdge(List<Vector2> Poly, int Aid, int Bid) 
    {
        for (int i = 0; i < Poly.Count; i++)
        {
            int j = (i + 1) % Poly.Count;// I do not need an intersection with an edge that contains Pvert 
            if (PointSimilarity(Poly[Aid], Poly[i]) | PointSimilarity(Poly[Aid], Poly[j])) continue; //
            if (PointSimilarity(Poly[Bid], Poly[i]) | PointSimilarity(Poly[Bid], Poly[j])) continue; //  | PointSimilarity(Poly[Bid], Poly[j])
            if (LineLineIntersection(Poly[Aid], Poly[Bid], Poly[i], Poly[j], out Vector2 dumdum)) 
            {
                return true;
            }
        }
        return false;
    }

    // Предполагается что точки в полигонах уже отсортированы против часоовй стрелки. 
    // Оба полигона должны быть выпуклыми
    public static List<Vector2> MergePolygons(List<Vector2> A, List<Vector2> B)
    {   // Не работает, я реализовал GH алгоритм объединения
        Debug.Log("NOT IMPLEEMNTED");
        //List<Intersection> intersections = new List<Intersection>();
        List<Vector2> result = new List<Vector2>();
        // N^2, не нравится мне это, но мне пока лень упрощать проверку
        // Как вариант можно рассчитать BBox для аолигона А и B 
        // Отдельно проверить какие грани А пересикаются с BBox B и наоборот, а потом сделать ОN^2 проверку на маленьком наборе
        for (int a1 = 0; a1 < A.Count; a1++) {
            for (int b1 = 0; b1 < B.Count; b1++) {
                int a2 = (a1 + 1) % A.Count;
                int b2 = (b1 + 1) % B.Count;

                Vector2 intersection_point;
                float distance;
                bool did_intersect = LineLineIntersection(A[a1], A[a2], B[b1], B[b2], out intersection_point, out distance);
                //Debug.LogFormat("{0} {1} {2} {3} {4}", a1, a2, b1, b2, did_intersect);
                if (did_intersect) {
                    //intersections.Add(new Intersection(a1, a2, b1, b2, intersection_point, distance));
                    result.Add(intersection_point);
                }
            }
        }

        return result;
    }
    /// <summary>
    /// Находит  линию или же точку если вершина находится на границе полигона
    /// </summary>
    /// <param name="point"></param>
    /// <param name="points"></param>
    /// <returns>(-1, -1) - Точка не найдена <br/>
    /// (n, -1) - Точка равняется точке n внутри полигона<br/>
    /// (n, m) - Точка коллинеарна линии (n, m), и лежит между этими двумя точками
    /// </returns>
    public static (int, int) PointOnBorder(Vector2 point, List<Vector2> points)
    {   // Находит линию или же точку если вершина находится на границе полигона
        // Возвращает пару индексов верин полигона. Если пара равна (n, -1) значит точка равна точке n
        for (int i = 0; i < points.Count; i++)
        {
            if (Poly2DToolbox.PointSimilarity(point, points[i])) return (i, -1);
            int j = (i + 1) % points.Count;
            Edge2D edge = new Edge2D(points[i], points[j]);
            
            if (!PointBelongToRay2D(points[i], (points[j] - points[i]).normalized, point, out float t))  continue;
            if (t >= 1.0f) continue;
            return (i, j);
        }
        return (-1, -1);
    }

    // Returns FALSE if point is outside, and TRUE if inside
    public static bool IsPointInsidePolygon(Vector2 point, List<Vector2> points)
    { // Я кидаю луч с лева (-1) на право до точки. 
        bool intersection_counter = false; // Я его просто флипаю, а не инкрементриурю. Иначе в конце придется сделать IC % 2
        //int debug_counter = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (PointInsidePolygonHorizontalRaycast(points[i], points[(i + 1) % points.Count], point))
            {
                intersection_counter = !intersection_counter;
                //debug_counter += 1;
            }
        }
        //Debug.Log(debug_counter);
        return intersection_counter;
    }

    private static bool PointInsidePolygonHorizontalRaycast(Vector2 A, Vector2 B, Vector2 targetPoint)
    {
        return (targetPoint.y < A.y != targetPoint.y < B.y) &&
            (targetPoint.x < A.x + ((targetPoint.y - A.y) / (B.y - A.y)) * (B.x - A.x));
    }


    public static bool IsInsidePolygonConvex(List<Vector2> vertices, Vector2 p, bool isHole)
    {
        if (isHole == true) // Ориентация часовой
            for (int i = vertices.Count - 1; i < 0; i--)
            {
                int j = (i - 1 + vertices.Count) % vertices.Count;
                if (isLeft(p, vertices[i], vertices[j])) return false;
            }
        else
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                if (isRight(p, vertices[i], vertices[j])) return false;
            }
        return true;
    }

    public static bool AreCrossing(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out Vector2 crossing)
    {
        // Переписка кода вот отсюда: https://habr.com/ru/articles/267037/
        // Вроде работает. TODO: замерить производительность Этой и другой функций и выбрать лучшую
        crossing = Vector2.zero;

        Vector2 cut1 = A2 - A1;
        Vector2 cut2 = B2 - B1;

        float prod1 = Poly2DToolbox.Cross(cut1, B1 - A1);
        float prod2 = Poly2DToolbox.Cross(cut1, B2 - A1);
        //Debug.LogFormat("{0} {1}", prod1, prod2);
        if (Mathf.Sign(prod1) == Mathf.Sign(prod2) || prod1 == 0 || prod2 == 0) return false;

        prod1 = Poly2DToolbox.Cross(cut2, A1 - B1);
        prod2 = Poly2DToolbox.Cross(cut2, A2 - B1);

        //Debug.LogFormat("{0} {1}", prod1, prod2);
        if (Mathf.Sign(prod1) == Mathf.Sign(prod2) || prod1 == 0 || prod2 == 0) return false;

        float t = Mathf.Abs(prod1) / Mathf.Abs(prod2 - prod1);
        crossing = new Vector2(
            A1.x + cut1.x * t,
            A1.y + cut1.y * t
        );

        return true;
    }
    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
    // Возвращает точку пересечения между линией AB и CD.
    // Также возвращает расстояние от точки А до точки пересечения.
    public static bool LineLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 interPoint, out float distance)
    {   // Выдернуто из другого проекта
        interPoint = Vector2.zero;
        distance = 0;
        float det = (A.x - B.x) * (C.y - D.y) - (A.y - B.y) * (C.x - D.x);

        if (Mathf.Abs(det) < Geo3D.epsilon)  return false;
        
        float X = (A.x * B.y - A.y * B.x) * (C.x - D.x) - (A.x - B.x) * (C.x * D.y - C.y * D.x);
        float Y = (A.x * B.y - A.y * B.x) * (C.y - D.y) - (A.y - B.y) * (C.x * D.y - C.y * D.x);
        det = 1 / det;
        interPoint = new Vector2(X * det, Y * det);

        // Проверка принадлежности к первой линии
        Vector2 diffAB = B - A;
        Vector2 interDiffAB = interPoint - A;
        float dotAB = Vector2.Dot(diffAB, interDiffAB);
        if (dotAB < 0)  return false;
        if (dotAB > diffAB.sqrMagnitude) return false;

        // Проверка принадлежности ко второй линии
        Vector2 diffCD = D - C;
        Vector2 interDiffCD = interPoint - C;
        float dotCD = Vector2.Dot(diffCD, interDiffCD);
        if (dotCD < 0) return false;
        if (dotCD > diffCD.sqrMagnitude) return false;

        distance = interDiffAB.magnitude;
        return true;
    }

    public static bool LineLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 interPoint)
    {
        float Dummy;
        return LineLineIntersection(A, B, C, D, out interPoint, out Dummy);
    }

    public static bool PointSimilarity(Vector2 A, Vector2 B, float epsilon = Geo3D.epsilon)
    { // если разница в обоих координатах меньше эпсилон, то это одна и та же точка
        //Debug.Log(A.ToString() + "  " + B.ToString());
        return (Mathf.Abs(A.x - B.x) < epsilon) & (Mathf.Abs(A.y - B.y) < epsilon);
    }

    public static float AreaShoelace(List<Vector2> points)
    {   // Can be used to identify order of vertices. Positive area - counter clockwise | Negative Area - clockwise | 0 Implies polygon is flat
        float area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].x * points[j].y - points[i].y * points[j].x;
        }
        return area / 2.0f;
    }

    public static float AreaTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        return ( A.x * (B.y - C.y) + B.x * (C.y - A.y) + C.x * (A.y - B.y)) * 0.5f;
    }
    public static bool SelfIntersectionNaive(List<Vector2> points)
    {
        if (points.Count < 3) return true;
        if (points.Count == 3) return false;

        for (int a = 0; a < points.Count; a++)
        {
            int a2 = (a + 1) % points.Count;
            for (int b = 2; b <= points.Count - 2; b++)
            {
                int b1 = (b + a + points.Count + 0) % points.Count;
                int b2 = (b + a + points.Count + 1) % points.Count;
                //Debug.Log("ab: " + a + " " + b + " a1 a2: " + a + " " + a2 + " b1 b2: " + b1 + " " + b2);
                if (LineLineIntersection(points[a], points[a2], points[b1], points[b2], out Vector2 dummy))  return true;
            }
        }
        return false;
    }

    // Does A belong to B?
    // 1 B inside A / 0 even level / -1 A inside B
    // If polygons are a result of Greiner-Hoffmann's algorithm, then they are almost guaranteed to have no intersections
    // It means that there are only 3 cases: A fully engulfed by B, A and B are separate, A fully engulfs B (so there are no reason to check all points, single point is sufficient)
    // There are so degenerate cases, mostly when a point A belongs to an edge B.
    public static int DoesPolygonContainOther(Poly2D A, Poly2D B)
    {
        return DoesPolygonContainOther(A.vertices, B.vertices, A.BBox, B.BBox);
    }
    public static int DoesPolygonContainOther(List<Vector2> A, List<Vector2> B, Bounds Ab, Bounds Bb)
    {
        if (!Ab.Intersects(Bb)) { return 0; }// Они раздельны
        bool is_B_Inside = IsPointInsidePolygon(B[0], A);
        bool is_A_Inside = IsPointInsidePolygon(A[0], B);
        //Debug.Log(is_B_Inside.ToString() + " " + is_A_Inside.ToString());
        if (is_B_Inside) return 1;
        if (is_A_Inside) return -1;
        return 0;
    }
    public static bool DoesPolygonContainOtherBool(Poly2D A, Poly2D B)
    {
        return (DoesPolygonContainOther(A, B) == 1);
    }

    public static bool DoesLineIntersectPolygon(Vector2 A, Vector2 B, List<Vector2> P)
    {
        for (int i = 0; i < P.Count; i++)
        {
            int j = (i + 1) % P.Count;
            if (LineLineIntersection(A, B, P[i], P[j], out Vector2 dumdum)) return true;
        }
        return false;
    }


    public static bool DoesLineIntersectPolygon(Vector2 A, Vector2 B, Poly2D P)
    {
        //P.BBox.
        return false;
    }
    public static bool PointBelongToLine2D(Vector2 origin, Vector2 direction, Vector2 point)
    {
        return PointBelongToRay2D(origin, direction, point) | PointBelongToRay2D(origin, -direction, point);
    }
    private static bool PointBelongToRay2D(Vector2 origin, Vector2 direction, Vector2 point)
    {   // Просто сравниваю направления векторов, если они слишком разнятся то точка не принадлежит линии
        Vector3 p_dir = (point - origin).normalized;
        if (Mathf.Abs(p_dir.x - direction.x) > Geo3D.epsilon) return false;
        if (Mathf.Abs(p_dir.y - direction.y) > Geo3D.epsilon) return false;
        return true;
    }
    private static bool PointBelongToRay2D(Vector2 origin, Vector2 direction, Vector2 point, out float t)
    {   // Просто сравниваю направления векторов, если они слишком разнятся то точка не принадлежит линии
        t = 0;
        if (direction.x <= Geo3D.epsilon | direction.y <= Geo3D.epsilon) return false;
        Vector3 p_dir = (point - origin).normalized;
        if (Mathf.Abs(p_dir.x - direction.x) > Geo3D.epsilon) return false;
        if (Mathf.Abs(p_dir.y - direction.y) > Geo3D.epsilon) return false;
        if (direction.x > Geo3D.epsilon) t = p_dir.x / direction.x; 
        else if (direction.x > Geo3D.epsilon) t = p_dir.y / direction.y;
        return true;
    }

    public static float TriangleCross(Vector2 p1, Vector2 p2, Vector2 p3)
    {   // 0 - collinear
        // negative - thos point is to the right from O->P line || clockwise triangle
        // positive - this point is to the left  from O->P line ||  counter clockwise triangle
        return ((p3.x - p2.x) * (p1.y - p2.y) - (p3.y - p2.y) * (p1.x - p2.x));
    }

    public static bool isRight(Vector2 Q, Vector2 p1, Vector2 p2)
    {
        return TriangleCross(Q, p1, p2) < Geo3D.epsilon;
    }
    public static bool isLeft(Vector2 Q, Vector2 p1, Vector2 p2)
    {
        return TriangleCross(Q, p1, p2) > Geo3D.epsilon;
    }
    //public static bool Coll

    public static float SignedAngle(Vector2 P2, Vector2 P1, Vector2 P3)
    {   // Я помню что реализовывал эту функцию, куда она потерялась?
        float angle = -(Mathf.Atan2(P3.y - P1.y, P3.x - P1.x) - Mathf.Atan2(P2.y - P1.y, P2.x - P1.x)) * Mathf.Rad2Deg;
        return (angle + 360.0f) % 360.0f;
    }

    public static bool IsReflex(Vector2 P2, Vector2 P1, Vector2 P3)
    {
        return SignedAngle(P2, P1, P3) > 180.0f;
    }

    public static bool IsConvex(List<Vector2> points, bool isHole)
    {
        if (points.Count < 3) return false;
        if (points.Count == 3) return true;
        int pc = points.Count;
        
        for (int i = 0; i < pc; i++)
        {
            int a = i;
            int b = (i + 1) % pc;
            int c = (i + 2) % pc;
            if (isHole) (a, c) = (c, a); // Меняются местами для установления корректного порядка
            float angle = SignedAngle(points[a], points[b], points[c]);
            if (angle >= straightAngle) return false;
        }
        return true;
    }


    public static void ConvexHull(List<Vector2> polygon)
    {
        //List<int> points = 
    } 



}

// Filters out degenerate line fragments that overlap. These fragments are used to bind Holes to Hull
// Degenerates come in pairs, so length is always even.
/*
public static bool ReturnFirstIntersectingEdgeDegenerateFilter(Vector2 Outsider, int Pvert, List<Vector2> Poly, List<int> degenerates, out int Pa)
{
    for (int i = 0; i < Poly.Count; i++)
    {
        int j = (i + 1) % Poly.Count;
        if (Pvert == i | Pvert == j) continue; // I do not need an intersection with an edge that contains Pvert 
        if (LineLineIntersection(Outsider, Poly[Pvert], Poly[i], Poly[j], out Vector2 dumdum))
        {
            Pa = i;
            return true;
        }
    }
    Pa = -1;
    return false;
}*/

/*
    private static bool PointInsidePolygonHorizontalRaycastLegacy(Vector2 A, Vector2 B, Vector2 targetPoint)
    {
        if (A.x >= targetPoint.x & B.x >= targetPoint.x) return false; // Луч кастуется слева, если оба x отрезка справа от точки значит пересечения быть не может
        if (A.y > targetPoint.y & B.y > targetPoint.y) return false; // Если обе точки выше цели, значит луч ниже отрезка
        if (A.y < targetPoint.y & B.y < targetPoint.y) return false; // Если обе точки ниже цели, значит луч выше отрезка
        return true;
    }
 
    public static bool LineLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 intersection)
    {
        Debug.Log("НЕ РАБОТАЕТ. ТОЧКИ НЕ СТАВЯТСЯ КОРРЕКТНО");
        intersection = Vector2.zero;
        float d = (D.y - C.y) * (B.x - A.x) - (D.x - C.x) * (B.y - A.y);

        if (d < Geo3D.epsilon) return false;

        float numerator1 = (D.x - C.x) * (A.y - C.y) - (D.y - C.y) * (A.x - C.x);
        float numerator2 = (B.x - A.x) * (A.y - C.y) - (B.y - A.y) * (A.x - C.x);

        float t = numerator1 / d;
        float u = numerator2 / d;

        Debug.LogFormat("{0} {1} {2} {3}", A, B, C, D);
        bool statement = t >= 0f && t <= 1f && u >= -0f && u <= 1f;
        Debug.Log(t.ToString() + " " + u + " " + statement);
        if (statement)
        {
            intersection = A + t * (B - A);
            return true;
        }

        return false;
    }
 */