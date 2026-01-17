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
    private enum EdgeSide {None = 0, Inside, Outside, Inn_Colin, Out_Colin, Used}
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
        if (intersections.Count == 0) { Debug.Log("Нет пересечений междлу полигонами"); return (null, null, null); }

        (int[] Ainter, int[] Binter) = MarkPoints(V, A, B, Ap, Bp, intersections);
        (EdgeSide[] Aedges, EdgeSide[] Bedges) = MarkEdges(Ainter, Binter, A, B);
        
        // DEBUG DRAW
        //for (int i = 0; i < intersections.Count; i++) DebugUtilities.DebugDrawSquare(V[A[intersections[i].A]], Color.yellow, time:5f);
        for (int i = 0; i < Ainter.Length; i++)
        {
            if (Ainter[i] == out_point) DebugUtilities.DebugDrawSquare(V[A[i]], Color.red, time: 5f);
            else if (Ainter[i] == inn_point) DebugUtilities.DebugDrawSquare(V[A[i]], Color.blue, time: 5f);
            else if (Ainter[i] >= 0) { DebugUtilities.DebugDrawSquare(V[A[i]], Color.yellow, time: 5f); }
            else { DebugUtilities.DebugDrawSquare(V[A[i]], Color.white, time: 5f); }
        }
        
        for (int i = 0; i < Binter.Length; i++)
        {
            if (Binter[i] == out_point) DebugUtilities.DebugDrawSquare(V[B[i]], Color.red, time: 5f);
            else if (Binter[i] == inn_point) DebugUtilities.DebugDrawSquare(V[B[i]], Color.blue, time: 5f);
            else if (Binter[i] >= 0) { DebugUtilities.DebugDrawSquare(V[B[i]], Color.yellow, time: 5f); }
            else { DebugUtilities.DebugDrawSquare(V[B[i]], Color.white, time: 5f); } 
        }

        // DEBUG DRAW
        /*
        for (int i = 0; i < Aedges.Length; i++)
        {
            Color color = Color.black;
            switch (Aedges[i])
            {
                case EdgeSide.None: color = Color.white; break;
                case EdgeSide.Inside: color = Color.green; break;
                case EdgeSide.Outside: color = Color.red; break;
                case EdgeSide.Inn_Colin: color = Color.greenYellow; break;
                case EdgeSide.Out_Colin: color = Color.pink; break;
            }
            DebugUtilities.DebugDrawLine(V[A[i]], V[A[(i + 1) % Aedges.Length]], color, 3f);
        }
        
        for (int i = 0; i < Bedges.Length; i++)
        {
            Color color = Color.black;
            switch (Bedges[i])
            {
                case EdgeSide.None: color = Color.white; break;
                case EdgeSide.Inside: color = Color.green; break;
                case EdgeSide.Outside: color = Color.red; break;
                case EdgeSide.Inn_Colin: color = Color.greenYellow; break;
                case EdgeSide.Out_Colin: color = Color.pink; break;
            }
            DebugUtilities.DebugDrawLine(V[B[i]], V[B[(i + 1) % Bedges.Length]], color, 6f);
        }*/
        string ae = "A edges: "; for (int i = 0; i < Aedges.Length; i++) ae += Aedges[i] + " "; Debug.Log(ae);
        string be = "B edges: "; for (int i = 0; i < Bedges.Length; i++) be += Bedges[i] + " "; Debug.Log(be);
        
        Debug.Log("<color='red'>==== A ONLY ====</color>");
        List<CH2D_Polygon> Aonly = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Aonly);
        for (int i = 0; i < Aonly.Count; i++)
        {
            int a_v_count = Aonly[i].vertices.Count;
            for (int x = 0; x < a_v_count; x++)
            {
                int y = (x + 1) % a_v_count;
                DebugUtilities.DebugDrawLine(V[Aonly[i].vertices[x]], V[Aonly[i].vertices[y]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), 9f + 1f * i); //2f + 1f * i, 0.3f
            }
        }

        return (null, null, null);
    }

    private static List<CH2D_Polygon> IsolateLoops(List<CH2D_P_Index> A, List<CH2D_P_Index> B, int[] Ainter, int[] Binter, EdgeSide[] Aedge, EdgeSide[] Bedge, BooleanOperation operation)
    {
        Debug.Log("Пожалуйста подними safety до какого-нибудь приличного числа!");
        List<CH2D_Polygon> polygons = new List<CH2D_Polygon>();
        (List<int> start_A, List<int> start_B) = GetEntryPoints(Aedge, Bedge, operation);
        string n = "Aedge: ";   for (int i = 0; i < start_A.Count; i++) n += start_A[i] + " "; Debug.Log(n);
        n = "Bedge: ";          for (int i = 0; i < start_B.Count; i++) n += start_B[i] + " "; Debug.Log(n);
        int safety = 0;
        //DebugStepState(Ainter, Binter, start_A, start_B, A, B);

        while (((start_A.Count + start_B.Count) > 0) && safety < 4)
        {
            safety += 1;
            (poly AorB, int pos) = PickStart(start_A, start_B, Aedge, Bedge);
            n = "Aedge: "; for (int i = 0; i < start_A.Count; i++) n += start_A[i] + " "; Debug.Log(n);
            n = "Bedge: "; for (int i = 0; i < start_B.Count; i++) n += start_B[i] + " "; Debug.Log(n);
            //DebugStepState(Ainter, Binter, start_A, start_B, A, B);
            if (pos == -1) { Debug.Log("No start location is valid!"); break; }
            Debug.Log(AorB + " " + pos);

            (bool good_loop, List<CH2D_P_Index> new_loop) = IsolateLoop(A, B, Ainter, Binter, Aedge, Bedge, AorB, pos, operation); // A - moving CCW; B - moving CW;
            n = "<b><color=white>Final Point Count:</b></color> "; for (int i = 0; i < new_loop.Count; i++) n += new_loop[i] + ", "; Debug.Log(n);
            if (good_loop) polygons.Add(new CH2D_Polygon(new_loop));
        }

        return polygons;
    }

    private static (bool good_loop, List<CH2D_P_Index> points) IsolateLoop(List<CH2D_P_Index> Av, List<CH2D_P_Index> Bv, int[] Ainter, int[] Binter, EdgeSide[] Aedge, EdgeSide[] Bedge, poly startP, int s_pos, BooleanOperation operation)
    {
        List<CH2D_P_Index> points = new List<CH2D_P_Index>();// poly AorB = cp_side;

        int curr_a = startP == poly.A ? s_pos : -1; int curr_b = startP == poly.B ? s_pos : -1;
        int start_a = (curr_a == -1) ? (Binter[curr_b] >= 0 ? Binter[curr_b] : -1) : curr_a; 
        int start_b = (curr_b == -1) ? (Ainter[curr_a] >= 0 ? Ainter[curr_a] : -1) : curr_b;

        int A_diff = 1; int B_diff = 1; int A_off = 0; int B_off = 0;
        if (operation == BooleanOperation.Aonly) { A_diff =  1; B_diff = -1; A_off = 0; B_off = -1; }
        if (operation == BooleanOperation.Bonly) { A_diff = -1; B_diff =  1; A_off = -1; B_off = 0; }
        Debug.Log("Aoff " + A_off + " Boff " + B_off);
        Debug.Log("Isolate Loop Start: side: " + curr_a + " pos: " + curr_b);
        Debug.Log("Isolate Loop Start points: a " + start_a + " b: " + start_b);
        string n = "Aedge: "; for (int i = 0; i < Aedge.Length; i++) n += Aedge[i] + " "; Debug.Log(n);
        n = "Bedge: "; for (int i = 0; i < Bedge.Length; i++) n += Bedge[i] + " "; Debug.Log(n);
        int safety = -1;
        bool goodLoop = false;
        while (safety < 25)
        {
            points.Add(curr_a < 0 ? Bv[curr_b] : Av[curr_a]);
            safety++;
            Debug.Log("<b>STEP</b>, " + curr_a + " " + curr_b);
            // curr_a и curr_b, только одно значение может иметь функциональное значение. Это означает что перезапись здесь будет лишь одна
            int next_a_index = -1000; int next_b_index = -1000;
            EdgeSide curr_side = EdgeSide.None;
            EdgeSide next_a_side = EdgeSide.None; EdgeSide next_b_side = EdgeSide.None;

            if (curr_a >= 0)
            {// A only valid
                //curr_a = curr_a;
                next_a_index =           (curr_a + A_diff + Aedge.Length) % Aedge.Length;
                int next_a_index_value = (curr_a + A_off  + Aedge.Length) % Aedge.Length;
                next_a_side =               Aedge[next_a_index_value];
                Aedge[next_a_index_value] = EdgeSide.Used;

                curr_b = Ainter[curr_a];
                next_b_index =           (curr_b + B_diff + Bedge.Length) % Bedge.Length;
                int next_b_index_value = (curr_b + B_off  + Bedge.Length) % Bedge.Length;
                next_b_side = curr_b >= 0 ? Bedge[next_b_index_value] : EdgeSide.None;
            }
            else/*(curr_b >= 0)*/
            { // B only valid   
                next_b_index =           (curr_b + B_diff + Bedge.Length) % Bedge.Length;
                int next_b_index_value = (curr_b + B_off  + Bedge.Length) % Bedge.Length;
                next_b_side = Bedge[next_b_index_value];
                Bedge[next_b_index_value] = EdgeSide.Used;

                curr_a = Binter[curr_b];
                next_a_index =           (curr_a + A_diff + Aedge.Length) % Aedge.Length;
                int next_a_index_value = (curr_a + A_off  + Aedge.Length) % Aedge.Length;
                next_a_side = Aedge[next_a_index_value];
            }
            Debug.Log(curr_side + " " + next_a_index + " " + next_a_side + " " + next_b_index + " " + next_b_side);
            //Debug.Log(curr_side + " " + next_a_index + " " + next_a_side + " " + next_b_index + " " + next_b_side);
            n = "Aedge: "; for (int i = 0; i < Aedge.Length; i++) n += Aedge[i] + " "; Debug.Log(n);
            n = "Bedge: "; for (int i = 0; i < Bedge.Length; i++) n += Bedge[i] + " "; Debug.Log(n);

            if (next_b_index == start_b | next_a_index == start_a) { Debug.Log("<color=green>Next A is finish point, leaving loop</color>"); goodLoop = true; break; }

            poly next_p = LCS_Aonly(next_a_side, next_b_side);
            Debug.Log("<b> Return: </b> " + next_p);
            if (next_p == poly.None) { Debug.Log("<color=red>Bad ending</color>"); goodLoop = false; break; }
            curr_a = next_p == poly.A ? next_a_index : -1;
            curr_b = next_p == poly.B ? next_b_index : -1;

            
            //if (curr_A == start_b && curr_B == start_a) { Debug.Log("Current point is an intersection where loop began, SUCCESSFULL FINISH"); goodLoop = true; break; }


        }
        return (goodLoop, points);
    }
    /*
     * if (curr_a >= 0) {// A only valid
                next_a_index = (curr_a + A_diff + Ainter.Length) % Ainter.Length;
                next_b_index = Ainter[next_a_index];
                curr_side = Aedge[curr_a];
                next_a_side = Aedge[next_a_index];
                next_b_side = next_b_index >= 0 ? Bedge[(next_b_index + B_off + Bedge.Length) % Bedge.Length] : EdgeSide.None;
                Aedge[curr_a + A_off] = EdgeSide.Used;
            }
            else{ // B only valid   
                next_b_index = (curr_b + B_diff + Binter.Length) % Binter.Length;
                next_a_index = Binter[next_b_index];
                curr_side = Bedge[curr_b];
                next_b_side = Bedge[next_b_index];
                next_a_side = next_a_index >= 0 ? Aedge[next_a_index] : EdgeSide.None;
                Bedge[curr_b + B_off] = EdgeSide.Used;
            }
*/
    private static poly LCS_Aonly(EdgeSide nextA, EdgeSide nextB)
    {   // A out + B inn + intersection

        //if (nextA == EdgeSide.Outside && nextB == EdgeSide.Inside) throw new System.Exception("Кандидаты A outside и B inside. Такая конфигурация невозможна, явно с полигонами какая-то хрень.");

        //if (nextA == EdgeSide.Used) return poly.B;
        //if (nextB == EdgeSide.Used) return poly.A;

        //if (currSide == EdgeSide.Inside)
        {
            if (nextA == EdgeSide.Outside) return poly.A;
            if (nextB == EdgeSide.Inside ) return poly.B;
        }
        //if (currSide == EdgeSide.Outside)
        {
            if (nextB == EdgeSide.Inside ) return poly.B;
            if (nextA == EdgeSide.Outside) return poly.A;
        }

        return poly.None;
    }

    private static (poly, int) PickStart(List<int> start_A, List<int> start_B, EdgeSide[] Ainter, EdgeSide[] Binter)
    {   // Просто выбирает A или B который не был использован.
        int start_p;
        for (int i = start_A.Count - 1; i >= 0; i--)
            if (Ainter[start_A[i]] != EdgeSide.Used) { start_p = start_A[i]; start_A.RemoveAt(i); return (poly.A, start_p); }
            else start_A.RemoveAt(i);
        for (int i = start_B.Count - 1; i >= 0; i--)
            if (Binter[start_B[i]] != EdgeSide.Used) { start_p = start_B[i]; start_B.RemoveAt(i); return (poly.B, start_p); }
            else start_B.RemoveAt(i);
        return (poly.A, -1);
    }

    private static (List<int>, List<int>) GetEntryPoints(EdgeSide[] Ainter, EdgeSide[] Binter, BooleanOperation operation)
    {
        List<int> a = new List<int>(); List<int> b = new List<int>();
        switch (operation) {
            case BooleanOperation.Aonly:// Нужно найти наружние А и внутренние В
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Outside) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Inside ) b.Add(i);
                return (a, b);
            case BooleanOperation.Bonly:// Нужно найти наружние В и внутренние А
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Inside ) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Outside) b.Add(i);
                return (a, b);
            case BooleanOperation.Union:// Нужно найти все пересечения для поиска возможного объединения
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Outside) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Outside) b.Add(i);
                return (a, b);
            case BooleanOperation.Inter:// Нужно найти все пересечения для поиска наложений. Так как может быть 100% наложение, ищутся именно пересечения
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Inside ) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Inside ) b.Add(i);
                return (a, b);
            default: return (a, b);
        }
    }

    private static (EdgeSide[], EdgeSide[]) MarkEdges(int[] Ainter, int[] Binter, List<CH2D_P_Index> A, List<CH2D_P_Index> B)
    {
        EdgeSide[] Aedge = new EdgeSide[Ainter.Length]; EdgeSide[] Bedge = new EdgeSide[Binter.Length];
        // Вообще надо бы начать найдя наружную точку, но в дегенеративных случаях наружной точки может не быть
        // Edge - определяется как начальная + последующая точки. 
        // Итеративно решает к какой стороне принадлежит грань
        int safety = 0;
        while (safety < 10) { safety += 1;
            bool solved = true;
            for (int i = 0; i < Ainter.Length; i++)
            {   // Solving for A polygon
                if (Aedge[i] != EdgeSide.None) continue;
                int currAindex = i; int nextAindex = (i + 1) % Ainter.Length; int prevAindex = (i - 1 + Ainter.Length) % Ainter.Length;
                int currA = Ainter[currAindex]; int nextA = Ainter[nextAindex];
                int prevBindex = (currA - 1 + Binter.Length) % Binter.Length;
                int nextBindex = (currA + 1) % Binter.Length;
                
                EdgeSide a_edge = PointBasedSolution(currA, nextA, nextBindex, prevBindex);
                if (a_edge != EdgeSide.None) { Aedge[i] = a_edge; continue; }   // Проверка значений точек

                CH2D_P_Index prevBPindex = B[prevBindex]; // Прооверкка на коллинеарность
                CH2D_P_Index nextBPindex = B[nextBindex];
                CH2D_P_Index nextAPindex = A[nextAindex];
                if (nextAPindex == nextBPindex) { Aedge[i] = EdgeSide.Out_Colin; continue; }
                if (nextAPindex == prevBPindex) { Aedge[i] = EdgeSide.Inn_Colin; continue; }

                Aedge[i] = EdgeBasedSolution(Bedge[currA], Aedge[prevAindex], Aedge[nextAindex]);

                if (Aedge[i] == EdgeSide.None) solved = false;
                //if (Aedge[prevAindex] == EdgeSide.Inside) { Aedge[i] = EdgeSide.Outside; continue; }

                solved = false;
            }
            for (int i = 0; i < Binter.Length; i++)
            {   // Solving for B polygon
                int currBindex = i; int nextBindex = (i + 1) % Binter.Length; int prevBindex = (i - 1 + Binter.Length) % Binter.Length;
                int currB = Binter[currBindex]; int nextB = Binter[nextBindex];
                int prevAindex = (currB - 1 + Ainter.Length) % Ainter.Length;
                int nextAindex = (currB + 1) % Ainter.Length;
                
                EdgeSide b_edge = PointBasedSolution(currB, nextB, nextAindex, prevAindex);
                if (b_edge != EdgeSide.None) { Bedge[i] = b_edge; continue; }   // Проверка значений точек

                CH2D_P_Index prevAPindex = A[prevAindex]; // Прооверкка на коллинеарность
                CH2D_P_Index nextAPindex = A[nextAindex];
                CH2D_P_Index nextBPindex = B[nextBindex];
                if (nextBPindex == nextAPindex) { Bedge[i] = EdgeSide.Out_Colin; continue; }
                if (nextBPindex == prevAPindex) { Bedge[i] = EdgeSide.Inn_Colin; continue; }

                Bedge[i] = EdgeBasedSolution(Aedge[currB], Bedge[prevBindex], Bedge[nextBindex]);

                if (Bedge[i] == EdgeSide.None) solved = false;
            }
            if (solved) break;
        }

        return (Aedge, Bedge);

        EdgeSide PointBasedSolution(int currA, int nextA, int nextBindex, int prevBindex)
        {   // Уверен это можно оптимизировать до двух сравнений, но сейчас не важно
            if (currA == out_point) return EdgeSide.Outside;
            if (currA == inn_point) return EdgeSide.Inside ; 
            if (nextA == out_point) return EdgeSide.Outside;  
            if (nextA == inn_point) return EdgeSide.Inside ;
            return EdgeSide.None;
        }
        EdgeSide EdgeBasedSolution(EdgeSide Aedge, EdgeSide prevBedge, EdgeSide nextBedge)
        {
            if (Aedge == EdgeSide.Outside) return EdgeSide.Inside; 
            if (Aedge == EdgeSide.Inside) return EdgeSide.Outside;
            // Заполнение пропусков на основе предыдущего или следующего решения. Если этот шаг проводить до определения коллинеаров, то коллинеары будут записаны на некорректные значения
            if (prevBedge == EdgeSide.Outside) return EdgeSide.Inside;
            if (prevBedge == EdgeSide.Inside) return EdgeSide.Outside;
            if (nextBedge == EdgeSide.Outside) return EdgeSide.Inside;
            if (nextBedge == EdgeSide.Inside) return EdgeSide.Outside;
            return EdgeSide.None;
        }
    }
    public static (List<CH2D_Polygon> overlap, List<CH2D_Polygon> onlyA, List<CH2D_Polygon> onlyB) CutPolyInt(List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections, bool tmp)
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
        /*
        Debug.Log("<color='red'>==== A ONLY ====</color>");
        List<CH2D_Polygon> Aonly = IsolateLoops(A, B, Aint_tmp, Bint_tmp, out_point, inn_point, 1, -1, BooleanOperation.Aonly);
        for (int i = 0; i < Aonly.Count; i++)
        {
            int a_v_count = Aonly[i].vertices.Count;
            for (int x= 0; x < a_v_count; x++)
            {
                int y = (x + 1) % a_v_count;
                DebugUtilities.DebugDrawLine(V[Aonly[i].vertices[x]], V[Aonly[i].vertices[y]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), 2f + 1f * i, 0.3f);
            }
        }
        
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
        }*/
        
        return (polyToReturn, null, null);
    }
    private enum poly {A, B, None};
    private enum BooleanOperation { Aonly, Bonly, Union, Inter };
    private static List<CH2D_Polygon> IsolateLoops(List<CH2D_P_Index> A, List<CH2D_P_Index> B, int[] Ainter, int[] Binter, int A_preference, int B_preference, int A_diff, int B_diff, BooleanOperation operation)
    {
        Debug.Log("Пожалуйста подними safety до какого-нибудь приличного числа!");
        List<CH2D_Polygon> polygons = new List<CH2D_Polygon>();
        (List<int> start_A, List<int> start_B) = GetEntryPoints(Ainter, Binter, operation);
        int safety = 0;
        //DebugStepState(Ainter, Binter, start_A, start_B, A, B);
        while (((start_A.Count + start_B.Count) > 0) && safety < 25)
        {
            safety += 1;
            (poly AorB, int pos) = PickStart(start_A, start_B, Ainter, Binter, A_preference, B_preference);
            DebugStepState(Ainter, Binter, start_A, start_B, A, B);
            if (pos == -1) { Debug.Log("No start location is valid!"); break; }
            Debug.Log(AorB + " " + pos);

            (bool good_loop, List<CH2D_P_Index> new_loop) = IsolateLoop(A, B, Ainter, Binter, pos, A_diff, B_diff, A_preference, B_preference, operation); // A - moving CCW; B - moving CW;
            string n = "Final Point Count: "; for (int i = 0; i < new_loop.Count; i++) n += new_loop[i] + ", "; Debug.Log(n);
            if (good_loop) polygons.Add(new CH2D_Polygon(new_loop));
        }

        return polygons;
    }
    private static (poly, int) PickStart(List<int> start_A, List<int> start_B, int[] Ainter, int[] Binter, int A_preference, int B_preference)
    {   // Просто выбирает A или B который не был использован.
        int start_p;
        for (int i = start_A.Count - 1; i >= 0; i--)
            if (Ainter[start_A[i]] >= 0) { start_p = start_A[i]; start_A.RemoveAt(i); return (poly.A, start_p); }
            else start_A.RemoveAt(i);
        for (int i = start_B.Count - 1; i >= 0; i--)
            if (Binter[start_B[i]] >= 0) { start_p = start_B[i]; start_B.RemoveAt(i); return (poly.B, start_p); }
            else start_B.RemoveAt(i);
        return (poly.A, -1);
    }
    private static (List<int>, List<int>) GetEntryPoints(int[] Ainter, int[] Binter, BooleanOperation operation)
    {
        List<int> a = new List<int>(); List<int> b = new List<int>();
        for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
        return (a, b);/*
        switch (operation) {
            case BooleanOperation.Aonly:// Нужно найти наружние А и внутренние В
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
                //b = new List<int>(); for (int i = 0; i < Binter.Length; i++) if (Binter[i] == inn_point) b.Add(i);
                return (a, b);
            case BooleanOperation.Bonly:// Нужно найти наружние В и внутренние А
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
                //b = new List<int>(); for (int i = 0; i < Binter.Length; i++) if (Binter[i] == out_point) b.Add(i);
                return (a, b);
            case BooleanOperation.Union:// Нужно найти все пересечения для поиска возможного объединения
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
                return (a, b);
            case BooleanOperation.Diffr:// Нужно найти все пересечения для поиска наложений. Так как может быть 100% наложение, ищутся именно пересечения
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] >= 0) a.Add(i);
                return (a, b);
            default:
                return (null, null);
        }*/
    }

    private static (bool good_loop, List<CH2D_P_Index> points) IsolateLoop(List<CH2D_P_Index> Av, List<CH2D_P_Index> Bv, int[] Ainter, int[] Binter, int start_a, int A_diff, int B_diff, int A_preference, int B_preference, BooleanOperation operation)
    {
        List<CH2D_P_Index> points = new List<CH2D_P_Index>();// poly AorB = cp_side;

        int curr_A = start_a; int start_b = Ainter[start_a];
        int curr_B = start_b;
        Debug.Log("Isolate Loop Start: A: " + curr_A + " B: " + curr_B);
        int safety = -1; bool isDone = false;
        bool goodLoop = false;
        while (safety < 10 && !isDone)
        {
            Debug.Log(curr_A + " " + curr_B);
            
            points.Add(curr_A == -1 ? Bv[curr_B] : Av[curr_A]);
            safety++;
            int next_A = (curr_A >= 0) ? (curr_A + A_diff + Ainter.Length) % Ainter.Length : -1;
            int next_B = (curr_B >= 0) ? (curr_B + B_diff + Binter.Length) % Binter.Length : -1;
            if (next_B == start_b | next_A == start_a) { Debug.Log("Next A is finish point, leaving loop"); goodLoop = true; break; }
            //if (cp_side == poly.B && next_B == cp_pos) { Debug.Log("Next B is finish point, leaving loop"); goodLoop = true; break; }
            if (curr_A >= 0 && Ainter[curr_A] < 0) Ainter[curr_A] = used_point;
            if (curr_B >= 0 && Binter[curr_B] < 0) Binter[curr_B] = used_point;
            switch (operation) {
                case BooleanOperation.Aonly: (curr_A, curr_B) = LCS_DifferenceAB(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
                case BooleanOperation.Bonly: (curr_A, curr_B) = LCS_DifferenceBA(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
                case BooleanOperation.Union: (curr_A, curr_B) = LCS_DifferenceAB(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
                case BooleanOperation.Inter: (curr_A, curr_B) = LCS_DifferenceAB(Ainter, Binter, curr_A, curr_B, next_A, next_B, safety == 0); break;
            }
            

            Debug.Log(curr_A + " " + curr_B);
            curr_B = (curr_A >= 0 && Ainter[curr_A] >= 0) ? Ainter[curr_A] : curr_B;
            curr_A = (curr_B >= 0 && Binter[curr_B] >= 0) ? Binter[curr_B] : curr_A;
            if (curr_A == start_b && curr_B == start_a) { Debug.Log("Current point is an intersection where loop began, SUCCESSFULL FINISH"); goodLoop = true; break; }
            if (curr_A == -1 && curr_B == -1) { Debug.Log("Bad ending"); goodLoop = false; break; }
            
        }


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