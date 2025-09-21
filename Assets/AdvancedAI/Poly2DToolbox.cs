using UnityEngine;
using System.Collections.Generic;
// Я запрещаю кому-либо использовать написанный мной код для обучения нейросетей. Это моя интеллектуальная собственность.
// I forbid anyone to use code, written by me, to train neural networks. It is my intellectual property.

// Работа с двухмерными полигонами. 
// Полигоны в 3D пространстве надо привести к двухмерному виду при помощи алгоритма из NavPoly3D
// TODO: Перекинуть все связанное с триангуляцией в отдельный файлик
public static class Poly2DToolbox
{
    
    // Предполагается что полигоны появились в результате GH объединения. Наружный полигон содержит 
    // Не забывай сохранять подаваемые на вход полигоны-дыры, они тоже могут быть важны для навигации 
    public static List<Vector3Int> EarClip(List<Vector2> vectorList)
    {   // Я хочу получить integer-список, 
        List<Vector3Int> triangles = new List<Vector3Int>(vectorList.Count - 2); // Количество треугольников равно количеству вершин -2
        List<int> indices = new List<int>(vectorList.Count);
        for (int i = 0; i < vectorList.Count; i++) indices.Add(i);

        int safety = 0;
        while (safety < 100 && indices.Count != 3) { safety += 1;
            for (int i = 0; i < indices.Count; i++) 
            {
                int A = indices[wrapAround(i, -1, indices.Count)];
                int B = indices[i];
                int C = indices[wrapAround(i, 1, indices.Count)];
                //Debug.Log(Get_ABC_angle(vectorList[A], vectorList[B], vectorList[C]));
                if (Get_ABC_angle(vectorList[A], vectorList[B], vectorList[C]) > 180.0f) continue;
                if (MassContainPoint(A, B, C, vectorList)) continue;
                //if (ReturnFirstIntersectingEdge(vectorList, A, C)) { continue; }
                triangles.Add(new Vector3Int(A, B, C));
                indices.RemoveAt(i);
                break;
            } 
        }
        triangles.Add(new Vector3Int(indices[0], indices[1], indices[2]));
        //string sterrrrng = "";for (int i = 0; i < triangles.Count; i++) sterrrrng+= triangles[i].ToString() + " "; Debug.Log(sterrrrng);
        return triangles;
    }

    public static List<Vector2> UniteHoles(Poly2D A, List<Poly2D> B)
    {
        List<Vector2> combined = new List<Vector2>(A.vertices);

        for (int i = 0; i < B.Count; i++)
        {
            (int ai, int bi) = UniteHole(combined, B[i]);
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
    public static (int,int) UniteHole(List <Vector2> A, List<Vector2> B)
    {   // Алгоритм прыжковый, если есть пересечение с A, то перепрыгивает на грань с которой было пересечение
        // Если есть пересечение с самим собой, то перепрыгивает на грань где было пересечение
        int currA = 0; int currB = 0;
        int safety = 0; bool hasChanged = true;
        while (safety < 10)
        { safety += 1;
            //Vector2 locA = A[currA]; Vector2 locB = B[currB];
            if (!hasChanged) break; // если не было изменений значит не было пересечений
            hasChanged = false;//DebugUtilities.DebugDrawLine(A[currA], B[currB], Color.red);
            if (ReturnFirstIntersectingEdge(A[currA], B, currB, out int newB))
            {
                hasChanged = true;
                //DebugUtilities.DebugDrawLine(B[currB], B[newB], Color.violet);
                currB = newB;
            }
            
            if (ReturnFirstIntersectingEdge(B[currB], A, currA, out int newA))
            {
                hasChanged = true;
                //DebugUtilities.DebugDrawLine(A[currA], A[newA], Color.yellow);
                currA = newA;
            }
        }
        //DebugUtilities.DebugDrawLine(A[currA], B[currB], Color.green);
        return (currA, currB);
    }

    // Uses heuristics to find two points that are separated by the least distance.
    public static (int, int) UniteHole(List<Vector2> A, Poly2D B)
    {   // Алгоритм прыжковый, если есть пересечение с A, то перепрыгивает на грань с которой было пересечение
        // Если есть пересечение с самим собой, то перепрыгивает на грань где было пересечение
        int currA = 0; int currB = 0;
        int safety = 0; bool hasChanged = true;

        int[] AindicesArr = new int[A.Count];
        for (int i = 0; i < A.Count; i++) AindicesArr[i] = i;
        Vector2 center = B.BBox.center;
        List<int> Aindices = new List<int>(AindicesArr);
        Aindices.Sort((a, b) => { // Sort by distance to center
            return (center - A[a]).magnitude.CompareTo((center - A[b]).magnitude);
        });
        currA = Aindices[0];

        while (safety < 20)
        {
            safety += 1;
            if (!hasChanged) break; // если не было изменений значит не было пересечений
            hasChanged = false;
            if (ReturnFirstIntersectingEdge(A[currA], B.vertices, currB, out int newB))
            {
                hasChanged = true;
                currB = newB;
            }

            if (ReturnFirstIntersectingEdge(B.vertices[currB], A, currA, out int newA))
            {
                hasChanged = true;
                currA = newA;
            }
        }
        return (currA, currB);
    }

    public static bool DoesContainPoint(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        Vector2 v0 = B - A, v1 = C - A, v2 = P - A;
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float denom = d00 * d11 - d01 * d01;
        if (denom == 0.0f) return false;
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float v = (d11 * d20 - d01 * d21) / denom;
        if (v < 0.0f || v > 1.0f) return false;
        float w = (d00 * d21 - d01 * d20) / denom;
        if (w < 0.0f || w > 1.0f) return false;
        float u = 1.0f - v - w;
        if (u < 0.0f || u > 1.0f) return false;
        return true;
    }

    public static bool MassContainPoint(int A, int B, int C, List<Vector2> Points)
    {
        for (int i = 0; i < Points.Count; i++) {
            //Debug.Log(Points[A] + " " + Points[B] + " " + Points[C] + " " + Points[i]);
            if (PointSimilarity(Points[A], Points[i]) || PointSimilarity(Points[B], Points[i]) || PointSimilarity(Points[C], Points[i])) continue;
            if (DoesContainPoint(Points[A], Points[B], Points[C], Points[i])) 
            {
                //Debug.Log(A + " " + B + " " + C + " " + i + " True");
                return true; 
            }
        }
        //Debug.Log(A + " " + B + " " + C + " False");
        return false;
    }

    // Stitches A and B, new list will contain A+B+2 points. There wil be a duplicate point in both A and B to create a degenerate line.
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
        if (Mathf.Abs(det) < Geo3D.epsilon) return false; 
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

    // Does A belong to B?
    // 1 B inside A / 0 even level / -1 A inside B
    // If polygons are a result of Greiner-Hoffmann's algorithm, then they are almost guaranteed to have no intersections
    // There are so degenerate cases, mostly when a point A belongs to an edge B.
    public static int DoesPolygonContainOther(Poly2D A, Poly2D B)
    {
        if (!A.BBox.Intersects(B.BBox)) { return 0; }// Они раздельны
        bool is_B_Inside = IsPointInsidePolygon(B.vertices[0], A.vertices);
        bool is_A_Inside = IsPointInsidePolygon(A.vertices[0], B.vertices);
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

    public static float SignedAngle(Vector2 P2, Vector2 P1, Vector2 P3)
    {   // Я помню что реализовывал эту функцию, куда она потерялась?
        float angle = -(Mathf.Atan2(P3.y - P1.y, P3.x - P1.x) - Mathf.Atan2(P2.y - P1.y, P2.x - P1.x)) * Mathf.Rad2Deg;
        return (angle + 360.0f) % 360.0f;
    }

    public static bool IsReflex(Vector2 P2, Vector2 P1, Vector2 P3)
    {
        return SignedAngle(P2, P1, P3) > 180.0f;
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