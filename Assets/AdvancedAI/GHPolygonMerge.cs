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
    const int no_point = -4;
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
    public static (List<CH2D_Polygon> overlap, List<CH2D_Polygon> onlyA, List<CH2D_Polygon> onlyB) CutPolyInt(List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)
    {
        Debug.Log("Not implemented");
        List<CH2D_Polygon> polyToReturn = new List<CH2D_Polygon>();
        if (intersections.Count == 0) return (polyToReturn, polyToReturn, polyToReturn);
        // Массивы с отметками стороны для поиска частей принадлежащий только A или только B  
        (int[] defAside, int[] defBside) = MarkPoints(V, A, B, Ap, Bp, intersections);
        int[] Aint_tmp; int[] Bint_tmp;
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
        Aint_tmp = new int[defAside.Length]; defAside.CopyTo(Aint_tmp, 0);
        Bint_tmp = new int[defBside.Length]; defBside.CopyTo(Bint_tmp, 0);
        
        Debug.Log("<color='red'>==== A ONLY ====</color>");
        List<CH2D_Polygon> Aonly = IsolateLoops(A, B, Aint_tmp, Bint_tmp, out_point, inn_point, 1, -1, BooleanOperation.Aonly);
        for (int i = 0; i < Aonly.Count; i++)
        {
            int a_v_count = Aonly[i].vertices.Count;
            for (int x= 0; x < a_v_count; x++)
            {
                int y = (x + 1) % a_v_count;
                DebugUtilities.DebugDrawLine(V[Aonly[i].vertices[x]], V[Aonly[i].vertices[y]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), 4f, 0.3f);
            }
        }
        /*
        Debug.Log("<color='red'>==== B ONLY ====</color>");
        Aint_tmp = new int[defAside.Length]; defAside.CopyTo(Aint_tmp, 0);
        Bint_tmp = new int[defBside.Length]; defBside.CopyTo(Bint_tmp, 0);
        List<CH2D_Polygon> Bonly = IsolateLoops(A, B, Aint_tmp, Bint_tmp, inn_point, out_point, -1, 1, BooleanOperation.Bonly);
        for (int i = 0; i < Bonly.Count; i++)
        {
            int a_v_count = Bonly[i].vertices.Count;
            for (int x = 0; x < a_v_count; x++)
            {
                int y = (x + 1) % a_v_count;
                DebugUtilities.DebugDrawLine(V[Bonly[i].vertices[x]], V[Bonly[i].vertices[y]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), 8f, 0.3f);
            }
        }
        */
        return (polyToReturn, null, null);
    }
    private enum poly {A, B};
    private enum BooleanOperation { Aonly, Bonly, Union, Diffr };
    private static List<CH2D_Polygon> IsolateLoops(List<CH2D_P_Index> A, List<CH2D_P_Index> B, int[] Ainter, int[] Binter, int A_preference, int B_preference, int A_diff, int B_diff, BooleanOperation operation)
    {
        Debug.Log("Пожалуйста подними safety до какого-нибудь приличного числа!");
        List<CH2D_Polygon> polygons = new List<CH2D_Polygon>();
        (List<int> start_A, List<int> start_B) = GetEntryPoints(Ainter, Binter, operation);
        int safety = 0;
        //DebugStepState(Ainter, Binter, start_A, start_B, A, B);
        while (((start_A.Count + start_B.Count) > 0) && safety < 5)
        {
            safety += 1;
            (poly AorB, int pos) = PickStart(start_A, start_B, Ainter, Binter, A_preference, B_preference);
            DebugStepState(Ainter, Binter, start_A, start_B, A, B);
            if (pos == -1) { Debug.Log("No start location is valid!"); break; }
            Debug.Log(AorB + " " + pos);

            (bool good_loop, List<CH2D_P_Index> new_loop) = IsolateLoop(A, B, Ainter, Binter, AorB, pos, A_diff, B_diff, A_preference, B_preference, operation); // A - moving CCW; B - moving CW;
            string n = "Final Point Count: "; for (int i = 0; i < new_loop.Count; i++) n += new_loop[i] + ", "; Debug.Log(n);
            if (good_loop) polygons.Add(new CH2D_Polygon(new_loop));
        }

        return polygons;
    }
    private static (poly, int) PickStart(List<int> start_A, List<int> start_B, int[] Ainter, int[] Binter, int A_preference, int B_preference)
    {   // Просто выбирает A или B который не был использован.
        int start_p;
        for (int i = start_A.Count - 1; i >= 0; i--)
            if (Ainter[start_A[i]] == A_preference) { start_p = start_A[i]; start_A.RemoveAt(i); return (poly.A, start_p); }
            else start_A.RemoveAt(i);
        for (int i = start_B.Count - 1; i >= 0; i--)
            if (Binter[start_B[i]] == B_preference) { start_p = start_B[i]; start_B.RemoveAt(i); return (poly.B, start_p); }
            else start_B.RemoveAt(i);
        return (poly.A, -1);
    }
    private static (List<int>, List<int>) GetEntryPoints(int[] Ainter, int[] Binter, BooleanOperation operation)
    {
        List<int> a; List<int> b;   
        switch (operation) {
            case BooleanOperation.Aonly:// Нужно найти наружние А и внутренние В
                a = new List<int>(); for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == out_point) a.Add(i);
                b = new List<int>(); for (int i = 0; i < Binter.Length; i++) if (Binter[i] == inn_point) b.Add(i);
                return (a, b);
            case BooleanOperation.Bonly:// Нужно найти наружние В и внутренние А
                a = new List<int>(); for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == inn_point) a.Add(i);
                b = new List<int>(); for (int i = 0; i < Binter.Length; i++) if (Binter[i] == out_point) b.Add(i);
                return (a, b);
            case BooleanOperation.Union:// Нужно найти все пересечения для поиска возможного объединения
                a = new List<int>(); for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
                return (a, null);
            case BooleanOperation.Diffr:// Нужно найти все пересечения для поиска наложений. Так как может быть 100% наложение, ищутся именно пересечения
                a = new List<int>(); for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
                return (a, null);
            default:
                return (null, null);
        }
    }

    private static (bool good_loop, List<CH2D_P_Index> points) IsolateLoop(List<CH2D_P_Index> Av, List<CH2D_P_Index> Bv, int[] Ainter, int[] Binter, poly cp_side, int cp_pos, int A_diff, int B_diff, int A_preference, int B_preference, BooleanOperation operation)
    {
        List<CH2D_P_Index> points = new List<CH2D_P_Index>(); poly AorB = cp_side;

        int curr_A = AorB == poly.A ? cp_pos : -1; 
        int curr_B = AorB == poly.B ? cp_pos : -1;
        Debug.Log("Isolate Loop Start: " + AorB + " A: " + curr_A + " B: " + curr_B);
        int safety = -1; bool isDone = false;
        bool goodLoop = false;
        while (safety < 10 && !isDone)
        {
            Debug.Log(curr_A + " " + curr_B);
            
            points.Add(curr_A == -1 ? Bv[curr_B] : Av[curr_A]);
            safety++;
            int next_A = (curr_A >= 0) ? (curr_A + A_diff + Ainter.Length) % Ainter.Length : -1;
            int next_B = (curr_B >= 0) ? (curr_B + B_diff + Binter.Length) % Binter.Length : -1;
            //Debug.Log(next_A + " " + next_B + " " + cp_pos);
            if (cp_side == poly.A && next_A == cp_pos) { Debug.Log("Next A is finish point, leaving loop"); goodLoop = true; break; }
            if (cp_side == poly.B && next_B == cp_pos) { Debug.Log("Next B is finish point, leaving loop"); goodLoop = true; break; }
            if (curr_A >= 0 && Ainter[curr_A] < 0) Ainter[curr_A] = used_point;
            if (curr_B >= 0 && Binter[curr_B] < 0) Binter[curr_B] = used_point;
            switch (operation) {
                case BooleanOperation.Aonly: (curr_A, curr_B) = LCS_DifferenceAB(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
                case BooleanOperation.Bonly: (curr_A, curr_B) = LCS_DifferenceBA(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
                case BooleanOperation.Union: (curr_A, curr_B) = LCS_DifferenceAB(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
                case BooleanOperation.Diffr: (curr_A, curr_B) = LCS_DifferenceAB(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
            }
            

            Debug.Log(curr_A + " " + curr_B);
            curr_B = (curr_A >= 0 && Ainter[curr_A] >= 0) ? Ainter[curr_A] : curr_B;
            curr_A = (curr_B >= 0 && Binter[curr_B] >= 0) ? Binter[curr_B] : curr_A;
            if (curr_A == -1 && curr_B == -1) { Debug.Log("Bad ending"); break; }
            
        }
        DebugUtilities.DebugList(points);

        return (goodLoop, points);
    }

    private static (int, int) LCS_DifferenceAB(int[] Ainter, int[] Binter, int curr_A, int curr_B, int next_A, int next_B, bool startOnly)
    {   // A out + B inn + intersection
        int next_A_state = next_A != -1 ? Ainter[next_A] : no_point;
        int next_B_state = next_B != -1 ? Binter[next_B] : no_point;
        Debug.Log("A: c" + curr_A + " n" + next_A + " " + intDecider(next_A_state) + " | B: c" + curr_B + " n" + next_B + " " + intDecider(next_B_state));
        if (next_A_state == out_point && next_B_state == inn_point) throw new System.Exception("Кандидаты A outside и B inside. Такая конфигурация невозможна, явно с полигонами какая-то хрень.");

        if (next_A_state == no_point) return (-1, next_B);
        if (next_B_state == no_point) return (next_A, -1);

        if (next_A_state >= 0 & next_B_state >= 0) { return (-1, next_B); } // У точки B в этом случае выше приоритет, выбор точки B создает более маленькие полигоны
        if (next_A_state == out_point) { return (next_A, -1); }
        if (next_B_state == inn_point) { return (-1, next_B); }
        
        // Тут может хуйня происходить
        if (next_A_state == inn_point & next_B_state >= 0) { return (-1, next_B); }
        if (next_B_state == out_point & next_A_state >= 0) { return (next_A, -1); }
        return (-1, -1);
    }
    private static (int, int) LCS_DifferenceBA(int[] Ainter, int[] Binter, int curr_A, int curr_B, int next_A, int next_B, bool startOnly)
    {   // A inn + B out + intersection
        int next_A_state = next_A != -1 ? Ainter[next_A] : no_point;
        int next_B_state = next_B != -1 ? Binter[next_B] : no_point;
        Debug.Log("A: c" + curr_A + " n" + next_A + " " + intDecider(next_A_state) + " | B: c" + curr_B + " n" + next_B + " " + intDecider(next_B_state));
        if (next_A_state == out_point && next_B_state == inn_point) throw new System.Exception("Кандидаты A outside и B inside. Такая конфигурация невозможна, явно с полигонами какая-то хрень.");

        if (next_A_state == no_point) return (-1, next_B);
        if (next_B_state == no_point) return (next_A, -1);

        if (next_A_state >= 0 & next_B_state >= 0) { return (next_A, -1); } // У точки B в этом случае выше приоритет, выбор точки B создает более маленькие полигоны
        if (next_A_state == inn_point) { return (next_A, -1); }
        if (next_B_state == out_point) { return (-1, next_B); }

        // Тут может хуйня происходить
        if (next_A_state == out_point & next_B_state >= 0) { return (-1, next_B); }
        if (next_B_state == inn_point & next_A_state >= 0) { return (next_A, -1); }
        return (-1, -1);
    }

    private static void DebugStepState(int[] defAside, int[] defBside, List<int> start_A, List<int> start_B, List<CH2D_P_Index> A, List<CH2D_P_Index> B)
    {
        string n = "A side: "; for (int i = 0; i < defAside.Length; i++) n += intDecider(defAside[i]) + ", "; Debug.Log(n);
        n = "B side: "; for (int i = 0; i < defBside.Length; i++) n += intDecider(defBside[i]) + ", "; Debug.Log(n);
        n = "start A c:" + start_A.Count + ": "; for (int i = 0; i < start_A.Count; i++) n += "(" + start_A[i] + " " + A[start_A[i]] + "), "; Debug.Log(n);
        n = "start B c:" + start_B.Count + ": "; for (int i = 0; i < start_B.Count; i++) n += "(" + start_B[i] + " " + B[start_B[i]] + "), "; Debug.Log(n);
        
    }
    private static string intDecider(int value)
    {
        if (value == inn_point ) return "inn";
        if (value == out_point ) return "out";
        if (value == used_point) return "usd";
        if (value == no_point  ) return "non";
        return value.ToString();
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