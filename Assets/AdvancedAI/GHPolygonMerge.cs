using System.Collections.Generic;
using UnityEngine;

public static class GHPolygonMerge
{
    private struct GH_Intersection // Greiner Hoff intersection
    { // Пересечение для алгоритма. Представляет собой голову - перечение двух отрезков, на которой растет волосок из последующих до следующего пересечения точек
        //int A1; int A2; int B1; int B2;
        public Vector2 intersectionPointID;
        public List<int> followingPointsIDs;
        public bool boolInside;
        public GH_Intersection(Vector2 intersect, List<int> points, bool inside)
        {
            this.intersectionPointID = intersect;
            this.followingPointsIDs = points;
            this.boolInside = inside;
        }
    }
    // Greiner Hoffman - очень сложный алгоритм, просто бессмысленно сложный.
    // Самое неприятное то, что он сохраняет дырки и отдельные треугольники. 
    // I.E. в одном объекте полигона мне ридется хранить несколько петель для внешних и внутренних границ.
    // Вместо этого я лучше триагрулирую свои полигоны на выпуклые и каждый с каждым порежу. 

    // Ainside/Binside - what side to keep, the one outside other polygon, or one isnide
    // True/True --- intersection
    // False/False --- union
    // True/false --- A - B
    // False/True --- B - A
    // Это односторонний грейнер хофф. Делится только один треугольник, и только одна часть его сторон возвращается
    // Думаю можно сделать одновременную проверку AB и BA, но это кажется сложным, ведь придется иногда делать шаги назад или вперед.
    // Еще читаемость кода упадет, а я не выдержу макароны распутывать
    private static List<GH_Intersection> SingleGreinerHoffmann(List<Vector2> A, List<Vector2> B, bool Ainside, float local_epsilon = Geo3D.epsilon)
    { // Each has to subdivide each. Обновление списка происходит при пересечении
        List<GH_Intersection> AB_intersections = new List<GH_Intersection>();

        // Список меток, отмечает какая точка является пересечением
        List<bool> isIntersection = new List<bool>(A.Count);
        for (int i = 0; i < A.Count; i++) isIntersection.Add(false);

        // Полигон B режет А. Внутри А появляются новые точки-пересечения.
        int safety = 0; int safety_limit = 125; // Мне не нравится когда зависает юнити
        for (int a1 = 0; a1 < A.Count; a1++)
        {
            safety += 1; if (safety > safety_limit) break;

            int a2 = (a1 + 1) % A.Count;
            for (int b1 = 0; b1 < B.Count; b1++)
            {
                safety += 1; if (safety > safety_limit) break;

                int b2 = (b1 + 1) % B.Count;
                Vector2 intersection;  // дистанция не нужна в этом 
                bool doesIntersect = Poly2DToolbox.LineLineIntersection(A[a1], A[a2], B[b1], B[b2], out intersection);

                // Прежде чем добавить точку она сравнивается со следующей и предыдущей, чтобы избежать дубликатов.
                local_epsilon = 0.01f; // TODO: удалить эту штуку, сейчас мне нужно не спамить точками
                if (doesIntersect) Debug.LogFormat("{0}, {1}, {2}", A[a1], intersection, A[a2]);
                if (doesIntersect && !Geo3D.PointSimilarity(A[a1], intersection, local_epsilon) && !Geo3D.PointSimilarity(A[a2], intersection, local_epsilon))
                {
                    Debug.Log("insertion phase");
                    A.Insert(a1 + 1, intersection);
                    isIntersection.Insert(a1 + 1, true);

                    a1 = a1 - 1;// Программа делает шаг назад и заново начинает проверки
                    break; // Т.К. Произошло разделение грани a1a2 на a1X / Xa2. Каждая из этих граней может иметь свои пересечения с полигоном B 
                }
            }
        }
        bool isInside = Poly2DToolbox.IsPointInsidePolygon(A[0], B);
        if (isInside) DebugUtilities.DebugDrawCross(A[0], Color.red);
        else DebugUtilities.DebugDrawCross(A[0], Color.blue);

        // MARKING every point as inside outside depending on starting point and intersection coujnt
        bool[] insideOutsideList = new bool[A.Count];

        for (int i = 0; i < A.Count; i++)
        {
            if (isIntersection[i]) { isInside = !isInside; }
            insideOutsideList[i] = isInside;
        }

        int first_difference = -1; // В этой переменной хранится первая разница на позиции I/I+1
        for (int i = 0; i < insideOutsideList.Length; i++)
        {
            int j = (i + 1) % insideOutsideList.Length;
            if (insideOutsideList[i] != insideOutsideList[j])
            {
                first_difference = i;
                break;
            }
        }
        if (first_difference == -1) { Debug.Log("No separations failure"); return new List<GH_Intersection>(); } // No separations


        // A1 и A2 compared, if they differ - new fragment, if they are same - append point to previous fragment
        for (int a = 0; a < A.Count; a++)
        {   //AB_intersections
            int a1 = (a + first_difference) % A.Count;
            int a2 = (a1 + 1) % A.Count; // A2 главнее чем A1, так как проверка производится именно на ней
            Debug.Log(a1.ToString() + " " + a2.ToString());
            if (insideOutsideList[a2] != Ainside)
            {   // Если A2 не равна входной переменной, то такая точка нам не интересна
                if (insideOutsideList[a1] != Ainside)
                { // Если ни одна точка не является входной переменной то скип
                    continue;
                }
                if (AB_intersections.Count != 0)
                {
                    AB_intersections[AB_intersections.Count - 1].followingPointsIDs.Add(a1);

                }
            }

            else
            {
                // insideOutsideList[a2] = Ainside // Тут гарантированное равенство
                if (insideOutsideList[a1] != Ainside) // Если неравно значит два значения разнятся. Надо создать новое пересечение
                {
                    Debug.Log("Adding new intersectino");
                    AB_intersections.Add(new GH_Intersection(A[a2], new List<int>(), Ainside));
                    continue;
                }
                Debug.Log("Adding new point to intersection");
                AB_intersections[AB_intersections.Count - 1].followingPointsIDs.Add(a2);
            }
        }

        //string boolstring = "IsIntersection: ";
        //foreach (bool item in isIntersection) { boolstring += item + " "; }
        //Debug.Log(boolstring);

        string boolstring = "Inside/Outside: ";
        foreach (bool item in insideOutsideList) { boolstring += item + " "; }
        Debug.Log(boolstring);

        foreach (GH_Intersection intersection in AB_intersections)
        {
            string interstring = "Intersection " + intersection.intersectionPointID + " ";
            foreach (int item in intersection.followingPointsIDs)
            {
                interstring += item + " ";
            }
            Debug.Log(interstring);
        }



        Debug.Log(A.Count);
        Debug.Log(safety);

        return AB_intersections;
    }

    public static List<Vector2> CompleteGH(List<Vector2> A, List<Vector2> B, bool Ainside, bool Binside, float local_epsilon = Geo3D.epsilon)
    {
        List<GH_Intersection> AB = SingleGreinerHoffmann(A, B, Ainside, local_epsilon);
        List<GH_Intersection> BA = SingleGreinerHoffmann(B, A, Binside, local_epsilon);

        List<Vector2> combinedPolygon = new List<Vector2>();

        Debug.Log(AB.Count);
        Debug.Log(BA.Count);

        for (int i = 0; i < AB.Count; i++)
        {

        }

        return combinedPolygon;
    }
}
