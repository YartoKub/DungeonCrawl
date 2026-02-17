using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
// Границы чанка определены Convex Hull, также чанк имеет BBox, просто чтобы был
// Внутри чанка не должно быть пересекающихся полигонов, все полигоны находятся на одном уровне, в общем графе
[Serializable]
public class CH2D_Chunk
{
    public List<CH2D_Polygon> polygons;
    public GraphDynamicList connections; // Сериализация не работает с абстрактными классами, поэтому тут нужно какой-то определенный выбирать. 
    // Чтобы получить выбор между несколькими дочерними классами можно создать wrapper, в которм enumератором указывать на тот который нужно использовать
    public const UInt16 MaxVertices = 1000; //UInt16.MaxValue; // Если число вершин выходит за пределы этого числа, чанк нужно разбить на меньшие части
    [SerializeField] public List<Vector2> vertices;
    public List<CH2D_P_Index> ConvexHull;
    public enum PolygonAddMode { Monolith, FillHoles}
    public CH2D_Chunk()
    {
        polygons = new List<CH2D_Polygon>();
        vertices = new List<Vector2>();
        ConvexHull = new List<CH2D_P_Index>();
        if (connections == null) connections = new GraphDynamicList();
    }
    // ======================================
    //  ДОБАВЛЕНИЕ НОВЫХ ШТУК
    // ======================================
    public void AddPolygon(List<Vector2> points, PolygonAddMode polygon_add_mode)
    {

        Poly2D poly;
        bool has_compiled = Poly2D.CompilePolygon(points, out poly);
        if (!has_compiled) return;
        
        this.AddPolygon(poly, polygon_add_mode);
        return;
    }
    // Это единственная рабочая функция добавления полигона, можно не беспокоиться какой полигон оказывается добавлен
    // Надо обернуть все в try+carch и вернуть bool чтобы сделать эту штуку еще более безопасной. 
    public void AddPolygon(Poly2D poly, PolygonAddMode polygon_add_mode = PolygonAddMode.Monolith)
    {   // Подохреваю что некоторую безопасность, такую как MutualVerticeIncorporation, можно оставить лишь на конечном этапе. Но мне лень это проверять
        Debug.Log("========" +polygon_add_mode);
        CH2D_Polygon int_poly = new CH2D_Polygon();
        int_poly.isHole = poly.isHole;
        int_poly.convex = poly.convex;
        int_poly.BBox.SetAB(poly.BBox.min, poly.BBox.max);
        List<CH2D_P_Index> int_vertices = AddPolygonPreprocess(poly.vertices); // Регистрация всех вершин
        string n = "poly so far: "; for (int i = 0; i < int_vertices.Count; i++) n += int_vertices[i] + " "; Debug.Log(n);
        //for (int i = 0; i < poly.vertices.Count; i++) int_vertices.Add(AddPointIfNew(poly.vertices[i])); 

        // Встройка всех коллинеарных вершин 
        List<int> p_overlap = GetBBoxOverlapList(new LipomaBounds(poly.BBox));
        for (int i = 0; i < p_overlap.Count; i++)
        {
            Incorporate_Bvertice_To_PolyA(int_vertices, this.polygons[p_overlap[i]].vertices); // Тут может начаться бесконечный цикл если есть дубликаты точек внутри списка вершин
            //MutualVerticeIncorporation(vertices, this.polygons[p_overlap[i]].vertices);
        }

        // Встройка всех пересечений (добавляет новые точки к существующим полигонам)
        for (int i = 0; i < p_overlap.Count; i++)
            PolyPolyOnlineIntersectionOnesided(int_vertices, this.polygons[p_overlap[i]].vertices);

        int_poly.RecalculateBBox(int_vertices.Select(int_v => vertices[int_v]).ToList());
        // Встройка новых вершин-пересечений с предыдущего шага в старые полигоны
        for (int i = 0; i < p_overlap.Count; i++) {
            Incorporate_Bvertice_To_PolyA(this.polygons[p_overlap[i]].vertices, int_vertices);
            //MutualVerticeIncorporation(this.polygons[p_overlap[i]].vertices, vertices);
            Debug.Log(p_overlap[i]);
        }
        //Все прошло хорошо, состалось только GH-подразбить полигоны
        // Новый полигон - основной. Производится итеративный GH, получается три области: old-only, new-old пересечение, new-only.
        // new-only уходит на следующий шаг итерации, если еще есть старые полигоны. 
        // Остаток new-only добавляется как новый полигон, если он не нулевой.
        Debug.Log(p_overlap.Count); // Тут нужно сделать switch для выбора режима добавления полигона
        List<CH2D_Polygon> outsides;// = CutPolygonAgainsManyMainMonolith(int_vertices, int_poly.BBox, p_overlap);
        
        switch (polygon_add_mode)
        {
            case PolygonAddMode.Monolith:
                outsides = CutPolygonAgainsManyMainMonolith(int_vertices, int_poly.BBox, p_overlap);
                if (outsides == null)
                {
                    // Все пошло плохо по той или иной причине.
                    Debug.Log("<b><color=red>Outsides list returned as NULL, something went wrong!</color></b>");
                    PurgeUnusedPoints();
                    return;
                }
                Debug.Log(outsides.Count);
                // УДАЛЕНИЕ СТАРЫХ ПОЛИГОНОВ
                p_overlap.Sort();   // Сортировка бесполезна, т.к. полигоны добавляются в возрастающем порядке. Но лучше перебздеть
                p_overlap.Reverse();
                for (int i = 0; i < p_overlap.Count; i++)
                {
                    Debug.Log(p_overlap[i]);
                    if (p_overlap[i] == -1) continue;
                    bool success = SoftDeletePolygon(this.polygons[p_overlap[i]]);
                    Debug.Log(success);
                }
                int_poly.vertices = new List<CH2D_P_Index>(int_vertices);
                int_poly.initialized = true;
                this.DangerousAddPolygon(int_poly);
                break;
            case PolygonAddMode.FillHoles:
                int_poly.vertices = int_vertices;
                int_poly.initialized = true;
                outsides = CutPolygonAgainsManyMainFillHoles(int_poly, p_overlap);
                if (outsides == null)
                {
                    // Все пошло плохо по той или иной причине.
                    Debug.Log("<b><color=red>Outsides list returned as NULL, something went wrong!</color></b>");
                    PurgeUnusedPoints();
                    return;
                }
                Debug.Log(outsides.Count);
                break;
            default:
                return;
        }
        // ДОБАВЛЕНИЕ НОВЫХ ПОЛИГОНОВ
        for (int i = 0; i < outsides.Count; i++)
        {
            this.DangerousAddPolygon(outsides[i]);
            string q = ""; for (int j = 0; j < outsides[i].vertices.Count; j++) q = q + outsides[i].vertices[j].ToString() + " "; Debug.Log(q);
            Debug.Log(outsides[i].BBox.min + " " + outsides[i].BBox.max);
        }

        FillTheHoles(int_vertices, p_overlap);
        
        
        // ПЕРЕРАСЧЕТ ГРАФА / GRAPH RECALCULATION
        this.ConnectionsRecalculateAll(); 
    }

    public bool DangerousAddPolygon(CH2D_Polygon poly)
    {
        if (poly.initialized == false)
        {
            List<Vector2> v = poly.vertices.Select(int_v => vertices[int_v]).ToList();
            poly.RecalculateBBox(v);
            poly.RecalculateOrientation(v);
            poly.RecalculateConvexity(v);
            poly.initialized = true;
        }
        this.polygons.Add(poly);
        this.connections.AddPoint();
        return true;
    }

    public void CutPolygonAgainsManyMainCutter(List<CH2D_P_Index> vertices)
    {   // Отличается от CutPolygonAgainsManyMainMonolith тем что новый полигон работает как ножик и подразделяет оригинальные полигоны 
        List<CH2D_Polygon> Aonly = new List<CH2D_Polygon>();
        Aonly.Add(new CH2D_Polygon(vertices));
        Debug.Log("Not implemented, Cut Polygon Agains Many that uses main polygon as a divider to subdivide other polygons");
        List<CH2D_Polygon> Bonly_return = new List<CH2D_Polygon>(); // Эти трогать не надо, в них попадают огрызки не являющиеся А.
        List<CH2D_Polygon> Inter_return = new List<CH2D_Polygon>(); // Они просто меняться не должны особо
        List<CH2D_Polygon> Union_return = new List<CH2D_Polygon>(); // В Union хранятся дырки-Clockwise, оболочка-CCW отбрасывается. Их надо-будет потом как-то обработать чтобы лишних дырок не наделать
        int safety = 0; int safety_margin = 25;
        while (Aonly.Count > 0 && safety < safety_margin) { safety += 1;
            //List<Vector2> v_vertices = int_vertices.Select(v => this.vertices[v]).ToList();
        }
    }
    public List<CH2D_Polygon> CutPolygonAgainsManyMainMonolith(List<CH2D_P_Index> AV, LipomaBounds ABBox, List<int> p_overlap)
    {   // Отличается от CutPolygonAgainsManyMainCutter тем что новый полигон остается неизменным. 
        List<Vector2> ap = AV.Select(v => this.vertices[v]).ToList();

        List<CH2D_Polygon> Bonly_return = new List<CH2D_Polygon>(); // Эти трогать не надо, в них попадают огрызки не являющиеся А.
        //List<CH2D_Polygon> Inter_return = new List<CH2D_Polygon>(); // Они просто меняться не должны особо
        //List<CH2D_Polygon> Union_return = new List<CH2D_Polygon>(); // В Union хранятся дырки-Clockwise, оболочка-CCW отбрасывается. Их надо-будет потом как-то обработать чтобы лишних дырок не наделать
        GHPolygonMerge.CutPolyIntSetting setting = new GHPolygonMerge.CutPolyIntSetting(Union: true, Inter: false, Aonly: false, Bonly: true);
        for (int i = 0; i < p_overlap.Count; i++)
        {
            List<Vector2> b_points = GetPolyVertices(p_overlap[i]);
            List<Pair> pairs = PolyPolySharedPoints(AV, this.polygons[p_overlap[i]].vertices, ABBox, this.polygons[p_overlap[i]].BBox);
            if (pairs.Count == 0)
                if (Poly2DToolbox.IsPointInsidePolygon(ap[0], b_points))
                {   // Полигон A полностью внутри полигона. Тут хз чего делать. 
                    return null;
                }

            (var union_potential_holes, var dummy_inter, var dummy_a, var only_b) = GHPolygonMerge.CutPolyInt(this.vertices, AV, this.polygons[p_overlap[i]].vertices, ap, b_points, pairs, setting);
            if (only_b == null) { p_overlap[i] = -1; continue; }
            Bonly_return.AddRange(only_b);

            for (int u_hole = 0; u_hole < union_potential_holes.Count; u_hole++)
            {
                bool local_isHole = Poly2DToolbox.IsClockwise(GetVertices(union_potential_holes[u_hole].vertices));
                Debug.Log("This part of the code adds holes that form as a result of two polygons as normal polygons oriented Counter Clockwise");
                if (local_isHole) { union_potential_holes[u_hole].vertices.Reverse(); Bonly_return.Add(union_potential_holes[u_hole]); }
            }

        }
        return Bonly_return;
    }

    public List<CH2D_Polygon> CutPolygonAgainsManyMainFillHoles(CH2D_Polygon polyA, List<int> p_overlap)
    {   // Отличается от CutPolygonAgainsManymonolith тем что заполняет пусторты и оставляет все существующие полигоны неизменными
        List<CH2D_Polygon> a_only = new List<CH2D_Polygon>(); a_only.Add(polyA);
        List<CH2D_Polygon> undivided = new List<CH2D_Polygon>(); // Здесь будут храниться полигоны которые более не могут быть поделены

        List<List<Vector2>> p_overlap_vertice_cash = new List<List<Vector2>>(p_overlap.Count);  // Полигоны из p_overlap неизменны, поэтому я их сразу слеплю вместо того чтобы каждый раз пересобирать
        for (int i = 0; i < p_overlap.Count; i++) p_overlap_vertice_cash.Add(GetPolyVertices(p_overlap[i]));
        // Эту штуку можно "оптимизировать" если полигоны объединить в несколько больших полигонов
        Debug.Log("START: "  + DebugUtilities.DebugListString(polyA.vertices.ToArray()));
        int safety = 0; int safety_margin = 9;
        while (a_only.Count != 0 & safety < safety_margin)   // Этот цикл итеративно подразбивает A на Aonly и CW Union/Дырки 
        { safety += 1;
            int a_curr_index = a_only.Count - 1;
            CH2D_Polygon a_curr = a_only[a_curr_index];
            bool has_been_cut = false;
            Debug.Log("<color=orange>" + DebugUtilities.DebugListString(a_curr.vertices.ToArray()) + "</color>");
            for (int po = 0; po < p_overlap.Count; po++)
            {
                CH2D_Polygon p_over = polygons[p_overlap[po]];
                if (!a_curr.BBox.Intersects(p_over.BBox)) { Debug.Log("No BBox intersection, continue"); continue; }

                List<Pair> intersections = PolyPolySharedPoints(a_curr.vertices, p_over.vertices, a_curr.BBox, p_over.BBox);
                string ni_d = ""; for (int i = 0; i < intersections.Count; i++) ni_d += "(" + intersections[i].A + " " + intersections[i].B + "), ";
                Debug.Log(intersections.Count + " " + ni_d);
                string p1 = ""; foreach (var item in GetVertices(a_curr.vertices)) p1 += "new Vector2(" + item.x + ", " + item.y + "),"; Debug.Log(p1);
                string p2 = ""; foreach (var item in p_overlap_vertice_cash[po])   p2 += "new Vector2(" + item.x + ", " + item.y + "),"; Debug.Log(p2);


                Debug.Log(DebugUtilities.DebugListString(GetVertices(a_curr.vertices).ToArray()));
                Debug.Log(DebugUtilities.DebugListString(p_overlap_vertice_cash[po].ToArray()));
                (int[] Ainter, int[] Binter, GHPolygonMerge.EdgeSide[] Amark, GHPolygonMerge.EdgeSide[] Bmark) = 
                    GHPolygonMerge.GetIntersectionAndMarkings(a_curr.vertices, p_over.vertices, GetVertices(a_curr.vertices), p_overlap_vertice_cash[po], intersections);

                GHPolygonMerge.CutPolyIntSetting setting = new GHPolygonMerge.CutPolyIntSetting( Union: true, Inter: false, Aonly: false, Bonly: false);
                if (!GHPolygonMerge.IsPolygonIntersectionOfType(Amark, PolygonIntersection.OutsideAny)) { // If intersects or inside, get Aonly, otherwise there is no need for it 
                    setting = new GHPolygonMerge.CutPolyIntSetting(Union: true, Inter: false, Aonly: true, Bonly: false);
                    has_been_cut = true;
                }

                DebugUtilities.DrawPolygon(GetVertices(a_curr.vertices), DebugUtilities.RainbowGradient_Red2Violet(safety, safety_margin), 2f * safety);
                
                (var union_potential_holes, var dummy_inter, var only_a, var dummy_b) =
                    GHPolygonMerge.CutPolyInt(vertices, a_curr.vertices, p_over.vertices, GetVertices(a_curr.vertices), p_overlap_vertice_cash[po], intersections, setting, Ainter, Binter, Amark, Bmark);
                if (only_a == null) { Debug.Log("<b><color=red>only A is equal to null, continuing. It should not be equal to Null, could be a mistake</color></b>"); continue; }
                Debug.Log("<b><color=white>Union polys: " + union_potential_holes.Count + " a_only polys: " + only_a.Count + "</color></b> ");
                for (int u_hole = 0; u_hole < union_potential_holes.Count; u_hole++)
                {
                    bool local_isHole = Poly2DToolbox.IsClockwise(GetVertices(union_potential_holes[u_hole].vertices));
                    if (local_isHole) 
                    { 
                        union_potential_holes[u_hole].vertices.Reverse();
                        union_potential_holes[u_hole].RecalculateBBox(GetVertices(union_potential_holes[u_hole].vertices));
                        a_only.Add(union_potential_holes[u_hole]);
                        Debug.Log("Added new hole!");
                    }
                }
                for (int i = 0; i < only_a.Count; i++)
                {
                    only_a[i].RecalculateBBox(GetVertices(only_a[i].vertices));
                    a_only.Add(only_a[i]);//Debug.Log(DebugUtilities.DebugListString(only_a[i].vertices.ToArray()));
                }
                if (has_been_cut) break;
            }
            if (has_been_cut)
            {
                a_only.RemoveAt(a_curr_index);
            } else
            {
                undivided.Add(a_only[a_curr_index]);
                a_only.RemoveAt(a_curr_index);
            }
            Debug.Log(a_only.Count + " " + undivided.Count);
        }
        Debug.Log("END: "  + undivided.Count + " safety: " + safety);

        return undivided;
    }

    // Новый полигон касается с соседними полигонами. Каждый соседний полигон должен проложить путь к другому полигону-соседу. В результате должны быть найдены петли. 
    // Каждая петля состоит начального соседнего полигона, конечного соседнего полигона, и новодобавленного полигона. Новодобавленный связан с соседями
    // Между начальным и конечным соседями может быть любое количесвто полигонов, формирующих петлю. И внутри этой петли может быть пустое пространство, которое должно превратиться в полигон-дырку.
    public void FillTheHoles(List<CH2D_P_Index> vertices, List<int> neighbours)
    {   // Так, сейчас нихуя не работает, поэтому надо просто убедиться что:
        // пути могут прокладываться от соседей к соседям (сделано)
        // Нужно идентифицировать путь-дупликат, который накладывается на все остальные
        Debug.Log("Функция FillTheHoles сейчас не работает. Прежде чем она будет работать, нужно реализовать Astar поиск пути, с расстоянием между центрами нод и определением площади результируемой дыры");
        if (this.connections == null) { Debug.Log("Нетоу графа связей, невозможно провести операцию"); return; }
        if (neighbours.Count == 1) { Debug.Log("Связь лишь с одним полигоном, дырки невозможны"); return; }

        List<List<int>> loops = new List<List<int>>();
        for (int i = 0; i < neighbours.Count; i++)
        {
            List<int> loop = GraphToolbox.FindPathNaive(connections, neighbours[i], neighbours[(i + 1) % neighbours.Count]); 
            if (loop != null)  loops.Add(loop);
        }
        // А тут хрень происходит! Так как связи отсортированы по индексу, а не по часовой стрелке, то метод поиска пути к "соседу" не применим
        // Последовательные индексы полигонов не гарантируют соседство этих полигонов.
        // Сейчас код рисует бессмысленные маршруты вместо минимальных петель

        /* Debug things here. I shall uncomment them once i return to work here
        string n = "<b><color=green>All identified loops: </color></b> \n";
        for (int i = 0; i < loops.Count; i++) n += "(Length: " + loops[i].Count + ") " + DebugUtilities.DebugListString(loops[i].ToArray()) + "\n";
        Debug.Log(n);
        for (int i = 0; i < loops.Count; i++)
        {
            List<Vector2> centers = new List<Vector2>();
            for (int j = 0; j < loops[i].Count; j++) centers.Add(this.polygons[loops[i][j]].BBox.center);
            DebugUtilities.DrawPath(centers, DebugUtilities.RainbowGradient_Red2Violet(i, loops.Count - 1), 3f + 3f * i);
        }*/

        


        // Поиск самой большой петли и удаление.  Самая большая петля вероятно проходит через ноды, через которые прошли все другие 
        if (loops.Count < 2) return;
        int largest_loop_index = 0; int largest_loop_length = loops[0].Count;
        for (int i = 1; i < loops.Count; i++)
            if (loops[i].Count > largest_loop_length) { largest_loop_index = i; largest_loop_length = loops[i].Count; }
        loops.RemoveAt(largest_loop_index);


    }



    public void PolyMergeDelegate(int A, int B)
    {
        List<Pair> pairs = PolyPolySharedPoints(polygons[A].vertices, polygons[B].vertices, polygons[A].BBox, polygons[B].BBox);
        GHPolygonMerge.CutPolyInt(this.vertices, polygons[A].vertices, polygons[B].vertices, GetPolyVertices(A), GetPolyVertices(B), pairs, GHPolygonMerge.default_setting);
    }

    /// <summary>
    /// Проверяет, какие полигоны пересекаются с точкой. Проверяет, какая грань полигона содержит точку. Точка может быть:<br/>
    /// - Одной из вершин полигона, будет возвращен index совпадающей вершины<br/>
    /// - Лежать отдельно, точка будет добавлена в список, будет возвращен ее index<br/>
    /// - Лежать на одной из граней, точка будет добавлена в список, будет возвращен ее index, оба полигона делящие эту грань будут обновлены чтобы включить эту точку
    /// </summary>
    /// <param name="point"></param>
    public CH2D_P_Index AddPoint(Vector2 point)
    {
        if (this.vertices.Count + 1 >= MaxVertices) throw new Exception("Больше вершин чем разрешено");
        this.vertices.Add(point);
        //Debug.Log(vertices.Count);
        return new CH2D_P_Index(this.vertices.Count - 1);
    }

    public CH2D_P_Index AddPointIfNew(Vector2 point)
    {
        for (int i = 0; i < this.polygons.Count; i++)
        {
            if (!this.polygons[i].BBox.Contains(point)) continue;
            for (int j = 0; j < this.polygons[i].vertices.Count; j++)
            {
                CH2D_P_Index p = this.polygons[i].vertices[j];
                if (Poly2DToolbox.PointSimilarity(point, this.vertices[p])) return p;
            }
        }
        //if (this.polygons.Count == 0) for (int i = 0; i < this.vertices.Count; i++) if (Poly2DToolbox.PointSimilarity(point, this.vertices[i])) return new CH2D_P_Index(i);

        return AddPoint(point);
    }
    /// <summary>
    /// iteratively checks every point ana adds only unique ones. 
    /// <br/>(!) Since i had forbidden for polygons to have duplicate points this function is virtually useless.
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    public List<CH2D_P_Index> AddPolygonPreprocess(List<Vector2> vertices)
    {
        List<Vector2> unique = new List<Vector2>(vertices.Count);
        Span<int> index = stackalloc int[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            int similar = -1;
            for (int u = 0; u < unique.Count; u++)
            {
                if (unique[u] == vertices[i]) { similar = u; break; }
            }
            if (similar == -1) unique.Add(vertices[i]);
            similar = similar == -1 ? unique.Count - 1 : similar;
            index[i] = similar;
        }
        List<CH2D_P_Index> unique_pindex = new List<CH2D_P_Index>();
        for (int i = 0; i < unique.Count; i++) unique_pindex.Add(AddPointIfNew(unique[i]));

        List<CH2D_P_Index> pindex = new List<CH2D_P_Index>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++) pindex.Add(unique_pindex[index[i]]);

        /*string n = ""; for (int i = 0; i < vertices.Count; i++) n += vertices[i] + " "; Debug.Log(n);
        n = ""; for (int i = 0; i < unique.Count; i++) n += unique[i] + " "; Debug.Log(n);
        n = ""; for (int i = 0; i < index.Length; i++) n += index[i] + " "; Debug.Log(n);
        n = ""; for (int i = 0; i < pindex.Count; i++) n += pindex[i] + " "; Debug.Log(n);*/
        return pindex;
    }
    // Кажется теперь это бесполезная функция, то что она делает решается при помощи MutualVerticeIncorporation(A, B)
    public CH2D_P_Index AddPointIfNewConvoluted(Vector2 point)
    {
        List<(int poly, Nullable<CH2D_P_Index> a, Nullable<CH2D_P_Index> b)> polys = DoesChunkHavePoint(point);
        if (polys.Count == 0) { Debug.Log("Добавлнеа НЕсуществующая точка"); return AddPoint(point); } // Нет похожего, создание новой точки
        // Похожая точка уже существует
        for (int i = 0; i < polys.Count; i++) // Похожая точка содержится в полигоне, возвращаем ее индекс
        {
            if (polys[i].b == null) { Debug.Log("Добавлена существующая точка точка " + polys[i].a.Value); return polys[i].a.Value; }
        }
        // Точка сидит на границе, тут редактируются один или два полигона с общей гранью
        Debug.Log("Добавлена точка на пересечении");
        CH2D_P_Index new_p = AddPoint(point);
        for (int i = 0; i < polys.Count; i++)
        {
            polygons[polys[i].poly].InsertPointIntoPolygon(new_p, polys[i].a.Value, polys[i].b.Value);
        }
        return new_p;
    }
    // ======================================
    //  ГРАФОМАНИЯ
    // ======================================

    public void ConnectionsCheckList()
    {
        bool okay = this.connections.ValidityCheck();
        if (! okay) this.ConnectionsRecalculateAll();
    }
    public void ConnectionsRecalculateAll()
    {
        this.connections = new GraphDynamicList();
        for (int i = 0; i < polygons.Count; i++) connections.AddPoint();
        for (int i = 0; i < polygons.Count; i++)
        {
            List<int> valid_neighbours = ConnectionsGetConnected(i);
            DebugUtilities.DebugList(valid_neighbours);
            for (int vn = 0; vn < valid_neighbours.Count; vn++)
            {
                connections.SetValue(true, i, valid_neighbours[vn]);
            }
        }
    }
    private List<int> ConnectionsGetConnected(int polygon)
    {
        List<int> overlaps = GetBBoxOverlapList(this.polygons[polygon].BBox);
        List<int> valid_neighbours = new List<int>();
        for (int i = 0; i < overlaps.Count; i++)
        {
            (bool valid, CH2D_P_Index dummy1, CH2D_P_Index dummy2) = GetSharedEdge(polygon, overlaps[i]);
            if (valid) valid_neighbours.Add(overlaps[i]);
        }
        return valid_neighbours;
    }
    public void ConnectionsRecalculateSingle(int polygon)
    {
        this.connections.DumpRow(polygon);
        List<int> valid_neighbours = ConnectionsGetConnected(polygon);
        for (int vn = 0; vn < valid_neighbours.Count; vn++) connections.SetValue(true, polygon, valid_neighbours[vn]);
    }

    public List<int> PathfindWithinChunk(int A, int B) { return GraphToolbox.FindPathNaive(this.connections, A, B); }


    // ======================================
    //  ПРОВЕРКИ
    // ======================================
    // Проверяет полигоны, чьи BBox содержат точку. 
    // Проверяет все грани этих полигонов, находит грани между которыми эта точка лежит.
    // Так как это Vector2 точка, а не Ch2D_P_index, то эта точка нова для полигонов
    // Следовательно она находится либо на границе с один полигоном, либо на границе между двемя полигонами.
    // Вообще можно вместо поиска внутри полигона 
    private List<(int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>)> DoesChunkHavePoint(Vector2 point)
    {   
        List<(int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>)> borderers = new List<(int, Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>)>(2);
        for (int i = 0; i < polygons.Count; i++)
        {
            if (!polygons[i].BBox.Contains(point)) continue;
            (Nullable<CH2D_P_Index> a, Nullable<CH2D_P_Index> b) = PointOnBorder(i, point);
            if (a == null) continue; // Тут может быть лишь два варианта: либо null+null, либо a+b.
            if (borderers.Count == 2) break;
            borderers.Add((i, a, b));
        }
        return borderers;
    }
    public (Nullable<CH2D_P_Index>, Nullable<CH2D_P_Index>) PointOnBorder(int poly, Vector2 point)
    {
        Nullable<CH2D_P_Index> a = null; Nullable<CH2D_P_Index> b = null;
        List<Vector2> border = GetPolyVertices(poly);
        (int int_a, int int_b) = Poly2DToolbox.PointOnBorder(point, border);
        if (int_a == -1) return (a, b);
        a = polygons[poly].vertices[int_a];
        b = int_b == -1 ? null : polygons[poly].vertices[int_b];
        return (a, b);
    }
    public List<int> GetBBoxOverlapList(LipomaBounds BBox)
    {   // Возвращает полигоны, чьи BBox пересекаются с этим BBox. Сложность O(N)
        List<int> result = new List<int>();
        for (int i = 0; i < polygons.Count; i++) if (polygons[i].BBox.Intersects(BBox)) result.Add(i);
        return result;
    }

    public List<Vector2> GetPolyVertices(int PolyID)
    {
        List<Vector2> points = new List<Vector2>(polygons[PolyID].vertices.Count);
        for (int i = 0; i < polygons[PolyID].vertices.Count; i++)
            points.Add(this.vertices[polygons[PolyID].vertices[i]]);
        return points;
    }
    public List<Vector2> GetVertices(List<CH2D_P_Index> indices) 
    {
        List<Vector2> points = new List<Vector2>(indices.Count);
        for (int i = 0; i < indices.Count; i++) {
            if (indices[i] < 0 | indices[i] >= this.vertices.Count) throw new Exception("Indices that you provided do not exist in the chunk");
            points.Add(this.vertices[indices[i]]);
        }
        return points;
    }
    public Poly2D GetPoly2D(int PolyID)
    {   // Проблема централизованного хранения вершин - нужда пересобирать полигоны для операций
        Poly2D toReturn = new Poly2D();
        toReturn.vertices = GetPolyVertices(PolyID);
        toReturn.BBox = this.polygons[PolyID].i_bounds;
        toReturn.isHole = this.polygons[PolyID].isHole;
        toReturn.convex = this.polygons[PolyID].convex;
        return toReturn;
    }

    public int GetPolygonNeighbouringEdge(int poly, CH2D_P_Index A, CH2D_P_Index B)
    {   
        for (int i = 0; i < polygons.Count; i++)
        {
            if (poly == i) continue;
            if (!this.polygons[poly].BBox.Intersects(this.polygons[i].BBox)) continue;
            if (DoPolygonsShareEdge(poly, i, A, B)) return i;
        }
        return -1;
    }
    public bool DoPolygonsShareEdge(int polyA, int polyB, CH2D_P_Index start, CH2D_P_Index end)
    {   // Сравнивает индексы вершин. Если их значение isHole равны, то грани смотрят в противоположные стороны, если isHole разнятся - то грани смотрят в одну сторону
        (bool a_success, int a_prev_i, CH2D_P_Index a_prev_v, int a_curr_i, CH2D_P_Index a_curr_v, int a_next_i, CH2D_P_Index a_next_v) = GetSurrounds(polyA, start);
        if (!a_success) return false;
        if (a_next_v != end) return false;
        (bool b_success, int b_prev_i, CH2D_P_Index b_prev_v, int b_curr_i, CH2D_P_Index b_curr_v, int b_next_i, CH2D_P_Index b_next_v) = GetSurrounds(polyB, start);
        if (!b_success) return false;
        if (this.polygons[polyA].isHole == this.polygons[polyB].isHole)
            return b_prev_v == end;
        else
            return b_next_v == end;
    }
    /// <summary>
    /// Волшебная функция, которая находит точку Point в полигоне, и возвращает ее index, а также индексы и значения своих соседей.
    /// </summary>
    /// <returns>success - whether Point belongs to Poly<br/>prev_i, curr_i, next_i - индексы предыдущей, этой (Point), следующей точек<br/>prev_v, curr_v, next_v - значения предыдущей, этой (Point), следующей точек</returns>
    public (bool success, int prev_i, CH2D_P_Index prev_v, int curr_i, CH2D_P_Index curr_v, int next_i, CH2D_P_Index next_v) GetSurrounds(int Poly, CH2D_P_Index Point)
    {
        int curr_i = -1; int PVCount = this.polygons[Poly].vertices.Count;
        for (int i = 0; i < PVCount; i++)
            if (this.polygons[Poly].vertices[i] == Point) { curr_i = i; break; }
        if (curr_i == -1) return (false, -1, new CH2D_P_Index(), -1, new CH2D_P_Index(), -1, new CH2D_P_Index());
        return GetSurrounds(Poly, curr_i);
    }
    public (bool success, int prev_i, CH2D_P_Index prev_v, int curr_i, CH2D_P_Index curr_v, int next_i, CH2D_P_Index next_v) GetSurrounds(int Poly, int curr_i)
    {
        int PVCount = this.polygons[Poly].vertices.Count;
        int prev_i = (curr_i - 1 + PVCount) % PVCount;
        int next_i = (curr_i + 1) % PVCount;
        return (true,
            prev_i, new CH2D_P_Index(this.polygons[Poly].vertices[prev_i]),
            curr_i, new CH2D_P_Index(this.polygons[Poly].vertices[curr_i]),
            next_i, new CH2D_P_Index(this.polygons[Poly].vertices[next_i]) );
    }
    /// <summary>
    /// Предполагается что будут даны полигоны, полученные в результате проверки BBoxOverlapList пересечения или же 
    /// </summary>
    public List<int> GetPolygonsOwningPoint(CH2D_P_Index point, List<int> loc_polygons)
    {
        List<int> to_return = new List<int>();
        for (int i = 0; i < loc_polygons.Count; i++)
        {
            CH2D_Polygon p = this.polygons[loc_polygons[i]];
            if (p.vertices.Contains(point)) to_return.Add(i);
        }
        return to_return;
    }
    /// <summary>
    /// This function is A-centric; it returns edge AB that is found in A. So in B this edge will be BA.<br/>
    /// If B.isHole and A.isHole are different, then the edge will be AB for both of them<br/>
    /// Also, it assumes that polygons touch, but not intersect.
    /// </summary>
    public (bool found, CH2D_P_Index A, CH2D_P_Index B) GetSharedEdge(int polyA, int polyB)
    {
        LipomaBounds boundsA = this.polygons[polyA].BBox;
        LipomaBounds boundsB = this.polygons[polyB].BBox;
        if (!boundsA.Intersects(boundsB)) return (false, new CH2D_P_Index(0), new CH2D_P_Index(0));
        List<CH2D_Edge> B_edges = EdgesInsideBounds(this.polygons[polyB].vertices, boundsA);
        List<CH2D_Edge> A_edges = EdgesInsideBounds(this.polygons[polyA].vertices, boundsB);
        if (this.polygons[polyA].isHole == this.polygons[polyB].isHole) {
            for (int a = 0; a < A_edges.Count; a++)
                for (int b = 0; b < B_edges.Count; b++)
                    if (A_edges[a].A == B_edges[b].B & A_edges[a].B == B_edges[b].A) return (true, A_edges[a].A, A_edges[a].B);
        } else {
            for (int a = 0; a < A_edges.Count; a++)
                for (int b = 0; b < B_edges.Count; b++)
                    if (A_edges[a].A == B_edges[b].A & A_edges[a].B == B_edges[b].B) return (true, A_edges[a].A, A_edges[a].B);
        }
        return (false, new CH2D_P_Index(0), new CH2D_P_Index(0));
    }
    /// <summary>
    /// This function returns chains of consequent edges, these chains are owned by both polygons.  <br/>
    /// This function is useful when polygons are not convex, and may have more than one contact point with each other<br/>
    /// It returns a list of lists, it forms said chains. Also it will return false+null if there are no chains
    /// </summary>
    public (bool found, List<List<CH2D_Edge>> continuities) GetSharedEdgeContinuities(int polyA, int polyB)
    {   // Эта штука возвращает цепочки из граней, которыми владеют оба полигона.
        LipomaBounds boundsA = this.polygons[polyA].BBox;
        LipomaBounds boundsB = this.polygons[polyB].BBox;
        if (!boundsA.Intersects(boundsB)) return (false, null);
        List<CH2D_Edge> B_edges = EdgesInsideBounds(this.polygons[polyB].vertices, boundsA);
        List<CH2D_Edge> A_edges = EdgesInsideBounds(this.polygons[polyA].vertices, boundsB);
        List<CH2D_Edge> mutual_edges = new List<CH2D_Edge>();
        if (this.polygons[polyA].isHole == this.polygons[polyB].isHole) {
            for (int a = 0; a < A_edges.Count; a++)
                for (int b = 0; b < B_edges.Count; b++)
                    if (A_edges[a].A == B_edges[b].B & A_edges[a].B == B_edges[b].A) mutual_edges.Add(A_edges[a]);
        } else {
            for (int a = 0; a < A_edges.Count; a++)
                for (int b = 0; b < B_edges.Count; b++)
                    if (A_edges[a].A == B_edges[b].A & A_edges[a].B == B_edges[b].B) mutual_edges.Add(A_edges[a]);
        }
        if (mutual_edges.Count == 0) return (false, null);
        List<List<CH2D_Edge>> mec = new List<List<CH2D_Edge>>();    // mutual edge chains
        mec.Add(new List<CH2D_Edge>());
        mec[0].Add(mutual_edges[0]);
        for (int i = 1; i < mutual_edges.Count; i++)
        {   // No need to do any fancy as A_edges come in order, therefore they can be chained together in order as well
            if (mec.Last().Last().B == mutual_edges[i].A) mec.Last().Add(mutual_edges[i]);
            else {
                mec.Add(new List<CH2D_Edge>());
                mec.Last().Add(mutual_edges[i]);
            }
        }
        /*for (int chain = 0; chain < mec.Count; chain++)
            for (int e = 0; e < mec[chain].Count; e++)
                DebugUtilities.DebugDrawLine(this.vertices[mec[chain][e].A], this.vertices[mec[chain][e].B], DebugUtilities.RainbowGradient_Red2Violet(chain, mec.Count - 1), 5f );
        */
        return (true, mec);
    }
    // ==============================================================
    // DRAWING, DEBUGGING
    // ==============================================================
    public void DebugDrawSelf()
    {
        foreach (var poly in polygons)
        {
            for (int i = 0; i < poly.vertices.Count - 1; i++)
                Debug.DrawLine(this.vertices[poly.vertices[i]], this.vertices[poly.vertices[i + 1]], Color.blue);
            Debug.DrawLine(this.vertices[poly.vertices[poly.vertices.Count - 1]], this.vertices[poly.vertices[0]], Color.blue);
        }
    }

    public void HandlesDrawSelf(bool draw_hierarchy)
    {
        Color tmp = Handles.color;
        Handles.color = Color.blue;

        //Debug.Log(polygons.Count);
        for (int i = 0; i < this.polygons.Count; i++)
            HandlesDrawPolyOutline(i, Color.blue);
        if (draw_hierarchy) HandlesDrawConnections();
        
        Handles.color = tmp;
    }
    public void HandlesDrawPolyOutline(int p, Color color) { DebugUtilities.HandlesDrawPolygon(GetPolyVertices(p), color, false); }
    public void HandlesDrawPolyOutlineDirected(int p, Color filled_color, Color hole_color) { DebugUtilities.HandlesDrawPolygon(GetPolyVertices(p), this.polygons[p].isHole ? hole_color : filled_color, true); }
    public void HandlesDrawConnections()
    {
        if (!connections.ValidityCheck()) return;
        for (int p = 0; p < this.connections.vCount; p++)
        {
            List<int> neighbours = connections.GetSliceIDList(p);
            for (int n = 0; n < neighbours.Count; n++)
            {
                DebugUtilities.HandlesDrawLine(this.polygons[p].BBox.center, this.polygons[neighbours[n]].BBox.center, Color.yellow);
            }
        }
    }
    public void HandlesDrawPolyBBox(int p, Color color) { DebugUtilities.HandlesDrawRectangle(this.polygons[p].BBox.min, this.polygons[p].BBox.max, color); }
    public void HandlesDrawPolyPoints(int p, Color color) { for (int i = 0; i < this.polygons[p].vertices.Count; i++) DebugUtilities.HandlesDrawCross(this.vertices[this.polygons[p].vertices[i]], color); }
    public void DebugRainbowPolygon(int p, float time, float cone_width)
    {
        int p_count = this.polygons[p].vertices.Count;
        List<CH2D_P_Index> v = this.polygons[p].vertices;
        for (int i = 0; i < p_count; i++)
        {
            int j = (i + 1) % p_count;
            DebugUtilities.DebugDrawLine(this.vertices[v[i]], this.vertices[v[j]], DebugUtilities.PickGradient(i, p_count - 1, DebugUtilities.GradientOption.Rainbow_Red2Violet), time, cone_width);
        }
    }
    public void DebugDumpChunkData()
    {
        string ret = "DebugChunkData: PolyCount: " + this.polygons.Count + " PointCount: " + this.vertices.Count +  " \n";
        for (int i = 0; i < polygons.Count; i++)
        {
            ret += "(P" + i + ") ";
            if (polygons[i].vertices == null) throw new Exception("Полигон с index " + i + " не имеет списка вершин!");
            ret += " (VCount: " + polygons[i].vertices.Count + " ) ";
            ret += " { ";
            for (int v = 0; v < polygons[i].vertices.Count; v++)
            {
                ret += polygons[i].vertices[v] + " ";
            }
            ret += "} \n";
        }
        Debug.Log(ret);
        //Debug.Log("Chunk connections data:");
        this.connections.DumpSelf();
        Debug.Log((this.connections.vCount == this.polygons.Count) ? 
            "<color=green>Amount of polygons in chunk and amount of vertices in connection graph seem to match:\n chunk: " + this.polygons.Count + " graph: " + this.connections.vCount +  "</color>\n" :
            "<color=red>Amount of polygons in chunk and amount of vertices in connection graph does NOT seem to match!:\n chunk: " + this.polygons.Count + " graph: " + this.connections.vCount + "</color>\n");
    }
    // Идея в том чтобы найти все пересечения и добавить точки в список
    public void DebugGetIntersections(bool DrawIntersections, bool FindInnsAndOuts)
    {
        if (this.polygons.Count < 2) return;

        Incorporate_Bvertice_To_PolyA(this.polygons[0].vertices, this.polygons[1].vertices);
        Incorporate_Bvertice_To_PolyA(this.polygons[1].vertices, this.polygons[0].vertices);
        // Point incorporations
        List<CH2D_Intersection> intersections = GetPolyPolyIntersections(0, 1);
        Debug.Log(intersections.Count);
        if (DrawIntersections)
            for (int i = 0; i < intersections.Count; i++)
            {
                CH2D_Intersection ii = intersections[i];
                Debug.Log(ii.a_e1 + " " + ii.a_e2 + " " + ii.b_e1 + " " + ii.b_e2);
                DebugUtilities.DebugDrawCross(intersections[i].pos, Color.red, 10.0f);
            }
        if (!FindInnsAndOuts) return;

        List<Pair> pairs = new List<Pair>(intersections.Count);
        for (int i = 0; i < intersections.Count; i++)
        {
            pairs.Add(new Pair(intersections[i].a_e1, intersections[i].a_e2, false));
        }

        GHPolygonMerge.CutPolyInt(vertices, polygons[0].vertices, polygons[1].vertices, GetPolyVertices(0), GetPolyVertices(1), pairs, GHPolygonMerge.default_setting);
    }


    // Incorporate collinear vertices
    // Совпадающие вершины должны 
    public void Incorporate_Bvertice_To_PolyA(List<CH2D_P_Index> a_v, List<CH2D_P_Index> b_v)
    {
        for (int a = 0; a < a_v.Count; a++)
        {
            CH2D_Edge ae = new CH2D_Edge(a_v[a], a_v[(a + 1) % a_v.Count]);
            for (int b = 0; b < b_v.Count; b++)
            {

                CH2D_P_Index bv = b_v[b];
                if (bv == ae.A | bv == ae.B) continue;
                
                bool success = Poly2DToolbox.PointBelongToLine2D(vertices[ae.A], vertices[ae.B], vertices[bv]);
                //Debug.Log(success + " " + bv + " " + ae.A + " " + ae.B);
                if (!success) continue;
                //CH2D_Polygon.InsertPointIntoPolygon(a_v, bv, ae.A, ae.B);// polygons[A].InsertPointIntoPolygon(bv, ae.A, ae.B);
                a_v.Insert(a + 1, bv);
                a = a - 1;
                break;
            }
        }
    }
    public void Incorporate_Bvertice_To_PolyA(int A, int B)
    {
        Incorporate_Bvertice_To_PolyA(this.polygons[A].vertices, this.polygons[B].vertices);
        this.polygons[A].RecalculateBBox(GetPolyVertices(A));
    }
    public void MutualVerticeIncorporation(List<CH2D_P_Index> a_v, List<CH2D_P_Index> b_v)
    {
        Incorporate_Bvertice_To_PolyA(a_v, b_v);
        Incorporate_Bvertice_To_PolyA(b_v, a_v);
    }
    public void MutualVerticeIncorporation(int a_i, int b_i)
    {
        MutualVerticeIncorporation(this.polygons[a_i].vertices, this.polygons[b_i].vertices);
    }
    public void PolyPolyIntersection(int A, int B)
    {
        List<CH2D_Intersection> intersections = GetPolyPolyIntersections(A, B);
        for (int i = 0; i < intersections.Count; i++)
        {
            CH2D_P_Index p_i = AddPointIfNew(intersections[i].pos);
            this.polygons[A].InsertPointIntoPolygon(p_i, intersections[i].a_e1, intersections[i].a_e2);
            this.polygons[B].InsertPointIntoPolygon(p_i, intersections[i].b_e1, intersections[i].b_e2);
        }
    }
    // Оба полигона должны существовать, тоесть эта штука неприменима во время добавления нового полигона, которого еще нет в спискe
    private List<CH2D_Intersection> GetPolyPolyIntersections(int a_p, int b_p)
    {
        return GetPolyPolyIntersections(this.polygons[a_p].vertices, this.polygons[b_p].vertices, this.polygons[a_p].BBox, this.polygons[b_p].BBox, a_p, b_p);
    }
    private List<CH2D_Intersection> GetPolyPolyIntersections(List<CH2D_P_Index> a_v, List<CH2D_P_Index> b_v, LipomaBounds a_bbox, LipomaBounds b_bbox, int a_i, int b_i)
    {
        List<CH2D_Intersection> intersections = new List<CH2D_Intersection>();

        List<CH2D_Edge> a_edges = EdgesInsideBounds(a_v, b_bbox);
        List<CH2D_Edge> b_edges = EdgesInsideBounds(b_v, a_bbox);

        for (int a = 0; a < a_edges.Count; a++)
        {
            for (int b = 0; b < b_edges.Count; b++)
            {
                CH2D_P_Index a1 = a_edges[a].A;
                CH2D_P_Index a2 = a_edges[a].B;
                CH2D_P_Index b1 = b_edges[b].A;
                CH2D_P_Index b2 = b_edges[b].B;
                //Debug.Log(a + " " + b + " " + a1 + " " + a2 + " " + b1 + " " + b2);
                if (!Poly2DToolbox.LineLineIntersection(this.vertices[a1], this.vertices[a2], this.vertices[b1], this.vertices[b2], out Vector2 inter, out float t)) continue;
                if (Poly2DToolbox.PointSimilarity(inter, this.vertices[b1]) | Poly2DToolbox.PointSimilarity(inter, this.vertices[b2])) continue;
                intersections.Add(new CH2D_Intersection(a_i, b_i, a1, a2, b1, b2, inter, t));
            }
        }
        return intersections;
    }
    // Делит полигон А об полигон В, добавляет в полигон А новые точки по мере поиска пересечений. 
    private void PolyPolyOnlineIntersectionOnesided(List<CH2D_P_Index> A, List<CH2D_P_Index> B)
    {
        string n = "New points added: ";
        for (int a = 0; a < A.Count; a++)
        {
            int a1 = A[a];
            int a2 = A[(a + 1) % A.Count];
            for (int b = 0; b < B.Count; b++)
            {
                int b1 = B[b];
                int b2 = B[(b + 1) % B.Count];
                //Debug.Log(a1 + " " + a2 + " " + b1 + " " + b2);
                if (!Poly2DToolbox.LineLineIntersection(this.vertices[a1], this.vertices[a2], this.vertices[b1], this.vertices[b2], out Vector2 inter, out float t)) { /*Debug.Log(inter);*/ continue; }
                if (Poly2DToolbox.PointSimilarity(inter, this.vertices[b1]) | Poly2DToolbox.PointSimilarity(inter, this.vertices[b2])) { /*Debug.Log("similar to B");*/ continue; }
                if (Poly2DToolbox.PointSimilarity(inter, this.vertices[a1]) | Poly2DToolbox.PointSimilarity(inter, this.vertices[a2])) { /*Debug.Log("similar to A");*/ continue; }
                CH2D_P_Index np = AddPoint(inter);

                A.Insert(a + 1, np);
                n += "( " + np + " " + inter + " ) ";
                a--;
                break;
            }
        }

    }
    // Overlap, Shared, Общие, Пересечение, Cross
    /// <summary>
    /// Returns paitrs of points from polygon A and polygon B that are the same point.<br/>
    /// First it finds in O(2N) time all vertices inside each other's BBoxes, then does O(N^2) similarity check on the remaining A and B vertices
    /// </summary>
    public List<Pair> PolyPolySharedPoints(List<CH2D_P_Index> polyA, List<CH2D_P_Index> polyB, LipomaBounds Abox, LipomaBounds Bbox)
    {
        Debug.Log("Abox: " + Abox.min + " " + Abox.max + " Bbox: " + Bbox.min + " " + Bbox.max);
        List<int> p_a = PointsInsideBoundsInt(polyA, Abox); // Тут опечатка, Abox и Bbox надо поменять местами. Сейчас это бесполезные функции. Еще мне кажется что они Bounds не являются инклюзивными на конце
        List<int> p_b = PointsInsideBoundsInt(polyB, Bbox);
        
        Debug.Log(DebugUtilities.DebugListString(polyA.ToArray()));
        Debug.Log(DebugUtilities.DebugListString(polyB.ToArray()));
        Debug.Log(DebugUtilities.DebugListString(p_a.ToArray()));
        Debug.Log(DebugUtilities.DebugListString(p_b.ToArray()));
        List<Pair> pairs = new List<Pair>();
        for (int a = 0; a < p_a.Count; a++)
            for (int b = 0; b < p_b.Count; b++)
                if (polyA[p_a[a]] == polyB[p_b[b]]) pairs.Add(new Pair(p_a[a], p_b[b], false));
        return pairs;
    }
    private List<int> PointsInsideBoundsInt(List<CH2D_P_Index> polyA, LipomaBounds bounds)
    {   // возвращает индексы точек внутри BBox
        List<int> points = new List<int>();
        for (int i = 0; i < polyA.Count; i++)
            if ( bounds.Contains(this.vertices[polyA[i]])) points.Add(i);
            //if (BoundsMathHelper.InclusiveContains(bounds, this.vertices[polyA[i]])) points.Add(i);
        return points;
    }
    private List<CH2D_P_Index> PointsInsideBounds(List<CH2D_P_Index> polyA, Bounds bounds)
    {
       return polyA.FindAll(p => bounds.Contains(this.vertices[polyA[p]]));
    }
    private List<CH2D_Edge> EdgesInsideBounds(List<CH2D_P_Index> polyA, LipomaBounds bounds)
    {
        List< CH2D_Edge > edges = new List<CH2D_Edge>();
        int c = polyA.Count;
        for (int i = 0; i < c; i++)
        {
            int j = (i + 1) % c;
            if (!BoundsMathHelper.DoesLineIntersectBoundingBox2D(this.vertices[polyA[i]], this.vertices[polyA[j]], bounds.min, bounds.max)) continue;
            edges.Add(new CH2D_Edge(polyA[i], polyA[j]));
        }
        return edges;
        //return polyA.FindAll(p => BoundsMathHelper.DoesLineIntersectBoundingBox2D(this.vertices[p], this.vertices[(p+1)%c], bounds));
    }
    private List<CH2D_Edge> EdgesInsideBounds(int p_i, Bounds bounds)
    {
        List<CH2D_Edge> edges = new List<CH2D_Edge>();
        int c = polygons[p_i].vertices.Count;
        for (int i = 0; i < c; i++)
        {
            int j = (i + 1) % c;
            if (!BoundsMathHelper.DoesLineIntersectBoundingBox2D(this.vertices[polygons[p_i].vertices[i]], this.vertices[polygons[p_i].vertices[j]], bounds.min, bounds.max)) continue;
            edges.Add(new CH2D_Edge(polygons[p_i].vertices[i], polygons[p_i].vertices[j]));
        }
        return edges;
    }

    private struct CH2D_Intersection
    {
        public int polyA;
        public int polyB;
        public CH2D_P_Index a_e1;
        public CH2D_P_Index a_e2;
        public CH2D_P_Index b_e1;
        public CH2D_P_Index b_e2;
        public float distance_from_e1;
        public Vector2 pos;
        public CH2D_Intersection(int polyA, int polyB, CH2D_P_Index a_e1, CH2D_P_Index a_e2, CH2D_P_Index b_e1, CH2D_P_Index b_e2, Vector2 pos, float distance_from_e1)
        {
            //this.new_point = new_point;
            this.polyA = polyA;
            this.polyB = polyB;
            this.a_e1 = a_e1;
            this.a_e2 = a_e2;
            this.b_e1 = b_e1;
            this.b_e2 = b_e2;
            this.pos = pos;
            this.distance_from_e1 = distance_from_e1;
        }
    }

    public List<int> PolygonPointIntersection(Vector2 p)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < polygons.Count; i++)
        {
            if (!polygons[i].BBox.Contains(p)) continue;
            List<Vector2> points = GetPolyVertices(i);
            if (Poly2DToolbox.IsPointInsidePolygon(p, points)) result.Add(i);
        }
        return result;
    }

    public string GetDebugData(int p)
    {
        if (p < 0 | p >= polygons.Count) return "No polygon selected\n";
        List<Vector2> points = GetPolyVertices(p);
        string to_return = "Polygon index: " + p + (this.polygons[p].isHole ? " <color=red>Hole</color>" : " <color=green>Fill</color>" + "\n");
        string p_list = "{";
        for (int i = 0; i < this.polygons[p].vertices.Count; i++) p_list += this.polygons[p].vertices[i] + " ";
        p_list += "}\n";
        string area = "Area: " + Poly2DToolbox.AreaShoelace(points) + "sqr\n";

        return to_return + area + p_list;
    }
    // ======================================
    //  УДАЛЕНИЕ
    // ======================================
    // Функция для уничтожения точек что не принадлежат ни одному полигону.
    // Цель - снизу вверх уничтожать по одной точке, переименовывая точки в полигонах
    // Надо составить словарь из старого названия точки и нового названия точки. Так для переписи внутри полигонов нужно будет сделать лишь один проход
    // Блин, задача удаления точки из списка точек - на удивление одна из самых дорогих задач в этьом коде

    // Можно упростить задачу если каждую точку представить как подчанк+точку, а подчанки сделать кластерами близких точек
    // В этом случае надо будет определить подчанки которые оказываются изменены, и переписать только привязанные к подчанкам полигоны
    public bool DeletePolygon(int p)
    {
        if (p < 0 | p >= this.polygons.Count) return false;
        this.polygons.RemoveAt(p);
        this.connections.DeletePoint(p);
        PurgeUnusedPoints();
        return true;
    }
    private bool SoftDeletePolygon(CH2D_Polygon p)
    {   // Soft-Deletes polygon without removing points, for internal use in CutIntPoly functions
        int exists = this.polygons.FindIndex(poly => poly == p);
        if (exists == -1) return false;
        this.polygons.RemoveAt(exists);
        this.connections.DeletePoint(exists);
        return true;
    }
    public void PurgeUnusedPoints()
    {
        (int to_delete, CH2D_P_Index[] new_point_array) = GetUsabilityDictionary();
        // Вырезка выпавших точек из Vector2 списка
        
        List<Vector2> new_vertices_list = new List<Vector2>(this.vertices.Count - to_delete);
        //string n = ""; for (int i = 0; i < this.vertices.Count; i++) n += this.vertices[i] + " "; Debug.Log(n);
        for (int i = 0; i < new_point_array.Length; i++)
        {
            if (new_point_array[i].i == ushort.MaxValue) continue;
            new_vertices_list.Add(this.vertices[i]);
        }
        //n = ""; for (int i = 0; i < new_vertices_list.Count; i++) n += new_vertices_list[i] + " "; Debug.Log(n);
        // Редактура всех полигонов в соответствии со словарем vertices:new_vertices_array

        for (int p = 0; p < this.polygons.Count; p++)
        {
            CH2D_Polygon poly = this.polygons[p];
            for (int v = 0; v < poly.vertices.Count; v++) 
                poly.vertices[v] = new_point_array[poly.vertices[v]];
        }

        this.vertices = new_vertices_list.ToList();
    }

    public (int to_delete, CH2D_P_Index[] new_point_array) GetUsabilityDictionary()
    {
        CH2D_P_Index[] new_point_list = new CH2D_P_Index[this.vertices.Count];
        for (int i = 0; i < new_point_list.Length; i++) new_point_list[i] = new CH2D_P_Index(ushort.MaxValue); 
        // Проврка всех полигонов, точки не участвующие в полигонах остаются равными ushort.maxvalue
        for (int i = 0; i < this.polygons.Count; i++)
            for (int j = 0; j < this.polygons[i].vertices.Count; j++)
                new_point_list[this.polygons[i].vertices[j]] = this.polygons[i].vertices[j];
        
        int unused_p = 0;
        for (int i = 0; i < new_point_list.Length; i++)
        {
            if (new_point_list[i].i == ushort.MaxValue) { unused_p += 1; }
            else { new_point_list[i] = new CH2D_P_Index(i - unused_p); } 
        }
        string n = ""; for (int i = 0; i < new_point_list.Length; i++) n += new_point_list[i] + " "; Debug.Log(n);

        return (unused_p, new_point_list);
    }
}


