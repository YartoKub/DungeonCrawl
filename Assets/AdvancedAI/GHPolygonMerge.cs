using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// Я запрещаю кому-либо использовать написанный мной код для обучения нейросетей. Это моя интеллектуальная собственность.
// I forbid anyone to use code, written by me, to train neural networks. It is my intellectual property.

// Кажется эта штука работает. С ней в целом проблем много быть не должно. 
public static class GHPolygonMerge
{
    // Списки A and B уже должны быть поделены друг на друга
    // A и B - корректные полигоны, без самопересечений и вершин-дубликатов
    // A и B содержат общие вершины, и полигоны А и B могут полностью совпадать
    // Цель: Найти все пересечения и все разница как отдельные полигоны
    const int inn_point = -1;
    const int out_point = -2;
    const int used_point = -3;

    /* Поиск разностей начинается с любой внутренней или наружней точке 
     * Запоминается сторона начальной точки и начальное направление. На пересечении проверяется следующая точка полигона A и предыдущая точка полигона B. 
     * При проходе через наружную/внутреннюю точку она закрашивается чтобы ее нельзя было выбрать повторно. Точки пересечения.
         o----------o                                     o----------o 
         |          |        A.o or B.i     B.i + A.i     |B.o or A.i|
   o-----+-------i  |         o-----+       +-------i     +-------i  |
   |  A  |#######|  |     ->  |Aonly|   +   |overlap| +           |  |
   o-----+-------i  |         o-----+       +-------i     +-------i  | i - inside
         |    B     |                                     |   Bonly  | + - cross 
         o----------o                                     o----------o o - outside
     */
    // Этот код ожидает что все пересечения заранее известны и находятся в списке intersections
    public static (List<Poly2D> overlap, List<Poly2D> onlyA, List<Poly2D> onlyB) CutPolyInt(List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)
    {
        Debug.Log("Not implemented");
        List<Poly2D> polyToReturn = new List<Poly2D>();
        if (intersections.Count == 0) return (polyToReturn, polyToReturn, polyToReturn);
        // Массивы с отметками стороны для поиска частей принадлежащий только A или только B  
        (int[] defAside, int[] defBside) = MarkPoints(V, A, B, Ap, Bp, intersections);

        // DEBUG DRAW
        //for (int i = 0; i < intersections.Count; i++) DebugUtilities.DebugDrawSquare(V[A[intersections[i].A]], Color.yellow, time:5f);
        for (int i = 0; i < defAside.Length; i++) {
            if (defAside[i] == out_point) DebugUtilities.DebugDrawSquare(V[A[i]], Color.red, time:5f);
            else if (defAside[i] == inn_point) DebugUtilities.DebugDrawSquare(V[A[i]], Color.blue, time:5f);
            else { DebugUtilities.DebugDrawSquare(V[A[i]], Color.yellow, time: 5f); }
        }
        for (int i = 0; i < defBside.Length; i++) {
            if (defBside[i] == out_point) DebugUtilities.DebugDrawSquare(V[B[i]], Color.red, time:5f);
            else if (defBside[i] == inn_point) DebugUtilities.DebugDrawSquare(V[B[i]], Color.blue, time:5f);
            else { DebugUtilities.DebugDrawSquare(V[B[i]], Color.yellow, time: 5f); }
        }// DEBUG DRAW
        // Массивы с отметками стороны для поиска пересечения двух полигонов
        int[] defAsideOverlap = new int[defAside.Length]; defAside.CopyTo(defAsideOverlap, 0);
        int[] defBsideOverlap = new int[defBside.Length]; defBside.CopyTo(defBsideOverlap, 0);
        int safety = 0;

        List<int> start_A;
        List<int> start_B;
        // Поиск только А. Только A состоит из A_outside, пересечений, и B_inside
        start_A = GetEntryPoints(defAside, out_point);
        start_B = GetEntryPoints(defBside, inn_point);
        string n = "A side"; for (int i = 0; i < defAside.Length; i++) n += defAside[i] + ", "; Debug.Log(n);
        n = "B side"; for (int i = 0; i < defBside.Length; i++) n += defBside[i] + ", "; Debug.Log(n);
        n = "start A"; for (int i = 0; i < start_A.Count; i++) n += start_A[i] + ", "; Debug.Log(n);
        n = "start B"; for (int i = 0; i < start_B.Count; i++) n += start_B[i] + ", "; Debug.Log(n);
        while (((start_A.Count + start_B.Count) > 0) && safety < 1) { safety += 1;
            bool AorB; int pos = -1;
            if (start_A.Count == 0) { AorB = false; pos = start_B.Last(); }
            else                    { AorB = true ; pos = start_A.Last(); }
            List<CH2D_P_Index> new_loop =  IsolateLoopInt(A, B, defAside, defBside, AorB, pos, 1, -1); // A - CCW; B - CW;
            Debug.Log("Final Point Count: " + new_loop.Count);
            for (int i = 0; i < new_loop.Count; i++)
            {
                int j = (i + 1) % new_loop.Count;
                DebugUtilities.DebugDrawLine(V[new_loop[i]], V[new_loop[j]], DebugUtilities.PickGradient(i, new_loop.Count, DebugUtilities.GradientOption.Rainbow_Red2Violet), 5f, 0.3f);
            }
        }


        return (polyToReturn, polyToReturn, polyToReturn);
    }

    private static List<int> GetEntryPoints(int[] Ainter, int side)
    {
        List<int> starts = new List<int>();
        for (int i = 0; i < Ainter.Length; i++)
            if (Ainter[i] == side) starts.Add(i);

        return starts;
    }

    // Чтобы эта штука работала нужно чтобы оба списка были однонаправленными. 
    private static List<CH2D_P_Index> IsolateLoopInt(List<CH2D_P_Index> Av, List<CH2D_P_Index> Bv, int[] Ainter, int[] Binter, bool AorB, int pos, int Adiff, int Bdiff)
    {
        bool curntAorB = AorB; int curr_A; int curr_B; // Одновременно отслеживаются позиции в A и в B. Движение по второму полигону чуть более приоритетно
        if (curntAorB)
        {
            curr_A = pos;
            curr_B = (Ainter[pos] < 0) ? -1 : Ainter[pos];
        } else
        {
            curr_A = (Binter[pos] < 0) ? -1 : Binter[pos];
            curr_B = pos;
        }
        Debug.Log(AorB + " " + curr_A + " " + curr_B);
        int safety = -1; bool isDone = false;

        List<CH2D_P_Index> newLoop = new List<CH2D_P_Index>();
        Debug.Log("Loop not finished, unimplemented");
        while (safety < 10 && !isDone) { safety ++;
            newLoop.Add(curr_A == -1 ? Bv[curr_B] : Av[curr_A]);
            (int nextA, int nextB) = LoopClosureStrategy_Difference(Ainter, Binter, curr_A, curr_B, Adiff, Bdiff);
            if (curr_A == pos && safety != 0) { Debug.Log("Все хорошо завершилось"); break; }
            if (nextA == -1 & nextB == -1) break;
            if (nextA == -1) nextA = (Binter[nextB] < 0) ? -1 : Binter[nextB]; // Если находится на пересечении то берем значение пересчениея
            if (nextB == -1) nextB = (Ainter[nextA] < 0) ? -1 : Ainter[nextA]; // Если находится на пересечении то берем значение пересчениея
            Debug.Log(nextA + " " + nextB);
            curr_A = nextA; curr_B = nextB;
        }
        return newLoop;
    }
     
    private static (int, int) LoopClosureStrategy_Difference(int[] Ainter, int[] Binter, int curr_A, int curr_B, int Adiff = 1, int Bdiff = -1)
    {   // Стратегия для поиска разностей между фигурами. Находит только A - B. Можно поменять местами для получения B - A. 
        // Находясь на пересечении есть 4 конфигурации соседей: Невозможно(Ao+Bi), A(Ao+Cross), B(Ai+Cross), A(Cross+Bo), B(Cross+Bi), Совпадение(Cross+Cross)
        int next_A = -1; int next_A_state = used_point;
        int next_B = -1; int next_B_state = used_point;
        if (curr_A >= 0) { next_A = (curr_A + Adiff + Ainter.Length) % Ainter.Length; next_A_state = Ainter[next_A]; }
        if (curr_B >= 0) { next_B = (curr_B + Bdiff + Binter.Length) % Binter.Length; next_B_state = Binter[next_B]; }
        
        Debug.Log(next_A + " " + next_B + " " + next_A_state + " " + next_B_state);
        if (next_A_state == used_point && next_B_state != used_point) { Binter[curr_B] = -1; return (-1, next_B); } // Иду по границе B, собираю B outside и жду cross. Записывая эту точку как used_point.
        if (next_B_state == used_point && next_A_state != used_point) { Ainter[curr_A] = -1; return (next_A, -1); } // Иду по границе А, собираю A outside и жду cross. Записывая эту точку как used_point.
        // Стою на пересечении
        if (next_A_state == out_point && next_B_state == inn_point) throw new System.Exception("Кандидаты A outside и B inside. Такая конфигурация невозможна, явно с полигонами какая-то хрень.");
        if (next_A_state >= 0 & next_B_state >= 0) { return (-1, next_B);} // У точки B в этом случае выше приоритет.
        if (next_A_state == out_point) { return (next_A, -1); }
        if (next_B_state == inn_point) { return (-1, next_B); }

        if (next_A_state == inn_point & next_B_state >= 0) { return (-1, next_B); }
        if (next_B_state == out_point & next_A_state >= 0) { return (next_A, -1); }
        
        Debug.Log("хрень какая-то произошла");
        return (-1, -1);
    }
    
    private struct AllowedSides
    {
        bool Ainside; bool Aoutside; bool Binside; bool Boutside;
        public AllowedSides(bool Ainside, bool Aoutside, bool Binside, bool Boutside ) { this.Ainside = Ainside; this.Aoutside = Aoutside; this.Binside = Binside; this.Boutside = Boutside; } 
    }

    private static (int[] Aside, int[] Bside) MarkPoints(List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)
    {
        int[] Ainter = new int[A.Count]; // default value - 0
        int[] Binter = new int[B.Count];
        for (int i = 0; i < Ainter.Length; i++) Ainter[i] = used_point;
        for (int i = 0; i < Binter.Length; i++) Binter[i] = used_point;

        for (int i = 0; i < intersections.Count; i++)
        {
            Ainter[intersections[i].A] = intersections[i].B;
            Binter[intersections[i].B] = intersections[i].A;
        }
        for (int i = 0; i < Ainter.Length; i++)
        {
            if (Ainter[i] != used_point) continue;
            Ainter[i] = Poly2DToolbox.IsPointInsidePolygon(V[A[i]], Bp) ? inn_point : out_point;
        }
        for (int i = 0; i < Binter.Length; i++)
        {
            if (Binter[i] != used_point) continue;
            Binter[i] = Poly2DToolbox.IsPointInsidePolygon(V[B[i]], Ap) ? inn_point : out_point;
        }

        return (Ainter, Binter);
    }


    // Ainside/Binside - what side to keep, the one outside other polygon, or one isnide
    // True/True --- intersection
    // False/False --- union
    // True/false --- A - B
    // False/True --- B - A
    // Желательно скармливать ему только полигоны без дырок
    public static HierarchicalPoly2D CompleteGH(List<Vector2> A, List<Vector2> B, bool Ainside, bool Binside, float local_epsilon = Geo3D.epsilon)
    {
        // Разделение полигонов. Они связаны пересечениямм
        SubdividePolygons(A, B, local_epsilon, out List<Pair> intersections);
        HierarchicalPoly2D polyToReturn = new HierarchicalPoly2D();
        if (intersections.Count == 0)
        {
            return polyToReturn;
        }

        int[] Ainter = new int[A.Count]; // default value - 0
        int[] Binter = new int[B.Count]; // default value - 0
        for (int i = 0; i < Ainter.Length; i++) Ainter[i] = used_point;
        for (int i = 0; i < Binter.Length; i++) Binter[i] = used_point;

        // -1 - true; // -2 - false
        for (int i = 0; i < intersections.Count; i++)
        {
            Ainter[intersections[i].A] = intersections[i].B;
            Binter[intersections[i].B] = intersections[i].A;
        }

        bool A0Inside = Poly2DToolbox.IsPointInsidePolygon(A[0], B);
        bool B0Inside = Poly2DToolbox.IsPointInsidePolygon(B[0], A);

        //Debug.Log(A0Inside.ToString() + " " + B0Inside.ToString());
        for (int i = 0; i < Ainter.Length; i++)
        {
            if (Ainter[i] >= 0) { A0Inside = !A0Inside; continue; }
            Ainter[i] = (Ainside == A0Inside) ? -1 : used_point;
        }
        for (int i = 0; i < Binter.Length; i++)
        {
            if (Binter[i] >= 0) { B0Inside = !B0Inside; continue; }
            Binter[i] = (Binside == B0Inside) ? -1 : used_point;
        }

        // Marking intersections as exiting and entering polygon A
        for (int i = 0; i < intersections.Count; i++)
        {
            intersections[i] = new Pair(intersections[i].A, intersections[i].B, A0Inside == (i % 2 == 0));
        }
        /*
        string intersectionCount = "InterCOunt " + intersections.Count.ToString() + "\n";
        for (int i = 0; i < intersections.Count; i++)
        {
            Pair p = intersections[i];
            intersectionCount += "(" + p.A + " " + p.B + " " + p.doesExit + ") " + A[p.A] + "\n";
        }
        Debug.Log(intersectionCount);*/
        /*
        string Asting = "";
        for (int i = 0; i < Ainter.Length; i++) { Asting += Ainter[i].ToString() + " "; }
        Debug.Log(Asting);

        string Bsting = "";
        for (int i = 0; i < Binter.Length; i++) { Bsting += Binter[i].ToString() + " "; }
        Debug.Log(Bsting);*/

        int safety = 0;
        while ((safety < 25) && intersections.Count > 0)
        {
            if (Ainter[intersections[0].A] == used_point | Binter[intersections[0].B] == used_point)
            {
                intersections.RemoveAt(0);
                continue;
            }
            List<Vector2> newLoop = IsolateLoop(A, B, Ainter, Binter, intersections, Ainside, Binside);
            
            if (newLoop.Count >= 3)
            {
                polyToReturn.polygons.Add(new Poly2D(newLoop));
            }
        }

        /*
        Debug.Log(polyToReturn.polygons.Count);

        Asting = "";
        for (int i = 0; i < Ainter.Length; i++) { Asting += Ainter[i].ToString() + " "; }
        Debug.Log(Asting);

        Bsting = "";
        for (int i = 0; i < Binter.Length; i++) { Bsting += Binter[i].ToString() + " "; }
        Debug.Log(Bsting);*/

        return polyToReturn;
    }


    // Pairs that are a part of a loop are popped
    private static List<Vector2> IsolateLoop(List<Vector2> A, List<Vector2> B, int[] Ainter, int[] Binter, List<Pair> pairs, bool Adir, bool Bdir)
    {
        // false - по часовой / true - против часовой
        // Снаружи - против часовой / Внутри - по часовой
        //Debug.Log("ISOLATE LOOP " + pairs[0].A);
        if (Ainter[pairs[0].A] == used_point)
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
            //string debugString = "";
            if (curntAorB)
            {
                currentLinkArray = Binter;
                currentList = B;
                current_step = Bdiff;
                //debugString += "B " + Bdiff.ToString() + " ";
            }
            else
            {
                currentLinkArray = Ainter;
                currentList = A;
                current_step = Adiff;
                //debugString += "A " + Adiff.ToString() + " ";
            }

            switch (currentLinkArray[curntPoint])
            {
                case -2:
                    isDone = true;
                    break;
                case -1:
                    //Debug.Log(debugString + " " + curntPoint.ToString() + " (" + currentLinkArray[curntPoint].ToString() + " -> -2)");
                    newLoop.Add(currentList[curntPoint]);
                    currentLinkArray[curntPoint] = used_point;
                    next_Point = Poly2DToolbox.wrapAround(curntPoint, current_step, currentLinkArray.Length);
                    break;
                default:
                    if (needToJump)
                    {
                        next_Point = currentLinkArray[curntPoint];
                        //Debug.Log("Jump " + (curntAorB ? "B" : "A") + curntPoint + " " + (!curntAorB ? "B" : "A") + next_Point + " (" + currentLinkArray[curntPoint].ToString() + " -> -2)");
                        //newLoop.Add(currentList[curntPoint]); // Это приводит к дубликатам вершин
                        curntAorB = !curntAorB;
                        currentLinkArray[curntPoint] = used_point;
                        needToJump = false;
                        continue;
                    }
                    //Debug.Log(debugString + " " + curntPoint.ToString() + " (" + currentLinkArray[curntPoint].ToString() + " -> -2)");
                    newLoop.Add(currentList[curntPoint]);
                    currentLinkArray[curntPoint] = used_point;
                    next_Point = Poly2DToolbox. wrapAround(curntPoint, current_step, currentLinkArray.Length);
                    needToJump = true;
                    break;
            }
        }
        return newLoop;
    }



    private static void SubdividePolygons(List<Vector2> A, List<Vector2> B, float local_epsilon, out List<Pair> intersections)
    {
        int safety = 0; int safety_limit = 1000; // Мне не нравится когда зависает юнити
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

                //Debug.Log("insertion phase");
                A.Insert(a1 + 1, intersection);
                B.Insert(b1 + 1, intersection);

                a1 = a1 - 1;// Программа делает шаг назад и заново начинает проверки
                break; // Т.К. Произошло разделение грани a1a2 на a1X / Xa2. Каждая из этих граней может иметь свои пересечения с полигоном B 
                
            }
        }
        //Debug.Log(safety);
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