using UnityEngine;
using System.Collections.Generic;
// Работа с двухмерными полигонами. 
// Полигоны в 3D пространстве надо привести к двухмерному виду при помощи алгоритма из NavPoly3D
public class Poly2DToolbox
{
    // Пока бесполезный алгоритм, т.к. у меня нет инструмента для объединения двух полигонов

    public static void EarClip(List<Vector2> points)
    {

    }

    // Предполагается что точки в полигонах уже отсортированы против часоовй стрелки. 
    // Оба полигона должны быть выпуклыми
    public static List<Vector2> MergePolygons(List<Vector2> A, List<Vector2> B)
    {
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

    public static bool PointSimilarity(Vector2 A, Vector2 B, float epsilon)
    { // если разница в обоих координатах меньше эпсилон, то это одна и та же точка
        //Debug.Log(A.ToString() + "  " + B.ToString());
        return (Mathf.Abs(A.x - B.x) < epsilon) & (Mathf.Abs(A.y - B.y) < epsilon);
    }

    public static float AreaShoelace(List<Vector2> points)
    {
        // Can be used to identify order of vertices. Positive area - counter clockwise | Negative Area - clockwise
        // 0 Implies polygon is flat
        float area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].x * points[j].y - points[i].y * points[j].x;

        }
        return area / 2.0f;
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

        if (is_B_Inside)
        {
            return 1;
        }
        if (is_A_Inside)
        {
            return -1;
        }

        return 0;
    }
    public static bool DoesPolygonContainOtherBool(Poly2D A, Poly2D B)
    {
        return (DoesPolygonContainOther(A, B) == 1);
    }

}

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