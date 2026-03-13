using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// Я запрещаю кому-либо использовать написанный мной код для обучения нейросетей. Это моя интеллектуальная собственность.
// I forbid anyone to use code, written by me, to train neural networks. It is my intellectual property.
// TODO: придумать имя для класса. Тут теперь не только Greiner-Horrmann живет, но и самоделка.
// TODO: Перерисовать ASCII картинку. Я теперь работаю с гранями а не с точками.
// TODO: Вместо шагохода для сборки полигонов использовать однонаправленный граф, все грани одного типа объединить в интерфалы 
// TODO: Прикрутить поддержку многоуровневых полигонов, ту же самую которую я реализовал в GH алгоритме для Vector2 полигонов, но для CH2D_P_Vertice
// TODO: Прикрутить выпуклую декомпозицию, ту же самую которая лежит в Poly2DToolbox
// TODO: Если будет свободное время то сделать копию этого алгоритма так чтобы он мог принимать только Vector2 а не CH2D_P_Vertice+Vector2
// TOOD: сделать простеньую операцию для обрезация полигона об прямоугольник/шестиугольник для регулярных чанков. Сложно быть не должно.
// ЕЩВЩ: Блин, еще надо как-то улучшить начальный этап поиска пересечений. Щас там просто O(N*N) брут форс перебор. И я не хочу использовать sweep line, он привязан к осям, и поэтому я не могу ему доверять

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
    public enum EdgeSide {
        None = 0, 
        Inside, 
        Outside, 
        Out_Colin, // This edge faces different directions in two polygons
        Inn_Colin, // This edge faces the same direction in both polygons
        Used}
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
    public struct CutPolyIntSetting {
        public bool Union, Inter, Aonly, Bonly;
        public CutPolyIntSetting(bool Union, bool Inter, bool Aonly, bool Bonly)    
        { 
            this.Union = Union; this.Inter = Inter; this.Aonly = Aonly; this.Bonly = Bonly; 
        }
    }
    public static CutPolyIntSetting default_setting { get { return new CutPolyIntSetting(true, true, true, true); } }
    public static (int[] Ainter, int[] Binter, EdgeSide[] BufferAedge, EdgeSide[] BufferBedge) GetIntersectionAndMarkings(
        List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)
    {
        (int[] Ainter, int[] Binter) = MarkPoints(Ap, Bp, intersections);
        (EdgeSide[] BufferAedge, EdgeSide[] BufferBedge) = MarkEdges(Ap, Bp, intersections);
        //(EdgeSide[] BufferAedge2, EdgeSide[] BufferBedge2) = MarkEdges(Ap, Bp, intersections);

        return (Ainter, Binter, BufferAedge, BufferBedge);
    }
    public static (List<CH2D_Polygon> union, List<CH2D_Polygon> overlap, List<CH2D_Polygon> onlyA, List<CH2D_Polygon> onlyB) CutPolyInt(
        List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections, CutPolyIntSetting setting,
        bool DebugUnion = false, bool DebugAonly = false, bool DebugBonly = false, bool DebugInter = false)
    {
        (int[] Ainter, int[] Binter, EdgeSide[] BufferAedge, EdgeSide[] BufferBedge) = GetIntersectionAndMarkings(A, B, Ap, Bp, intersections);
        return CutPolyInt(V, A, B, Ap, Bp, intersections, setting, Ainter, Binter, BufferAedge, BufferBedge, DebugUnion, DebugAonly, DebugBonly, DebugInter);
    }
    public static (List<CH2D_Polygon> union, List<CH2D_Polygon> overlap, List<CH2D_Polygon> onlyA, List<CH2D_Polygon> onlyB) CutPolyInt(
        List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections, CutPolyIntSetting setting, int[] Ainter, int[] Binter, EdgeSide[] BufferAedge, EdgeSide[] BufferBedge,
        bool DebugUnion = false, bool DebugAonly = false, bool DebugBonly = false, bool DebugInter = false )
    {
        EdgeSide[] Aedges = new EdgeSide[BufferAedge.Length]; BufferAedge.CopyTo(Aedges, 0);
        EdgeSide[] Bedges = new EdgeSide[BufferBedge.Length]; BufferBedge.CopyTo(Bedges, 0);

        /*string intersections_s = "intersectins: "; for (int i = 0; i < intersections.Count; i++) intersections_s += " (" + intersections[i].A  + " " + intersections[i].B + ") "; Debug.Log(intersections_s);
        string ae = "A edges: "; for (int i = 0; i < Aedges.Length; i++) ae += Aedges[i] + " "; Debug.Log(ae); string be = "B edges: "; for (int i = 0; i < Bedges.Length; i++) be += Bedges[i] + " "; Debug.Log(be);
        ae = "A edges: "; for (int i = 0; i < Ainter.Length; i++) ae += Ainter[i] + " "; Debug.Log(ae); be = "B edges: "; for (int i = 0; i < Binter.Length; i++) be += Binter[i] + " "; Debug.Log(be);*/

        
        List<CH2D_Polygon> Union = new List<CH2D_Polygon>(); List<CH2D_Polygon> Inter = new List<CH2D_Polygon>(); List<CH2D_Polygon> Aonly = new List<CH2D_Polygon>(); List<CH2D_Polygon> Bonly = new List<CH2D_Polygon>();
        if (setting.Union)
        {
            Debug.Log("<color='red'>==== Union ====</color>");
            Union = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Union);
        }
        
        if (setting.Inter)
        {
            Debug.Log("<color='red'>==== Intersectiion ====</color>");
            BufferAedge.CopyTo(Aedges, 0);
            BufferBedge.CopyTo(Bedges, 0);
            Inter = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Inter);
        }

        BufferAedge.CopyTo(Aedges, 0);
        BufferBedge.CopyTo(Bedges, 0);
        if (setting.Aonly)
        {
            Debug.Log("<color='red'>==== A ONLY ====</color>");
            Aonly = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Aonly);
        }
        
        if (setting.Bonly)
        {
            Debug.Log("<color='red'>==== B ONLY ====</color>");
            Bonly = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Bonly);
        }
        
        //"<color='red'>==== Intersectiion ====</color>");
        float default_offset = 2f;
        float ab_offset = 1f + Mathf.Max(Aonly.Count, Bonly.Count);
        float union_offset = 1f + Union.Count;
        if (DebugInter)
        for (int i = 0; i < Inter.Count; i++) {
            int a_v_count = Inter[i].vertices.Count;
            for (int x = 0; x < a_v_count; x++)
                DebugUtilities.DebugDrawLine(V[Inter[i].vertices[x]], V[Inter[i].vertices[(x + 1) % a_v_count]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), default_offset + ab_offset + union_offset + 1f * i); //2f + 1f * i, 0.3f
        }
        //"<color='red'>==== Union ====</color>");
        if (DebugUnion)
            for (int i = 0; i < Union.Count; i++) {
            int a_v_count = Union[i].vertices.Count;
            for (int x = 0; x < a_v_count; x++)
                DebugUtilities.DebugDrawLine(V[Union[i].vertices[x]], V[Union[i].vertices[(x + 1) % a_v_count]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), default_offset + ab_offset + 1f * i); //2f + 1f * i, 0.3f
        }
        //"<color='red'>==== A ONLY ====</color>");
        if (DebugAonly)
            for (int i = 0; i < Aonly.Count; i++) {
            int a_v_count = Aonly[i].vertices.Count;
            for (int x = 0; x < a_v_count; x++)
                DebugUtilities.DebugDrawLine(V[Aonly[i].vertices[x]], V[Aonly[i].vertices[(x + 1) % a_v_count]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), default_offset + 1f * i); //2f + 1f * i, 0.3f
        }
        //"<color='red'>==== B ONLY ====</color>");
        if (DebugBonly)
            for (int i = 0; i < Bonly.Count; i++) {
            int a_v_count = Bonly[i].vertices.Count;
            for (int x = 0; x < a_v_count; x++)
                DebugUtilities.DebugDrawLine(V[Bonly[i].vertices[x]], V[Bonly[i].vertices[(x + 1) % a_v_count]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), default_offset + 1f * i); //2f + 1f * i, 0.3f
        }

        Debug.Log("<b><color=red> Aonly: " + Aonly.Count + "</color><color=blue> Bonly: " + Bonly.Count + "</color> <color=green>Inter: " + Inter.Count + "</color><color=yellow> Union: " + Union.Count + " </color></b>");
        return (Union, Inter, Aonly, Bonly);
    }

    public static (List<CH2D_Polygon> overlap, List<CH2D_Polygon> onlyA, List<CH2D_Polygon> onlyB) PolyPolyTest(List<Vector2> V, List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections, bool drawPoints, bool drawEdges, bool InterCalc, bool UnionCalc, bool AonlyCalc, bool BonlyCalc)
    {   // Ничего не записывает, но все отображает
        if (intersections.Count == 0) { Debug.Log("Нет пересечений междлу полигонами"); return (null, null, null); }

        (int[] Ainter, int[] Binter) = MarkPoints(Ap, Bp, intersections);
        (EdgeSide[] BufferAedge, EdgeSide[] BufferBedge) = MarkEdges(Ainter, Binter, A, B);
        EdgeSide[] Aedges = new EdgeSide[BufferAedge.Length]; BufferAedge.CopyTo(Aedges, 0);
        EdgeSide[] Bedges = new EdgeSide[BufferBedge.Length]; BufferBedge.CopyTo(Bedges, 0);
        if (drawPoints) {
            for (int i = 0; i < Ainter.Length; i++)  {
                if (Ainter[i] == out_point) DebugUtilities.DebugDrawSquare(V[A[i]], Color.red, time: 5f);
                else if (Ainter[i] == inn_point) DebugUtilities.DebugDrawSquare(V[A[i]], Color.blue, time: 5f);
                else if (Ainter[i] >= 0) { DebugUtilities.DebugDrawSquare(V[A[i]], Color.yellow, time: 5f); }
                else { DebugUtilities.DebugDrawSquare(V[A[i]], Color.white, time: 5f); }
            }

            for (int i = 0; i < Binter.Length; i++) {
                if (Binter[i] == out_point) DebugUtilities.DebugDrawSquare(V[B[i]], Color.red, time: 5f);
                else if (Binter[i] == inn_point) DebugUtilities.DebugDrawSquare(V[B[i]], Color.blue, time: 5f);
                else if (Binter[i] >= 0) { DebugUtilities.DebugDrawSquare(V[B[i]], Color.yellow, time: 5f); }
                else { DebugUtilities.DebugDrawSquare(V[B[i]], Color.white, time: 5f); }
            }
        }
        if (drawEdges){
            for (int i = 0; i < Aedges.Length; i++)  {
                Color color = Color.black;
                switch (Aedges[i]) {
                    case EdgeSide.None: color = Color.white; break;
                    case EdgeSide.Inside: color = Color.green; break;
                    case EdgeSide.Outside: color = Color.red; break;
                    case EdgeSide.Inn_Colin: color = Color.greenYellow; break;
                    case EdgeSide.Out_Colin: color = Color.pink; break;
                }
                DebugUtilities.DebugDrawLine(V[A[i]], V[A[(i + 1) % Aedges.Length]], color, 3f);
            }

            for (int i = 0; i < Bedges.Length; i++) {
                Color color = Color.black;
                switch (Bedges[i]) {
                    case EdgeSide.None: color = Color.white; break;
                    case EdgeSide.Inside: color = Color.green; break;
                    case EdgeSide.Outside: color = Color.red; break;
                    case EdgeSide.Inn_Colin: color = Color.greenYellow; break;
                    case EdgeSide.Out_Colin: color = Color.pink; break;
                }
                DebugUtilities.DebugDrawLine(V[B[i]], V[B[(i + 1) % Bedges.Length]], color, 6f);
            }
        }
        
        string ae = "A edges: "; for (int i = 0; i < Aedges.Length; i++) ae += Aedges[i] + " "; Debug.Log(ae);
        string be = "B edges: "; for (int i = 0; i < Bedges.Length; i++) be += Bedges[i] + " "; Debug.Log(be);
        ae = "A edges: "; for (int i = 0; i < Ainter.Length; i++) ae += Ainter[i] + " "; Debug.Log(ae);
        be = "B edges: "; for (int i = 0; i < Binter.Length; i++) be += Binter[i] + " "; Debug.Log(be);

        Debug.Log("<color='red'>==== Union Calculation ====</color>");
        List<CH2D_Polygon> Union = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Union);

        Debug.Log("<color='red'>==== Intersectiion Calculation ====</color>");
        BufferAedge.CopyTo(Aedges, 0);
        BufferBedge.CopyTo(Bedges, 0);
        List<CH2D_Polygon> Inter = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Inter);

        Debug.Log("<color='red'>==== A ONLY Calculation ====</color>");
        BufferAedge.CopyTo(Aedges, 0);
        BufferBedge.CopyTo(Bedges, 0);
        List<CH2D_Polygon> Aonly = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Aonly);

        Debug.Log("<color='red'>==== B ONLY Calculation ====</color>");
        List<CH2D_Polygon> Bonly = IsolateLoops(A, B, Ainter, Binter, Aedges, Bedges, BooleanOperation.Bonly);
        Debug.Log("Aonly: " + Aonly.Count + " Bonly: " + Bonly.Count + " Inter: " + Inter.Count + " Union: " + Union.Count);

        if (InterCalc) {
            for (int i = 0; i < Inter.Count; i++){
                int a_v_count = Inter[i].vertices.Count;
                for (int x = 0; x < a_v_count; x++) {
                    int y = (x + 1) % a_v_count;
                    DebugUtilities.DebugDrawLine(V[Inter[i].vertices[x]], V[Inter[i].vertices[y]], DebugUtilities.HSVGradient(new Color(0f, 0.5f, 0f), new Color(0.2f, 1f, 0.2f), x, a_v_count - 1), 5f); //2f + 1f * i, 0.3f
                }
            }
        }
        
        if (UnionCalc) {
            for (int i = 0; i < Union.Count; i++){
                int a_v_count = Union[i].vertices.Count;
                for (int x = 0; x < a_v_count; x++) {
                    int y = (x + 1) % a_v_count;
                    DebugUtilities.DebugDrawLine(V[Union[i].vertices[x]], V[Union[i].vertices[y]], DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), 5f); //2f + 1f * i, 0.3f
                }
            }
        }

        if (AonlyCalc) {
            for (int i = 0; i < Aonly.Count; i++) {
                int a_v_count = Aonly[i].vertices.Count;
                for (int x = 0; x < a_v_count; x++) {
                    int y = (x + 1) % a_v_count;
                    //DebugUtilities.PickGradient(x, a_v_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet);
                    DebugUtilities.DebugDrawLine(V[Aonly[i].vertices[x]], V[Aonly[i].vertices[y]], DebugUtilities.HSVGradient(new Color(0.5f, 0f, 0f), new Color(1f, 0.2f, 0.2f), x, a_v_count - 1), 5f); //2f + 1f * i, 0.3f
                }
            }
        }
        
        if (BonlyCalc) {
            for (int i = 0; i < Bonly.Count; i++) {
                int a_v_count = Bonly[i].vertices.Count;
                for (int x = 0; x < a_v_count; x++) {
                    int y = (x + 1) % a_v_count;
                    DebugUtilities.DebugDrawLine(V[Bonly[i].vertices[x]], V[Bonly[i].vertices[y]], DebugUtilities.HSVGradient(new Color(0f, 0f, 0.5f), new Color(0.2f, 0.2f, 1f), x, a_v_count - 1), 5f); //2f + 1f * i, 0.3f
                }
            }
        }
        return (null, null, null);
    }

    private static List<CH2D_Polygon> IsolateLoops(List<CH2D_P_Index> A, List<CH2D_P_Index> B, int[] Ainter, int[] Binter, EdgeSide[] Aedge, EdgeSide[] Bedge, BooleanOperation operation)
    {
        Debug.Log("Пожалуйста подними safety до какого-нибудь приличного числа!");
        List<CH2D_Polygon> polygons = new List<CH2D_Polygon>();
        (List<int> start_A, List<int> start_B) = GetEntryPoints(Aedge, Bedge, operation);
        /*string n = "Aedge: ";   for (int i = 0; i < start_A.Count; i++) n += start_A[i] + " "; Debug.Log(n);
        n = "Bedge: ";          for (int i = 0; i < start_B.Count; i++) n += start_B[i] + " "; Debug.Log(n);*/
        int safety = 0; int safety_margin = start_A.Count + start_B.Count;
        //DebugStepState(Ainter, Binter, start_A, start_B, A, B);

        while (((start_A.Count + start_B.Count) > 0) && safety < 20)
        {
            safety += 1;
            (poly AorB, int pos) = PickStart(start_A, start_B, Aedge, Bedge);
            /*n = "Aedge: "; for (int i = 0; i < start_A.Count; i++) n += start_A[i] + " "; Debug.Log(n);
            n = "Bedge: "; for (int i = 0; i < start_B.Count; i++) n += start_B[i] + " "; Debug.Log(n);
            DebugStepState(Ainter, Binter, start_A, start_B, A, B);*/
            if (pos == -1) { Debug.Log("No start location is valid!"); break; }
            // Debug.Log(AorB + " " + pos);

            (bool good_loop, List<CH2D_P_Index> new_loop) = IsolateLoop(A, B, Ainter, Binter, Aedge, Bedge, AorB, pos, operation); // A - moving CCW; B - moving CW;
            //n = "<b><color=white>Final Point Count:</b></color> "; for (int i = 0; i < new_loop.Count; i++) n += new_loop[i] + ", "; Debug.Log(n);
            if (good_loop) polygons.Add(new CH2D_Polygon(new_loop));
        }

        return polygons;
    }

    private static (bool good_loop, List<CH2D_P_Index> points) IsolateLoop(List<CH2D_P_Index> Av, List<CH2D_P_Index> Bv, int[] Ainter, int[] Binter, EdgeSide[] Aedge, EdgeSide[] Bedge, poly startP, int s_pos, BooleanOperation operation)
    {
        List<CH2D_P_Index> points = new List<CH2D_P_Index>();// poly AorB = cp_side;

        int A_diff = 1; int B_diff = 1; int A_off = 0; int B_off = 0;
        if (operation == BooleanOperation.Aonly) { A_diff = 1; B_diff = -1; A_off = 0; B_off = -1; }
        if (operation == BooleanOperation.Bonly) { A_diff = -1; B_diff = 1; A_off = -1; B_off = 0; }
        
        int curr_a = startP == poly.A ? (s_pos - A_off) % Aedge.Length : -1;
        int curr_b = startP == poly.B ? (s_pos - B_off) % Bedge.Length : -1;
        int start_a = (curr_a == -1) ? (Binter[curr_b] >= 0 ? Binter[curr_b] : -1) : curr_a;
        int start_b = (curr_b == -1) ? (Ainter[curr_a] >= 0 ? Ainter[curr_a] : -1) : curr_b;
        /*
        Debug.Log("Aoff " + A_off + " Boff " + B_off);
        Debug.Log("Isolate Loop Start: side: " + curr_a + " pos: " + curr_b);
        Debug.Log("Isolate Loop Start points: a " + start_a + " b: " + start_b);
        string n = "Aedge: "; for (int i = 0; i < Aedge.Length; i++) n += Aedge[i] + " "; Debug.Log(n);
        n = "Bedge: "; for (int i = 0; i < Bedge.Length; i++) n += Bedge[i] + " "; Debug.Log(n);
        n = "Aedge: "; for (int i = 0; i < Ainter.Length; i++) n += Ainter[i] + " "; Debug.Log(n);
        n = "Bedge: "; for (int i = 0; i < Binter.Length; i++) n += Binter[i] + " "; Debug.Log(n);*/
        int safety = -1;
        bool goodLoop = false;
        
        while (safety < 35)
        {
            points.Add(curr_a < 0 ? Bv[curr_b] : Av[curr_a]);
            safety++;
            //Debug.Log("<b>STEP</b>, " + curr_a + " " + curr_b);
            // curr_a и curr_b, только одно значение может иметь функциональное значение. Это означает что перезапись здесь будет лишь одна
            int next_a_index = -99; int next_b_index = -99; int alt_a_index = -99; int alt_b_index = -99;
            EdgeSide next_a_side = EdgeSide.None; EdgeSide next_b_side = EdgeSide.None; EdgeSide alt_a_side = EdgeSide.None; EdgeSide alt_b_side = EdgeSide.None;
            if (curr_a >= 0)
            {// A only valid
                next_a_index = (curr_a + A_diff + Aedge.Length) % Aedge.Length;
                int next_a_index_value = (curr_a + A_diff + A_off + Aedge.Length) % Aedge.Length;
                next_a_side = Aedge[next_a_index_value];
                Aedge[(curr_a + A_off + Aedge.Length) % Aedge.Length] = EdgeSide.Used;

                curr_b = Ainter[next_a_index];
                if (curr_b >= 0)
                {
                    next_b_index = curr_b;
                    int next_b_index_value = (curr_b + B_off + Bedge.Length) % Bedge.Length;
                    next_b_side = Bedge[next_b_index_value];
                }
                if (next_a_side == EdgeSide.Out_Colin && operation == BooleanOperation.Union)
                {
                    //next_a_index = Binter[next_b_index];
                    //next_a_side = Aedge[next_a_index];
                    alt_a_index = Binter[next_b_index];
                    alt_a_side = Aedge[alt_a_index];
                }
            }
            else/*(curr_b >= 0)*/
            { // B only valid   
                next_b_index = (curr_b + B_diff + Bedge.Length) % Bedge.Length;
                int next_b_index_value = (curr_b + B_diff + B_off + Bedge.Length) % Bedge.Length;
                next_b_side = Bedge[next_b_index_value];
                Bedge[(curr_b + B_off + Bedge.Length) % Bedge.Length] = EdgeSide.Used;

                curr_a = Binter[next_b_index];
                if (curr_a >= 0)
                {
                    next_a_index = curr_a;
                    int next_a_index_value = (curr_a + A_off + Aedge.Length) % Aedge.Length;
                    next_a_side = Aedge[next_a_index_value];
                }
                if (next_b_side == EdgeSide.Out_Colin && operation == BooleanOperation.Union)
                {
                    //next_b_index = Ainter[next_a_index];
                    //next_b_side = Bedge[next_b_index];
                    alt_b_index = Ainter[next_a_index];
                    alt_b_side = Bedge[alt_b_index];
                }
            }/*
            Debug.Log(" a: " + curr_a + " " + next_a_index + " " + next_a_side + " b: " + curr_b + " " + next_b_index + " " + next_b_side + " alt a: " + alt_a_index + " " + alt_a_side + " alt b: " + alt_b_index + " " + alt_b_side);
            n = "Aedge: "; for (int i = 0; i < Aedge.Length; i++) n += Aedge[i] + " "; Debug.Log(n);
            n = "Bedge: "; for (int i = 0; i < Bedge.Length; i++) n += Bedge[i] + " "; Debug.Log(n);*/
            if ((next_a_index == start_a | next_b_index == start_b)) 
            {   Debug.Log("<color=green>Current point is finish point, leaving</color>");
                goodLoop = true;
                break; 
            }
            poly next_p = poly.None;
            switch (operation)
            {
                case BooleanOperation.Aonly: next_p = LCS_Aonly(next_a_side, next_b_side, alt_a_side, alt_b_side); break;
                case BooleanOperation.Bonly: next_p = LCS_Bonly(next_a_side, next_b_side, alt_a_side, alt_b_side); break;
                case BooleanOperation.Union: next_p = LCS_Union(next_a_side, next_b_side, alt_a_side, alt_b_side); break;
                case BooleanOperation.Inter: next_p = LCS_Inter(next_a_side, next_b_side, alt_a_side, alt_b_side); break;
            }
            
            //Debug.Log("<b> Return: </b> " + next_p);
            if (next_p == poly.None) { Debug.Log("<color=red>Bad ending</color>"); goodLoop = false; break; }
            curr_a = -1;
            curr_b = -1;
            switch (next_p) {
                case poly.A: curr_a = next_a_index; break;
                case poly.B: curr_b = next_b_index; break;
                case poly.altA: curr_a = alt_a_index; break;
                case poly.altB: curr_b = alt_b_index; break;
            }
            //curr_a = next_p == poly.A ? next_a_index : -1;
            //curr_b = next_p == poly.B ? next_b_index : -1;


            //if (curr_A == start_b && curr_B == start_a) { Debug.Log("Current point is an intersection where loop began, SUCCESSFULL FINISH"); goodLoop = true; break; }


        }
        if (safety >= 35) Debug.Log("<color=orange>Ran out of safety margins, Comissar Yarrick, please consider rising safety limit here to a highert value</color>");
        return (goodLoop, points);
    }

    private static poly LCS_Aonly(EdgeSide nextA, EdgeSide nextB, EdgeSide altA, EdgeSide altB)
    {   // A out + B inn + intersection
        if (nextA == EdgeSide.Outside) return poly.A;
        if (nextB == EdgeSide.Inside ) return poly.B;
        
        if (nextB == EdgeSide.Inside ) return poly.B;
        if (nextA == EdgeSide.Outside) return poly.A;

        if (nextA == EdgeSide.Out_Colin) return poly.A;

        return poly.None;
    }
    private static poly LCS_Bonly(EdgeSide nextA, EdgeSide nextB, EdgeSide altA, EdgeSide altB)
    {   // A inn + B out + intersection
        if (nextB == EdgeSide.Outside) return poly.B;
        if (nextA == EdgeSide.Inside) return poly.A;

        if (nextA == EdgeSide.Inside) return poly.A;
        if (nextB == EdgeSide.Outside) return poly.B;

        if (nextB == EdgeSide.Out_Colin) return poly.B;
        return poly.None;
    }
    // Тут вот интересный костыль есть:
    // Проверяется Только точка А на наличие значения Inn_Colin
    // Так как точки A & B со значением Inn_Colin всегда идут парами (внутренние коллинеарные, две грани однонаправлены, начинаются и кончаются в одной точке)
    // Поэтому в выборке стартовых точек GetEntryPoints выбираются только A Inn_Colin
    private static poly LCS_Inter(EdgeSide nextA, EdgeSide nextB, EdgeSide altA, EdgeSide altB)
    {   // A inn + B out + intersection
        if (nextA == EdgeSide.Inside) return poly.A;
        if (nextB == EdgeSide.Inside) return poly.B;

        if (nextA == EdgeSide.Inn_Colin) return poly.A; // Проверяется только точка A.
        return poly.None;
    }
    private static poly LCS_Union(EdgeSide nextA, EdgeSide nextB, EdgeSide altA, EdgeSide altB)
    {   // A inn + B out + intersection
        if (nextA == EdgeSide.Outside) return poly.A;
        if (nextB == EdgeSide.Outside) return poly.B;

        if (nextA == EdgeSide.Inn_Colin) return poly.A;
        if (nextA == EdgeSide.Out_Colin)
        {
            if (altA == EdgeSide.Outside) return poly.altA;
            if (altB == EdgeSide.Outside) return poly.altB;
        }
        return poly.None;
    }
    private static poly LCS_Plug(EdgeSide nextA, EdgeSide nextB)
    {  
        Debug.Log("Используется Dummy затычка");
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
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Out_Colin) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Inside ) b.Add(i);
                return (a, b);
            case BooleanOperation.Bonly:// Нужно найти наружние В и внутренние А
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Inside ) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Outside) b.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Out_Colin) b.Add(i);
                return (a, b);
            case BooleanOperation.Union:// Нужно найти все пересечения для поиска возможного объединения
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Outside) a.Add(i);
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Inn_Colin) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Outside) b.Add(i);
                return (a, b);
            case BooleanOperation.Inter:// Нужно найти все пересечения для поиска наложений. Так как может быть 100% наложение, ищутся именно пересечения
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Inside ) a.Add(i);
                for (int i = 0; i < Ainter.Length; i++) if (Ainter[i] == EdgeSide.Inn_Colin) a.Add(i);
                for (int i = 0; i < Binter.Length; i++) if (Binter[i] == EdgeSide.Inside ) b.Add(i);
                return (a, b);
            default: return (a, b);
        }
    }

    //private static 

    private static (EdgeSide[], EdgeSide[]) MarkEdges(List<Vector2> A, List<Vector2> B, List<Pair> intersections)
    {   // План прост: Использовать функцию Geo3D.IsVectorInSectorLongNameForgot(0 для определения стороны? а затем по соседям определить принадлежность
        // Жирным плюсом является отсутствие нужды в While и всей итой итеративной хуйни которую я написал в прошлой реализации
        // Вроде работает, также точно как и предыдущая реализация
        EdgeSide[] Aedge = new EdgeSide[A.Count]; EdgeSide[] Bedge = new EdgeSide[B.Count];

        if (intersections.Count == 0)
        {
            bool BinsideA = Poly2DToolbox.IsPointInsidePolygon(B[0], A);
            bool AinsideB = Poly2DToolbox.IsPointInsidePolygon(A[0], B);
            EdgeSide Aside = AinsideB ? EdgeSide.Inside : EdgeSide.Outside;
            EdgeSide Bside = BinsideA ? EdgeSide.Inside : EdgeSide.Outside;
            for (int i = 0; i < Aedge.Length; i++) Aedge[i] = Aside;
            for (int i = 0; i < Bedge.Length; i++) Bedge[i] = Bside;
            return (Aedge, Bedge);
        }

        int[] Ainter = new int[A.Count]; int[] Binter = new int[B.Count];
        for (int i = 0; i < Ainter.Length; i++) Ainter[i] = -1;
        for (int i = 0; i < Binter.Length; i++) Binter[i] = -1;
        for (int i = 0; i < intersections.Count; i++) { Pair p = intersections[i]; Ainter[p.A] = p.B; Binter[p.B] = p.A; }

        // Поиск коллинеаров
        for (int i = 0; i < intersections.Count; i++)
        {
            int Aindex = intersections[i].A; int Bindex = intersections[i].B;
            int prevAindex = (Aindex - 1 +  Ainter.Length) % Ainter.Length;
            int nextAindex = (Aindex + 1) % Ainter.Length;
            int prevBindex = (Bindex - 1 +  Binter.Length) % Binter.Length;
            int nextBindex = (Bindex + 1) % Binter.Length;
            if (Binter[prevBindex] == nextAindex)
            {
                Aedge[Aindex]     = EdgeSide.Out_Colin;
                Bedge[prevBindex] = EdgeSide.Out_Colin;
                continue;
            }
            if (Binter[nextBindex] == nextAindex)
            {
                Aedge[Aindex] = EdgeSide.Inn_Colin;
                Bedge[Bindex] = EdgeSide.Inn_Colin;
            }
        }
        //Debug.Log(DebugUtilities.DebugListString(Aedge));
        //Debug.Log(DebugUtilities.DebugListString(Bedge));
        for (int i = 0; i < intersections.Count; i++)
        {
            int Aindex = intersections[i].A; int Bindex = intersections[i].B;
            int nextAindex = (Aindex + 1) % Ainter.Length;
            int nextBindex = (Bindex + 1) % Binter.Length;
            int prevAindex = (Aindex - 1 + Ainter.Length) % Ainter.Length;
            int prevBindex = (Bindex - 1 + Binter.Length) % Binter.Length;

            if (Aedge[intersections[i].A] == EdgeSide.None)
            {
                bool a_outside = Geo3D.DoesVectorDLieInSectorAB(A[Aindex], B[prevBindex], B[nextBindex], A[nextAindex]);
                Aedge[Aindex] = a_outside ? EdgeSide.Outside : EdgeSide.Inside;
            }
            if (Bedge[intersections[i].B] == EdgeSide.None)
            {
                bool b_outside = Geo3D.DoesVectorDLieInSectorAB(B[Bindex], A[prevAindex], A[nextAindex], B[nextBindex]);
                Bedge[Bindex] = b_outside ? EdgeSide.Outside : EdgeSide.Inside;
            }
        }

        // Закрашивание всех последующих граней под цвет предыдущей
        int a_offset = 0;
        for (int i = 0; i < Aedge.Length; i++)
            if (Aedge[(0 - i + Aedge.Length) % Aedge.Length] != EdgeSide.None) { a_offset = i; break; }
        for (int i = 0; i < Aedge.Length; i++)
        {
            int i_curr = (i + Aedge.Length - a_offset) % Aedge.Length;
            int prev_i = (i_curr - 1 + Aedge.Length) % Aedge.Length;
            if (Aedge[i_curr] == EdgeSide.None) Aedge[i_curr] = Aedge[prev_i];
        }

        int b_offset = 0;
        for (int i = 0; i < Bedge.Length; i++)
            if (Bedge[(0 - i + Bedge.Length) % Bedge.Length] != EdgeSide.None) { b_offset = i; break; }
        for (int i = 0; i < Bedge.Length; i++)
        {
            int i_curr = (i + Bedge.Length - b_offset) % Bedge.Length;
            int prev_i = (i_curr - 1 + Bedge.Length) % Bedge.Length;
            if (Bedge[i_curr] == EdgeSide.None) Bedge[i_curr] = Bedge[prev_i];
        }
        Debug.Log(DebugUtilities.DebugListString(Aedge));
        Debug.Log(DebugUtilities.DebugListString(Bedge));

        return (Aedge, Bedge);
    }
    private static (EdgeSide[], EdgeSide[]) MarkEdges(int[] Ainter, int[] Binter, List<CH2D_P_Index> A, List<CH2D_P_Index> B)
    {
        EdgeSide[] Aedge = new EdgeSide[Ainter.Length]; EdgeSide[] Bedge = new EdgeSide[Binter.Length];
        // Вообще надо бы начать найдя наружную точку, но в дегенеративных случаях наружной точки может не быть
        // Edge - определяется как начальная + последующая точки. 
        // Итеративно решает к какой стороне принадлежит грань
        /*
        Debug.Log(Binter.Length + " " + B.Count);
        Debug.Log(DebugUtilities.DebugListString(Ainter));
        Debug.Log(DebugUtilities.DebugListString(Binter));*/
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
                //Debug.Log(a_edge + " " + currA + " " + nextA);
                if (a_edge != EdgeSide.None) { Aedge[i] = a_edge; continue; }   // Проверка значений точек

                CH2D_P_Index prevBPindex = B[prevBindex]; // Прооверкка на коллинеарность
                CH2D_P_Index nextBPindex = B[nextBindex];
                CH2D_P_Index nextAPindex = A[nextAindex];
                //Debug.Log(prevBindex + " " + nextBindex + " " + nextAindex);
                if (nextAPindex == nextBPindex) { Aedge[i] = EdgeSide.Inn_Colin; continue; }
                if (nextAPindex == prevBPindex) { Aedge[i] = EdgeSide.Out_Colin; continue; }
                //Debug.Log("curr: " + nextAindex + " prev: " + prevAindex + " next: " + nextAindex);Debug.Log(Bedge[currA] + " " + Aedge[prevAindex] + " " + Aedge[nextAindex]);
                Aedge[i] = EdgeBasedSolution(Bedge[currA], Aedge[prevAindex], Aedge[nextAindex]);
                //Debug.Log(Aedge[i]);
                if (Aedge[i] == EdgeSide.None) solved = false;
                //if (Aedge[prevAindex] == EdgeSide.Inside) { Aedge[i] = EdgeSide.Outside; continue; }
                //solved = false;
            }
            for (int i = 0; i < Binter.Length; i++)
            {   // Solving for B polygon
                if (Bedge[i] != EdgeSide.None) continue;
                int currBindex = i; int nextBindex = (i + 1) % Binter.Length; int prevBindex = (i - 1 + Binter.Length) % Binter.Length;
                int currB = Binter[currBindex]; int nextB = Binter[nextBindex];
                int prevAindex = (currB - 1 + Ainter.Length) % Ainter.Length;
                int nextAindex = (currB + 1) % Ainter.Length;

                //Debug.Log(currBindex + " " + nextBindex + " " + prevBindex);
                EdgeSide b_edge = PointBasedSolution(currB, nextB, nextAindex, prevAindex);
                if (b_edge != EdgeSide.None) { Bedge[i] = b_edge; continue; }   // Проверка значений точек

                CH2D_P_Index prevAPindex = A[prevAindex]; // Прооверкка на коллинеарность
                CH2D_P_Index nextAPindex = A[nextAindex];
                CH2D_P_Index nextBPindex = B[nextBindex];
                //Debug.Log(prevAPindex + " " + nextAPindex + " " + nextBPindex);
                if (nextBPindex == nextAPindex) { Bedge[i] = EdgeSide.Inn_Colin; continue; }
                if (nextBPindex == prevAPindex) { Bedge[i] = EdgeSide.Out_Colin; continue; }
                //Debug.Log(Aedge[currB] + " " + Bedge[prevBindex] + " " + Bedge[nextBindex]);
                Bedge[i] = EdgeBasedSolution(Aedge[currB], Bedge[prevBindex], Bedge[nextBindex]);
                //Debug.Log(i + " " + Bedge[i]);
                //Debug.Log(Bedge[i]);
                if (Bedge[i] == EdgeSide.None) solved = false;
            }
            if (solved) break;
        }
        Debug.Log(DebugUtilities.DebugListString(Aedge));
        Debug.Log(DebugUtilities.DebugListString(Bedge));
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
            if ((prevBedge == EdgeSide.Out_Colin) && (nextBedge == EdgeSide.Out_Colin)) return EdgeSide.Outside; // A strange workaround, may backfire
            return EdgeSide.None;
        }
    }
   
    private enum poly {A, B, None, altA, altB};
    private enum BooleanOperation { Aonly, Bonly, Union, Inter };
    /// <summary>
    /// Checks whether polygon B is inside or outside polygon A. It also checks whether polygon B touches polygon A. <br/>
    /// It returns one of following: OutsideTouching, OutsideFull, Intersecting, InsideFull, InsideTouching.
    /// </summary>
    public static PolygonIntersection Is_BPoly_Inside_APoly(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)
    {
        (int[] Ainter, int[] Binter) = MarkPoints(Ap, Bp, intersections);
        (EdgeSide[] Aedges, EdgeSide[] Bedges) = MarkEdges(Ainter, Binter, A, B);
        return PolygonIntersectionTypeIdentify(Bedges);
    }
    public static bool IsPolygonIntersectionOfType(EdgeSide[] edges, PolygonIntersection option)
    {
        PolygonIntersection pit = PolygonIntersectionTypeIdentify(edges);
        Debug.Log("Intersection type check for polygon A: " + pit);
        Debug.Log(DebugUtilities.DebugListString(edges));
        switch (option)
        {
            case PolygonIntersection.InsideAny : return (pit == PolygonIntersection.InsideFull  | pit == PolygonIntersection.InsideTouching );
            case PolygonIntersection.OutsideAny: return (pit == PolygonIntersection.OutsideFull | pit == PolygonIntersection.OutsideTouching);
            default: return (pit == option);
        }
    }
    public static PolygonIntersection PolygonIntersectionTypeIdentify(EdgeSide[] edges)
    {
        bool HasIntersections = false;
        bool HasOutsides = false;
        bool HasInsides = false;
        for (int i = 0; i < edges.Length; i++) if (edges[i] == EdgeSide.Outside  ) { HasOutsides      = true; break; }
        for (int i = 0; i < edges.Length; i++) if (edges[i] == EdgeSide.Inside   ) { HasInsides       = true; break; }
        for (int i = 0; i < edges.Length; i++) if (edges[i] == EdgeSide.Out_Colin) { HasIntersections = true; break; } // Есть наружные касания, значит можно получить Union с удалением этих граней. 
        for (int i = 0; i < edges.Length; i++) if (edges[i] == EdgeSide.Inn_Colin) { HasIntersections = true; break; } // Это внутреннее касание, значит полигон тоже пересекается. Это полезно для получения разности. 
        if (HasOutsides & HasInsides) return PolygonIntersection.Intersecting;
        if (HasIntersections)
        {
            if (HasInsides ) return PolygonIntersection.InsideTouching ;
            if (HasOutsides) return PolygonIntersection.OutsideTouching;
        }
        else
        {
            if (HasInsides ) return PolygonIntersection.InsideFull ;
            if (HasOutsides) return PolygonIntersection.OutsideFull;
        }
        int out_colin_counter = 0;
        int inn_colin_counter = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            if (edges[i] == EdgeSide.Out_Colin) out_colin_counter += 1;
            if (edges[i] == EdgeSide.Inn_Colin) inn_colin_counter += 1;
        }
        if (out_colin_counter == edges.Length) return PolygonIntersection.Intersecting; // Вот эти фигульки стоит заменить на SameExact и SameOpposite
        if (inn_colin_counter == edges.Length) return PolygonIntersection.Intersecting;
        throw new System.Exception("Your polygon B is strange, it has been unable to be classified as Inside or outside. It could be that it is degenerate and flat, or have duplicate vertices.");
    }
    public static bool IsPolyOutside(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections) {
        PolygonIntersection pi = Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections); return pi == PolygonIntersection.OutsideFull | pi == PolygonIntersection.OutsideTouching;
    }
    public static bool IsPolyOutsideFully(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)  {
        return Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections) == PolygonIntersection.OutsideFull;
    }
    public static bool IsPolyOutsideTouching(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections) {
        return Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections) == PolygonIntersection.OutsideTouching;
    }
    public static bool IsPolyIntersecting(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections) {
        return Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections) == PolygonIntersection.Intersecting;
    }
    public static bool IsPolyInsideTouching(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections) {
        return Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections) == PolygonIntersection.InsideTouching;
    }
    public static bool IsPolyIsnideFully(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections) {
        return Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections) == PolygonIntersection.InsideFull;
    }
    public static bool IsPolyInside(List<CH2D_P_Index> A, List<CH2D_P_Index> B, List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections) {
        PolygonIntersection pi = Is_BPoly_Inside_APoly(A, B, Ap, Bp, intersections); return pi == PolygonIntersection.InsideTouching | pi == PolygonIntersection.InsideFull;
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

    // TODO: replace Poly2DToolbox.IsPointInsidePolygon for Geo3D.SomeSecotrFuncitonForgotNameItsTooLong // Thinksing, about it, i can do it directly in MarkEdges
    private static (int[] Aside, int[] Bside) MarkPoints(List<Vector2> Ap, List<Vector2> Bp, List<Pair> intersections)
    {
        int[] Ainter = new int[Ap.Count]; // default value - 0
        int[] Binter = new int[Bp.Count];
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
            Ainter[i] = Poly2DToolbox.IsPointInsidePolygon(Ap[i], Bp) ? inn_point : out_point;
        }
        for (int i = 0; i < Binter.Length; i++)
        {
            if (Binter[i] != used_point) continue;
            Binter[i] = Poly2DToolbox.IsPointInsidePolygon(Bp[i], Ap) ? inn_point : out_point;
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