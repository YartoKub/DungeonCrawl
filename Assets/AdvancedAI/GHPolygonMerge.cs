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

    // Ainside/Binside - what side to keep, the one outside other polygon, or one isnide
    // True/True --- intersection
    // False/False --- union
    // True/false --- A - B
    // False/True --- B - A
    // Это односторонний грейнер хофф. Делится только один треугольник, и только одна часть его сторон возвращается
    // Думаю можно сделать одновременную проверку AB и BA, но это кажется сложным, ведь придется иногда делать шаги назад или вперед.
    // Еще читаемость кода упадет, а я не выдержу макароны распутывать

    public static List<Vector2> CompleteGH(List<Vector2> A, List<Vector2> B, bool Ainside, bool Binside, float local_epsilon = Geo3D.epsilon)
    {
        //List<GH_Intersection> AB = SingleGreinerHoffmann(A, B, Ainside, local_epsilon);
        //List<GH_Intersection> BA = SingleGreinerHoffmann(B, A, Binside, local_epsilon);
        // Разделение полигонов. Они связаны пересечениямм
        SubdividePolygons(A, B, local_epsilon, out List<Pair> intersections);

        int[] Ainter = new int[A.Count]; // default value - 0
        int[] Binter = new int[B.Count]; // default value - 0
        for (int i = 0; i < Ainter.Length; i++) Ainter[i] = -2;
        for (int i = 0; i < Binter.Length; i++) Binter[i] = -2;

        // -1 - true; // -2 - false
        for (int i = 0; i < intersections.Count; i++)
        {
            Ainter[intersections[i].A] = intersections[i].B;
            Binter[intersections[i].B] = intersections[i].A;
        }

        bool B0Inside = Poly2DToolbox.IsPointInsidePolygon(B[0], A);
        bool A0Inside = Poly2DToolbox.IsPointInsidePolygon(A[0], B);

        for (int i = 0; i < Ainter.Length; i++)
        {
            if (Ainter[i] >= 0) { A0Inside = !A0Inside; continue; }
            Ainter[i] = (Ainside == A0Inside) ? -1 : -2;
        }
        for (int i = 0; i < Binter.Length; i++)
        {
            if (Binter[i] >= 0) { B0Inside = !B0Inside; continue; }
            Binter[i] = (Binside == B0Inside) ? -1 : -2;
        }

        // Marking intersections as exiting and entering polygon A
        for (int i = 0; i < intersections.Count; i++)
        {
            intersections[i] = new Pair(intersections[i].A, intersections[i].B, A0Inside == (i % 2 == 0));
        }
        string intersectionCount = "InterCOunt " + intersections.Count.ToString() + "\n";
        for (int i = 0; i < intersections.Count; i++)
        {
            Pair p = intersections[i];
            intersectionCount += "(" + p.A + " " + p.B + " " + p.doesExit + ") " + A[p.A] + "\n";
        }
        Debug.Log(intersectionCount);

        // Picking Loops:
        string Asting = "";
        for (int i = 0; i < Ainter.Length; i++) { Asting += Ainter[i].ToString() + " "; }
        Debug.Log(Asting);

        string Bsting = "";
        for (int i = 0; i < Binter.Length; i++) { Bsting += Binter[i].ToString() + " "; }
        Debug.Log(Bsting);

        List<Vector2> toReturn = IsolateLoop(A, B, Ainter, Binter, intersections, Ainside, Binside);


        Asting = "";
        for (int i = 0; i < Ainter.Length; i++) { Asting += Ainter[i].ToString() + " "; }
        Debug.Log(Asting);

        Bsting = "";
        for (int i = 0; i < Binter.Length; i++) { Bsting += Binter[i].ToString() + " "; }
        Debug.Log(Bsting);

        return toReturn;
    }


    // Pairs that are a part of a loop are popped
    private static List<Vector2> IsolateLoop(List<Vector2> A, List<Vector2> B, int[] Ainter, int[] Binter, List<Pair> pairs, bool Adir, bool Bdir)
    {
        // false - по часовой / true - против часовой
        // Снаружи - против часовой / Внутри - по часовой
        Debug.Log("ISOLATE LOOP " + pairs[0].A);
        if (Ainter[pairs[0].A] == -2)
        {// если текущий выброшен значит здесь уже прошелся алгоритм
            pairs.RemoveAt(0);
            return new List<Vector2>(0);
        }

        int startPoint = pairs[0].A; bool startAorB = false;
        if (!pairs[0].doesExit) // If A enters B, then start at B
        {
            startPoint = pairs[0].B; startAorB = true;
        }

        int curntPoint = startPoint; bool curntAorB = startAorB;
        int next_Point = curntPoint; bool needToJump = false;
        int safety = 0; bool isDone = false;

        List<Vector2> newLoop = new List<Vector2>();

        int Adiff = Adir ? -1 : 1;
        int Bdiff = Bdir ? -1 : 1;

        int[] currentLinkArray;
        List<Vector2> currentList;
        int current_step;
        while (safety < 250 && !isDone)
        {
            safety += 1;

            curntPoint = next_Point;
            if (curntAorB)
            {
                currentLinkArray = Binter;
                currentList = B;
                current_step = Bdiff;
            }
            else
            {
                currentLinkArray = Ainter;
                currentList = A;
                current_step = Adiff;
            }
            switch (currentLinkArray[curntPoint])
            {
                case -2:
                    isDone = true;
                    break;
                case -1:
                    Debug.Log(curntPoint.ToString() + " (" + currentLinkArray[curntPoint].ToString() + " -> -2)");
                    newLoop.Add(currentList[curntPoint]);
                    currentLinkArray[curntPoint] = -2;
                    next_Point = wrapAround(curntPoint, current_step, currentLinkArray.Length);
                    break;
                default:
                    if (needToJump)
                    {
                        next_Point = currentLinkArray[curntPoint];
                        Debug.Log("Jump B" + curntPoint + " -> A" + next_Point + " (" + currentLinkArray[curntPoint].ToString() + " -> -2)");
                        curntAorB = false;
                        currentLinkArray[curntPoint] = -2;
                        needToJump = false;
                        continue;
                    }
                    Debug.Log(curntPoint.ToString() + " (" + currentLinkArray[curntPoint].ToString() + " -> -2)");
                    newLoop.Add(currentList[curntPoint]);
                    currentLinkArray[curntPoint] = -2;
                    next_Point = wrapAround(curntPoint, current_step, currentLinkArray.Length);
                    needToJump = true;
                    break;
            }
        }
        return newLoop;
    }

    private static int wrapAround(int curr, int diff, int max)
    {
        return (curr + diff + max) % max;
    }

    public struct Pair
    {
        public int A; public int B; public bool doesExit;
        public Pair(int A, int B, bool doesExit)
        {
            this.A = A; this.B = B; this.doesExit = doesExit;
        }

    }

    private static void SubdividePolygons(List<Vector2> A, List<Vector2> B, float local_epsilon, out List<Pair> intersections)
    {
        int safety = 0; int safety_limit = 500; // Мне не нравится когда зависает юнити
        for (int a1 = 0; a1 < A.Count; a1++)
        { safety += 1; if (safety > safety_limit) break;
            int a2 = (a1 + 1) % A.Count;
            for (int b1 = 0; b1 < B.Count; b1++)
            { safety += 1; if (safety > safety_limit) break;
                int b2 = (b1 + 1) % B.Count;

                Vector2 intersection;
                bool doesIntersect = Poly2DToolbox.LineLineIntersection(A[a1], A[a2], B[b1], B[b2], out intersection);

                // Прежде чем добавить точку она сравнивается со следующей и предыдущей, чтобы избежать дубликатов.
                //if (doesIntersect) Debug.LogFormat("{0}, A{1}, A{2}, B{3}, B{4}", intersection, A[a1], A[a2], B[b1], B[b2]);
                if (!doesIntersect) continue;
                if (Poly2DToolbox.PointSimilarity(A[a1], intersection, local_epsilon)) continue;
                if (Poly2DToolbox.PointSimilarity(A[a2], intersection, local_epsilon)) continue;
                if (Poly2DToolbox.PointSimilarity(B[b1], intersection, local_epsilon)) continue;
                if (Poly2DToolbox.PointSimilarity(B[b2], intersection, local_epsilon)) continue;

                Debug.Log("insertion phase");
                A.Insert(a1 + 1, intersection);
                B.Insert(b1 + 1, intersection);


                a1 = a1 - 1;// Программа делает шаг назад и заново начинает проверки
                break; // Т.К. Произошло разделение грани a1a2 на a1X / Xa2. Каждая из этих граней может иметь свои пересечения с полигоном B 
                
            }
        }
        intersections = new List<Pair>();

        for (int a = 0; a < A.Count; a++)
        {
            for (int b = 0; b < B.Count; b++)
            { // Поиск одинаковых точек в двух полигонах
                //Debug.Log(A[a].ToString() + " " + B[b].ToString());
                if (Poly2DToolbox.PointSimilarity(A[a], B[b], local_epsilon)) 
                {
                    intersections.Add(new Pair(a, b, false));
                }
            }
        }
    }
}

/* LEGACY ISOLATE LOOP
    private static List<Vector2> IsolateLoop(List<Vector2> A, List<Vector2> B, int[] Ainter, int[] Binter, List<Pair> pairs, bool Adir, bool Bdir)
    {
        // false - по часовой / true - против часовой
        // Снаружи - против часовой / Внутри - по часовой
        Debug.Log("ISOLATE LOOP " + pairs[0].A);
        if (Ainter[pairs[0].A] == -2)
        {// если текущий выброшен значит здесь уже прошелся алгоритм
            pairs.RemoveAt(0);
            return new List<Vector2>(0);
        }

        int startPoint = pairs[0].A; bool startAorB = false;
        if (!pairs[0].doesExit) // If A enters B, then start at B
        {
            startPoint = pairs[0].B; startAorB = true;
        }

        int curntPoint = startPoint; bool curntAorB = startAorB;
        int next_Point = curntPoint; bool needToJump = false;
        int safety = 0; bool isDone = false;

        List<Vector2> newLoop = new List<Vector2>();

        int Adiff = Adir ? -1 : 1;
        int Bdiff = Bdir ? -1 : 1;
        
        while (safety < 50 && !isDone)
        {   safety += 1;

            curntPoint = next_Point;
            if (curntAorB)
            {   // BBBBBBBBBBBBBB
                switch (Binter[curntPoint])
                {
                    case -2:
                        isDone = true;
                        break;
                    case -1:
                        Debug.Log(curntPoint.ToString() + " (" + Binter[curntPoint].ToString() + " -> -2)");
                        newLoop.Add(B[curntPoint]);
                        Binter[curntPoint] = -2;
                        next_Point = wrapAround(curntPoint, Bdiff, Binter.Length);
                        break;
                    default:
                        if (needToJump)
                        {
                            next_Point = Binter[curntPoint];
                            Debug.Log("Jump B" + curntPoint + " -> A" + next_Point + " (" + Binter[curntPoint].ToString() + " -> -2)");
                            curntAorB = false;
                            Binter[curntPoint] = -2;
                            needToJump = false;
                            continue;
                        }
                        Debug.Log(curntPoint.ToString() + " (" + Binter[curntPoint].ToString() + " -> -2)");
                        newLoop.Add(B[curntPoint]);
                        Binter[curntPoint] = -2;
                        next_Point = wrapAround(curntPoint, Bdiff, Binter.Length);
                        needToJump = true;
                        break;
                }
            } 
            else
            {   // AAAAAAAAAAAAAA
                switch (Ainter[curntPoint])
                {
                    case -2:
                        isDone = true;
                        break;
                    case -1:
                        Debug.Log(curntPoint.ToString() + " (" + Ainter[curntPoint].ToString() + " -> -2)");
                        newLoop.Add(A[curntPoint]);
                        Ainter[curntPoint] = -2;
                        next_Point = wrapAround(curntPoint, Adiff, Ainter.Length);
                        break;
                    default:
                        if (needToJump)
                        {
                            next_Point = Ainter[curntPoint];
                            Debug.Log("Jump A" + curntPoint + " -> B" + next_Point + " (" + Ainter[curntPoint].ToString() + " -> -2)");
                            curntAorB = true;
                            Ainter[curntPoint] = - 2;
                            needToJump = false;
                            continue;
                        }
                        Debug.Log(curntPoint.ToString() + " (" + Ainter[curntPoint].ToString() + " -> -2)");
                        newLoop.Add(A[curntPoint]);
                        Ainter[curntPoint] = -2;
                        next_Point = wrapAround(curntPoint, Adiff, Ainter.Length);
                        needToJump = true;
                        break;
                }
            }
        }
        return newLoop;
    }
 */