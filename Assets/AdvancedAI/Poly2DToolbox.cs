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
    public static List<Vector2> MergePolygons(List<Vector2> A, List<Vector2> B)
    {
        List<Intersection> intersections = new List<Intersection>();
        List<Vector2> result = new List<Vector2>();
        // N^2, не нравится мне это, но мне пока лень упрощать проверку
        // Как вариант можно рассчитать BBox для аолигона А и B 
        // Отдельно проверить какие грани А пересикаются с BBox B и наоборот, а потом сделать ОN^2 проверку на маленьком наборе
        for (int a1 = 0; a1 < A.Count; a1++) {
            for (int b1 = 0; b1 < B.Count; b1++) {
                int a2 = (a1 + 1) % A.Count;
                int b2 = (b1 + 1) % B.Count;
                Vector2 intersection_point = Vector2.zero;
                if (LineLineIntersection(A[a1], A[a2],B[b1], B[b2], out intersection_point)) {
                    intersections.Add(new Intersection(a1, a2, b1, b2, intersection_point));
                }
            }
        }

        if (intersections.Count == 0) 
        {
            // Тут нет пересечений, либо полигоны уделены друг от друга либо же один сидит внутри дргуого
            // Тут нужно проверить с какой стороны относительно грани A расположена произвольная точка B:
            // Если снаружи значит они раздельны, если внутри то полигон можно поглотить или удалить, нет разницы
            Debug.Log("нет пересечений!");
            return result;
        }

        
        Debug.Log("NOT IMPLEEMNTED");
        return result;
    }

    private struct Intersection
    { // AB x CD пересечение линий
        int A1;
        int A2;
        int B1;
        int B2;
        Vector2 point;

        public Intersection(int A1, int A2, int B1, int B2, Vector2 point)
        {
            this.A1 = A1;
            this.A2 = A2;
            this.B1 = B1;
            this.B2 = B2;
            this.point = point;
        }
    }

    public static bool LineLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float d = (D.y - C.y) * (B.x - A.x) - (D.x - C.x) * (B.y - A.y);

        if (d < Geo3D.epsilon) return false;

        float numerator1 = (D.x - C.x) * (A.y - C.y) - (D.y - C.y) * (A.x - C.x);
        float numerator2 = (B.x - A.x) * (A.y - C.y) - (B.y - A.y) * (A.x - C.x);

        float t = numerator1 / d;
        float u = numerator2 / d;

        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersection = A + t * (B - A);
            return true;
        }

        return false;
    }


}
