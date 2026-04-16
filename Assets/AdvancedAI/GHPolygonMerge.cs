using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// Я запрещаю кому-либо использовать написанный мной код для обучения нейросетей. Это моя интеллектуальная собственность.
// I forbid anyone to use code, written by me, to train neural networks. It is my intellectual property.
// TODO: придумать имя для класса. Тут теперь не только Greiner-Horrmann живет, но и самоделка.
// TODO: Перерисовать ASCII картинку. Я теперь работаю с гранями а не с точками.

// Сначала реализовать рабочие многоуровневые CH2D_P_Index полигоны.
// Добавление новых полигонов к ним через менеджер с относительной позицией
// TODO: Вместо шагохода для сборки полигонов использовать однонаправленный граф, все грани одного типа объединить в интерфалы 
// TODO: Из-за шагохода нужно отмечать каждую пройденную грань и хранить информацию о гранях которые уже были использованы
// TODO: С графовой штукой придется делать все то же самое, но грани будут иметь длину в 1 интервал, может проще будет.

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
    const int used_point = -3; // Эта вся штука бесполезна с новым методом разметки граней.
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

    // Да даже чанки брать не обязательно. Просто скормить список полигонов. Дальше оно само рассосется, система хорошая. 
    // Надо получить общее облако точек, и общие индексы для вершин. Обновить каждый полигон с учетом точек пересечения.
    // Наверное полигоны можно не обновлять. Достаточно найти пересекающиеся вершины и сохранить их пары.
    // Затем, если с одной гранью есть несколько пересечений, можно по данным текущего пересечения определить какое пересечение должно быть следующим.
    // По итогу нет нужды обновлять полигоны. Тоесть тут выбор между вычислительной нагрузкой и нагрузкой на пасять.
    // Либо я обновляю полигоны, произвожу много простых операций и может быть имею нужду расширить массиф
    // Либо я не обновляю полигоны, и проверяю вообще все пересечения с каждой из граней. Из плюсов получается то что два изначальных полигона остаются неизменны и операцию можно в любой момент отменить.
    // Внутри каждой из групп не должно быть переесчений граней. Может быть допустимы касания вершин, вообще штука должна с ними справиться, но тут хз.
    /// <summary>
    /// I assume that polygons within groups do not have intersections. Once it is done i will see whether it can handle single point intersections or not.
    /// Hope it will, because it should handle them without extra logic.
    /// Chaos Star Polygon Clipping Algorythm? Нормальное имя для алгоритма. 
    /// </summary>
    /// <returns></returns>
    public static GraphDynamicList GetGraph(CH2D_Chunk A, CH2D_Chunk B, bool dummy, int draw_connection = -1) 
    {
        List<Level2IntersectionChunkPoint> intersections = GetPairIntersections(A, B);
        // Надо из этой хрени достать, по порядку:
        // Одинаковые значения ca или cb и из них создать PGPoints
        // Пары полигонаА + полигонБ
        // Найти все уникальные va и vb и уже на их основе заполнить PGCon 
        List<List<Level2IntersectionChunkPoint>> unique_ppvv = intersections.GroupBy(v => v.ca).Select(v => v.ToList()).ToList(); // созданы списки с одинаковым ca+cb

        // В DataStructures я написал фигню без поддержки <T> для граней и без поддержки значений вне матрицы. Пишу здесь новый граф.
        //Debug.Log("Uniqeus: "); // Тест на инь-яне говорит что пары единичны для каждого случая (так и надо) // Тест на круасанf[ выдает четыре пары для одной точки (тоже корректно)
        //for (int i = 0; i < unique_ppvv.Count; i++) Debug.Log(unique_ppvv[i]);

        List<PGPoint> PGpoints = new();
        for (int i = 0; i < unique_ppvv.Count; i++)
        {
            List<Level2IntersectionChunkPoint> local_inter_list = unique_ppvv[i]; // в нем все ca и cb одинаковые
            PGPoint new_point = new(); new_point.Aindex = local_inter_list[0].ca; new_point.Bindex = local_inter_list[0].cb; new_point.connections = new List<PGCon>();

            List<List<Level2IntersectionChunkPoint>> pairs_of_polygons = local_inter_list.GroupBy(poly_pair => (poly_pair.polyA, poly_pair.polyB)).Select(v => v.ToList()).ToList();

            // Сгруппированы по паре polyA+polyB
            for (int p = 0; p < pairs_of_polygons.Count; p++)
            {
                int polyA_index = pairs_of_polygons[p][0].polyA;
                int polyB_index = pairs_of_polygons[p][0].polyB;

                HashSet<int> hash_va = new();
                HashSet<int> hash_vb = new();
                for (int u = 0; u < pairs_of_polygons[p].Count; u++)
                {
                    hash_va.Add(pairs_of_polygons[p][u].va);
                    hash_vb.Add(pairs_of_polygons[p][u].vb);
                }
                List<int> unique_va = hash_va.ToList();
                List<int> unique_vb = hash_vb.ToList();

                for (int ua = 0; ua < unique_va.Count; ua++)
                {
                    CH2D_Polygon polygon = A.polygons[polyA_index];
                    new_point.connections.Add(new PGCon(PGDirection.Outgoing, PGBelong.A, polyA_index, unique_va[ua]));
                    new_point.connections.Add(new PGCon(PGDirection.Ingoing,  PGBelong.A, polyA_index, (unique_va[ua] - 1 + polygon.vertices.Count) % polygon.vertices.Count));
                }

                for (int ub = 0; ub < unique_vb.Count; ub++) {
                    CH2D_Polygon polygon = B.polygons[polyB_index];
                    new_point.connections.Add(new PGCon(PGDirection.Outgoing, PGBelong.B, polyB_index, unique_vb[ub]));
                    new_point.connections.Add(new PGCon(PGDirection.Ingoing,  PGBelong.B, polyB_index, (unique_vb[ub] - 1 + polygon.vertices.Count) % polygon.vertices.Count));
                }
            }
            PGpoints.Add(new_point);
        }
        // Chaos start debug draw
        for (int i = 0; i < PGpoints.Count; i++) Debug.Log(PGpoints[i]);
        if (draw_connection >= 0 & draw_connection < PGpoints.Count) DrawChaosStar(PGpoints[draw_connection], A, B);
        // ХЗ что делать. С одной стороны надо скомпилировать грани графа, тоесть совершить проход по всем полигонам.
        // С другой стороны надо скомпилировать точки. 
        // Компиляция граней. Надо перерабо
        return null;
    }

    public static GraphDynamicList GetGraph(CH2D_Chunk A, CH2D_Chunk B, int draw_connection = -1)
    {
        (List<(CH2D_P_Index A, CH2D_P_Index B)> shared_points, List<ReturnPoint> shared_segments) = GetPairIntersectionsSimpler(A, B);

        // Реализация графа которая кажется чуть более умной:
        // Более умный алгоритм:
        // (галочка) Найти все точки пересечения точно также как в прошлый раз, но занести их все в hashset или лучше словать (Ai, Bi) : List<Chunk+Poly+index>
        // (галочка) Как вариант можно создать двойной hashset, первый уровень: (Ai, Bi) для точки, затем второй уровень для (Chunk+Poly+index) комбинаций
        // (галочка) еще внутри вместо hashset-а можно использовать сортируемый список с только уникальными элементами, его эффективность будет падать с ростом количества пересекающихся полигонов в одной точке
        // (галочка) Ai Bi сделать список PGPointIntwise, отсортировать по точке A. Количество элементов в списке соединений равно (Chunk+Poly+index).Count * 2
        //
        // (галочка) Из Chunk+Poly+index сделать отметки на всех полигонах.
        // (галочка) Тут абсолютно не важно какая грань с чем пересекается, важен лишь факт общей точки, поэтому hashset можно составлять во время поиска пересечений

        // Провести проход по вем полигонам, создавая эджи. Эджи отсортировать по порядку индекса начальной точки А чтобы не ебаться лишний раз
        // Или можно просто добавлять эджи, находя конечную и начальную точки, и пихать эти эдэи в обе вершины. Потом внутри вершин их отсортировать и оценить
        // Этот шаг в целом схож с уже реализованным разметчиком

        // Вот теперь самое время склеить все в месте. Эджи есть .

        // Сам граф можно распутать отрезанием ушек, поиск точки с 
        List<PGPointIntwise> PGPoints = new();
        for (int i = 0; i < shared_points.Count; i++)
            PGPoints.Add(new PGPointIntwise(shared_points[i].A, shared_points[i].B));

        PGPoints.Sort((a, b) => a.Aindex.CompareTo(b.Aindex));
        string n = "PGPoints " + PGPoints.Count + ": " ;
        for (int i = 0; i < PGPoints.Count; i++) n += "\n" + PGPoints[i].ToString() + " " + A.vertices[PGPoints[i].Aindex];
        Debug.Log(n);
        // Маркировка
        List<int[]> chunkA_marks = new(A.polygons.Count);
        List<int[]> chunkB_marks = new(B.polygons.Count);
        for (int i = 0; i < A.polygons.Count; i++)
            chunkA_marks.Add(Matrix.GetSetArray(A.polygons[i].vertices.Count, -1));
        for (int i = 0; i < B.polygons.Count; i++)
            chunkB_marks.Add(Matrix.GetSetArray(B.polygons[i].vertices.Count, -1));

        for (int i = 0; i < shared_segments.Count; i++) {
            ReturnPoint p = shared_segments[i];
            int point_link = PGPoints.FindIndex(v => v.Aindex == p.chunk_A_index); 
            if (p.belong == PGBelong.A) chunkA_marks[p.polygon][p.index] = point_link;
            else                        chunkB_marks[p.polygon][p.index] = point_link;
        }

        Debug.Log("A marks");
        for (int i = 0; i < chunkA_marks.Count; i++)
            Debug.Log(DebugUtilities.DebugListString(chunkA_marks[i].ToArray()));
        Debug.Log("B marks");
        for (int i = 0; i < chunkB_marks.Count; i++)
            Debug.Log(DebugUtilities.DebugListString(chunkB_marks[i].ToArray()));
        // Построение еджей
        // Сначала создать пустые эджи, с данными об интервале, начале и конце, но без классификации внутренной принадлежности 
        List<PGEdge> edges = new();
        for (int i = 0; i < chunkA_marks.Count; i++)
            edges.AddRange(GetEdgesFromMarks(chunkA_marks[i], PGBelong.A, i));
        for (int i = 0; i < chunkB_marks.Count; i++)
            edges.AddRange(GetEdgesFromMarks(chunkB_marks[i], PGBelong.B, i));

        Debug.Log("SEGMENTS: ");
        for (int i = 0; i < edges.Count; i++) Debug.Log(edges[i]);
        // Добавление сегментов в граф связей:
        for (int i = 0; i < edges.Count; i++)
        {
            CH2D_Chunk chunk = edges[i].belong == PGBelong.A ? A : B;
            CH2D_Polygon p = chunk.polygons[edges[i].poly_id];
            CH2D_Edge out_ch2d_edge = p.GetEdge(edges[i].segment_start);
            CH2D_Edge inn_ch2d_edge = p.GetEdge((edges[i].segment_start + edges[i].segment_length) % p.vertices.Count);
            Vector2 out_v = chunk.vertices[out_ch2d_edge.B] - chunk.vertices[out_ch2d_edge.A];
            Vector2 inn_v = chunk.vertices[inn_ch2d_edge.A] - chunk.vertices[inn_ch2d_edge.B];
            float out_angle = Mathf.Atan2(out_v.x, out_v.y);
            float inn_angle = Mathf.Atan2(inn_v.x, inn_v.y);
            PGPoints[edges[i].start].con_list.Add(new PGConnection(edges[i], PGDirection.Outgoing, out_angle));
            PGPoints[edges[i].end]  .con_list.Add(new PGConnection(edges[i], PGDirection.Ingoing , inn_angle));
            Debug.Log(edges[i].ToString() + " " + (Mathf.Rad2Deg * out_angle).ToString("0.0000") + " " + (Mathf.Rad2Deg * inn_angle).ToString("0.0000"));
        }
        // Затем в каждой точке отсортировать эджи по глобальному углу
        for (int i = 0; i < PGPoints.Count; i++) PGPoints[i].con_list.Sort((a, b) => a.angle.CompareTo(b.angle)); // (-90 -x), (0 +y), (90 +x), (+-180 -y)
        if (draw_connection >= 0 && draw_connection < PGPoints.Count) DrawChaosStar(draw_connection, PGPoints, edges, A, B);
        // Есть излишние эджи-дубликаты. Надо все эджи с одним исходящим углом объеденить в одну супер-эджу.
        // Пришлось переписать PGEdge из структуры в класс. Так меньше ебли с перезаписью индексов. 
        
        Debug.Log("<color=orange> ДО ОБЪЕДИНЕНИЯ <color/>");
        for (int i = 0; i < PGPoints.Count; i++) Debug.Log(PGPoints[i]);
        
        for (int i = 0; i < PGPoints.Count; i++)
        {
            UnifyEdgesInPoint(i, PGPoints, edges);
        }
        Debug.Log("<color=orange> ПОСЛЕ ОБЪЕДИНЕНИЯ <color/>");
        for (int i = 0; i < PGPoints.Count; i++) Debug.Log(PGPoints[i]);
        

        // Когда все отсортировано, будет легко определить принадлежность каждой из эджей.



        //for (int i = 0; i < PGpoints.Count; i++) Debug.Log(PGpoints[i]);
        //if (draw_connection >= 0 & draw_connection < PGpoints.Count) DrawChaosStar(PGpoints[draw_connection], A, B);


        return null;
        void UnifyEdgesInPoint(int target_p, List<PGPointIntwise> PGPoints, List<PGEdge> edges)
        {   // Сравнение соседних углов, объединение одинаковых в один.
            PGPointIntwise tp = PGPoints[target_p];
            for (int i = 0; i < tp.con_list.Count; i++)
            {
                int edge1 = i;
                int edge2 = (i + 1) % tp.con_list.Count;
                if (tp.con_list[edge1].angle != tp.con_list[edge2].angle) continue;
                {
                    PGEdge e1 = tp.con_list[edge1].edge_id; PGEdge e2 = tp.con_list[edge2].edge_id;
                    if (e1.start == e2.start & e1.end == e2.end) continue;
                    if (e1.start == e2.end & e1.end == e2.start) continue;
                }

                int other_point = tp.con_list[i].edge_id.start == target_p ? tp.con_list[i].edge_id.end : tp.con_list[i].edge_id.start;

                int index_to_edit = PGPoints[other_point].con_list.FindIndex(v => v.edge_id == tp.con_list[edge1].edge_id);
                int index_to_remove = PGPoints[other_point].con_list.FindIndex(v => v.edge_id == tp.con_list[edge2].edge_id);

                /*Debug.Log(PGPoints[target_p]);
                Debug.Log(PGPoints[other_point]);
                Debug.Log(edge1 + " " + edge2);
                Debug.Log(index_to_edit + " " + index_to_remove);
                Debug.Log(tp.con_list.Count);*/
                // Обновление оригинального и удаление дубликата в соседней вершиен
                if (index_to_remove != -1) {
                    PGPoints[other_point].con_list[index_to_edit].UpdateDirection(PGPoints[other_point].con_list[index_to_remove].dir);
                    PGPoints[other_point].con_list.RemoveAt(index_to_remove);
                }
                // Обновление оригинального и удаление дубликата у себя дома
                tp.con_list[edge1].UpdateDirection(tp.con_list[edge2].dir);
                tp.con_list.RemoveAt(edge2);
                edges.RemoveAt(edge2);
            }
        }
        List<PGEdge> GetEdgesFromMarks(int[] marked_polygon, PGBelong belong, int poly_id)
        {   //Разбивает циклический список на сегменты. Интервалы: [p != -1, p != -1) внутри содержатся все значения точек равных -1.
            List<(int a, int b)> pairs = ArrayAndListToolbox.LoopedListSegmentation(marked_polygon);
            //if (pairs.Count == 0) return null; // в полигоне нет пересечений. Для этого случая нужно отдельную логику присобачить. Классическое внутри/снаружи c sweep line проверкой
            List<PGEdge> edges = new(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
                edges.Add(new PGEdge(belong, poly_id, pairs[i].a, pairs[i].b, marked_polygon[pairs[i].a], marked_polygon[(pairs[i].a + pairs[i].b + 1) % marked_polygon.Length]));
            return edges;
        }
    }
    // TODO: преобразовать все ссылки на edge-ы из интежеров в классовую просто-ссылку.
    private static void DrawChaosStar(int point_i, List<PGPointIntwise> points, List<PGEdge> edges, CH2D_Chunk A, CH2D_Chunk B)
    {
        PGPointIntwise point = points[point_i];
        Vector2 center_point = A.vertices[point.Aindex];
        DebugUtilities.DebugDrawSquare(center_point, Color.yellow, 0.2f, 4f);
        DebugUtilities.DebugDrawSquare(center_point, Color.yellow, 0.4f, 4f);

        List<PGEdge> valid_edges = new(point.con_list.Count);
        for (int i = 0; i < point.con_list.Count; i++) valid_edges.Add(point.con_list[i].edge_id);
        Debug.Log(point);
        for (int i = 0; i < valid_edges.Count; i++)
        {
            PGConnection con = point.con_list[i];
            Color color = Color.black;
            switch (con.dir)
            {
                case PGDirection.Ingoing: color = Color.cyan; break;
                case PGDirection.Outgoing: color = Color.pink; break;
                case PGDirection.Bidirectional: color = Color.yellow; break;
                default: break;
            }
            CH2D_Chunk chunk = valid_edges[i].belong == PGBelong.A ? A : B;
            CH2D_Edge edge_start = chunk.polygons[valid_edges[i].poly_id].GetEdge(valid_edges[i].segment_start);
            CH2D_Edge edge_end = chunk.polygons[valid_edges[i].poly_id].GetEdge(valid_edges[i].segment_start + valid_edges[i].segment_length);
            //Debug.Log(edge_start.A + " " + edge_start.B + " " + edge_end.A + " " + edge_end.B);
            //Debug.Log(A.vertices[point.Aindex] + " " + chunk.vertices[edge_start.A] + " " +chunk.vertices[edge_end.B]);
            Color square_color = DebugUtilities.RainbowGradient_Red2Violet(i, point.con_list.Count - 1);
            float square_size = 0.05f + 0.20f * (float)i / (float)valid_edges.Count;
            if (con.dir == PGDirection.Outgoing)
            {
                DebugUtilities.DebugDrawLine(chunk.vertices[edge_start.A], chunk.vertices[edge_start.B], color, 4f);
                DebugUtilities.DebugDrawSquare(chunk.vertices[edge_start.B], square_color, 0.3f, 4f);
            }
            if (con.dir == PGDirection.Ingoing)
            {
                DebugUtilities.DebugDrawLine(chunk.vertices[edge_end.A], chunk.vertices[edge_end.B], color, 4f);
                DebugUtilities.DebugDrawSquare(chunk.vertices[edge_end.A], square_color, 0.3f, 4f);
            }
            if (con.dir == PGDirection.Bidirectional)
            {
                DebugUtilities.DebugDrawLine(chunk.vertices[edge_start.A], chunk.vertices[edge_start.B], color, 4f);
                DebugUtilities.DebugDrawSquare(chunk.vertices[edge_start.B], square_color, 0.3f, 4f);
                DebugUtilities.DebugDrawLine(chunk.vertices[edge_end  .B], chunk.vertices[edge_end  .A], color, 4f);
                DebugUtilities.DebugDrawSquare(chunk.vertices[edge_end  .A], square_color, 0.3f, 4f);
            }
            int other_point = valid_edges[i].start == point_i ? valid_edges[i].end : valid_edges[i].start;
            DebugUtilities.DebugDrawSquare(A.vertices[points[other_point].Aindex], square_color, square_size, 4f);
            //DebugUtilities.DebugDrawSquare(chunk.vertices[edge_end.B], DebugUtilities.RainbowGradient_Red2Violet(i, point.con_list.Count - 1), 0.1f, 4f);
            //DebugUtilities.DebugDrawSquare(chunk.vertices[edge_start.A], DebugUtilities.RainbowGradient_Red2Violet(i, point.con_list.Count - 1), 0.1f, 4f);
        }
    }

    private static void DrawChaosStar(PGPoint point, CH2D_Chunk A, CH2D_Chunk B)
    {
        Vector2 center_point = A.vertices[point.Aindex];
        DebugUtilities.DebugDrawSquare(center_point, Color.yellow, 0.2f, 4f);
        DebugUtilities.DebugDrawSquare(center_point, Color.yellow, 0.4f, 4f);
        for (int i = 0; i < point.connections.Count; i++)
        {
            PGCon con = point.connections[i];
            Color color = Color.black;
            switch (con.dir)
            {
                case PGDirection.Ingoing: color = Color.cyan; break;
                case PGDirection.Outgoing: color = Color.pink; break;
                case PGDirection.Bidirectional: color = Color.yellow; break;
                default: break;
            }
            Vector2 p1; Vector2 p2; // заполнение этих значений позициями из корректного чанка
            {
                CH2D_Chunk target_chunk = (con.belong == PGBelong.A) ? A : B;
                CH2D_Polygon poly = target_chunk.polygons[con.poly_index];
                p1 = target_chunk.vertices[poly.vertices[con.edge_index]];
                p2 = target_chunk.vertices[poly.vertices[(con.edge_index + 1) % poly.vertices.Count]];
            }
            Vector2 target_order_point = p1 == center_point ? p2 : p1;

            DebugUtilities.DebugDrawLine(p1, p2, color, 4f);
            DebugUtilities.DebugDrawSquare(target_order_point, DebugUtilities.RainbowGradient_Red2Violet(i, point.connections.Count - 1), 0.1f, 4f);
            DebugUtilities.DebugDrawSquare(target_order_point, DebugUtilities.RainbowGradient_Red2Violet(i, point.connections.Count - 1), 0.2f, 4f);
        }
    }

    public static GraphDynamicList GetGraphNoIntersection(CH2D_Chunk A, CH2D_Chunk B, List<Pair> intersections)
    {   // Есть пары точек, общих для обоих чанков в intersections
        // Проблема - я не знаю какая точка следует за каждой из точек потому что я храню только точки, а не полигоны
        // А надо index полигона + index точки в полигоне
        return null;
    }
    
    protected class PGPointIntwise
    {
        public List<PGConnection> con_list;
        public CH2D_P_Index Aindex;
        public CH2D_P_Index Bindex;
        public PGPointIntwise(CH2D_P_Index Aindex, CH2D_P_Index Bindex)
        {
            this.Aindex = Aindex;
            this.Bindex = Bindex;
            this.con_list = new();
        }
        public override string ToString()
        {
            string n = "PGPoint: a " + Aindex + " b " + Bindex;

            for (int i = 0; i < con_list.Count; i++)
            {
                n += "\n" + con_list[i].edge_id + " " + con_list[i].dir;
                n += " angle " + con_list[i].angle.ToString("0.0000");
            }
            return n;
        }
    }

    protected struct PGConnection
    {
        public PGEdge edge_id; // грань графа может начаться  изакончиться в одной и той же точке. ПОэтому индекс живет в связи.
        public PGDirection dir; // каждая грань одномвременно входная и выходная, поэтому направление живет в связи. 
        public float angle;// каждая грань одномвременно входная и выходная, и имеет два угла на вход и на выход, поэтому угол живет в связи. 
        public PGConnection(PGEdge edge_id, PGDirection dir, float angle) { this.edge_id = edge_id; this.dir = dir; this.angle = angle; }
        public void UpdateDirection(PGDirection dir) { this.dir |= dir; }
    }
    protected class PGEdge
    {   // Надо жестче разделить вершины и грани. 
        public EdgeSide side; public PGBelong belong;
        public int poly_id, segment_start, segment_length;
        public int start, end;
        public PGEdge(PGBelong belong, int poly_id, int segment_start, int segment_length, int start_PGPoint, int end_PGPoint)
        {
            this.belong = belong; this.poly_id = poly_id; this.segment_start = segment_start; this.segment_length = segment_length; this.start = start_PGPoint; this.end = end_PGPoint;
            this.side = EdgeSide.None;
        }
        public void SetEdgeSide(EdgeSide side) { this.side = side; }
        public override string ToString()
        {
            return this.belong + " " + this.poly_id + " " + this.segment_start + " " + this.segment_length + " side: " + this.side + " s/e " + start + " " + end;
        }
    }

    // Старая реализация элементов графа:
    protected class PGPoint // Polygon Graph Poing
    {
        public List<PGCon> connections;
        public CH2D_P_Index Aindex; 
        public CH2D_P_Index Bindex;
        public override string ToString()
        {
            string n = "PGPoint: a " + Aindex + " b " + Bindex;

            for (int i = 0; i < connections.Count; i++)
            {
                n += "\n" + connections[i].belong + " " + connections[i].dir;
                n += " angle " + connections[i].global_angle.ToString("0.0000") + 
                " ( " + connections[i].edge_index + " " + connections[i].segment_length + " ) "
                + " " + connections[i].poly_index + " links: ( " + connections[i].prev_PGPoint_index + " " + connections[i].next_PGPoint_index + ")"; 
            }
            return n;
        }
    }
    protected struct PGCon // Polygon Graph Connection
    {
        public PGDirection dir; public PGBelong belong; 
        public float global_angle; // global anlge, relative to global coordinate system. It is to sort connections
        public int edge_index, segment_length; // Edge chains are reduced to start+length, to reduce the amount of graph steps and ease loop finding
        public int poly_index, prev_PGPoint_index, next_PGPoint_index;
        //public EdgeSide side;
        public PGCon(PGDirection dir, PGBelong belong, float global_anlge, int poly_index, int edge_index, int segment_length, int prev_PGPoint_index, int next_PGPoint_index) {
            this.dir = dir; this.belong = belong; this.global_angle = global_anlge; this.poly_index = poly_index; this.edge_index = edge_index; this.segment_length = segment_length; this.prev_PGPoint_index = prev_PGPoint_index; this.next_PGPoint_index = next_PGPoint_index; }
        public PGCon(PGDirection dir, PGBelong belong, int poly_index, int edge_index) {
            this.dir = dir; this.belong = belong; this.global_angle = 0f; this.poly_index = poly_index; this.edge_index = edge_index; this.segment_length = -1; this.prev_PGPoint_index = -1; this.next_PGPoint_index = -1;
        }
        public PGCon(PGBelong belong, int poly_index, int edge_index) {
            this.dir = PGDirection.None; this.belong = belong; this.global_angle = 0f; this.poly_index = poly_index; this.edge_index = edge_index; this.segment_length = -1; this.prev_PGPoint_index = -1; this.next_PGPoint_index = -1; }
        public void SetAngle(float angle) { this.global_angle = angle; }
        public void SetSegmentLength(int length) { this.segment_length = length; }
        public void SetNextPGPoint(int next) { this.next_PGPoint_index = next; }
        public void SetPrevPGPoint(int prev) { this.prev_PGPoint_index = prev; }
        public void SetPGDirection(PGDirection dir) { this.dir = dir; }
    }
    protected enum PGDirection : sbyte { Ingoing = 1, Outgoing = 2, Bidirectional = 3, None = -1  }
    protected enum PGBelong : byte { None = 0, A, B, Both }
    protected struct ReturnPoint
    {
        public PGBelong belong; public int polygon, index;
        public CH2D_P_Index chunk_A_index;
        public ReturnPoint(PGBelong belong, int polygon, int index, CH2D_P_Index chunk_A_index) { this.belong = belong; this.polygon = polygon; this.index = index; this.chunk_A_index = chunk_A_index; }
    }
    /// <summary>
    /// Iteratively updates every polygon to include shared points and find intersections
    /// </summary>
    /// <returns></returns>
    public static List<Level2IntersectionChunkPoint> GetPairIntersections(CH2D_Chunk A, CH2D_Chunk B)
    {
        // Part responsible for finding intersections and adding them
        for (int a = 0; a < A.polygons.Count; a++)
        { // there is no need to perfor intersection and vertice insertion on both boxes. Intersection points are inserted only into A, as they will then be picked up by collinearity operation
            CH2D_Polygon ap = A.polygons[a];
            for (int b = 0; b < B.polygons.Count; b++)
            {
                CH2D_Polygon bp = B.polygons[b];
                if (!ap.BBox.Intersects(bp.BBox)) continue;
                CH2D_Chunk.PolyPolyOnlineIntersectionOnesided(A, B, a, b);
            }
        }

        // Part responsible for handling shared and collinear points that already exist, and incorporating them into both chunks
        List<Level2IntersectionChunkPoint> pairs = new();
        for (int a = 0; a < A.polygons.Count; a++)
        {
            CH2D_Polygon ap = A.polygons[a];
            for (int b = 0; b < B.polygons.Count; b++)
            {
                CH2D_Polygon bp = B.polygons[b];
                if (!ap.BBox.Intersects(bp.BBox)) continue;
                Debug.Log("vertice count before operation " +  A.polygons[a].vertices.Count + " " + B.polygons[b].vertices.Count);
                List<Pair> point_pairsAB = CH2D_Chunk.Incorporate_B_to_A_GetPpolyPointPairs(A, B, a, b);
                List<Pair> point_pairsBA = CH2D_Chunk.Incorporate_B_to_A_GetPpolyPointPairs(B, A, b, a);
                Debug.Log("vertice count after operation " + A.polygons[a].vertices.Count + " " + B.polygons[b].vertices.Count);
                Debug.Log(a + DebugUtilities.DebugListString(point_pairsAB.ToArray()) + " " + b + " " + DebugUtilities.DebugListString(point_pairsBA.ToArray()));
                //pairs.AddRange(point_pairsAB);
                for (int i = 0; i < point_pairsAB.Count; i++) 
                    pairs.Add(new Level2IntersectionChunkPoint(a, b, point_pairsAB[i].A, point_pairsAB[i].B, ap.vertices[point_pairsAB[i].A], bp.vertices[point_pairsAB[i].B]));
                for (int i = 0; i < point_pairsBA.Count; i++)
                    pairs.Add(new Level2IntersectionChunkPoint(a, b, point_pairsBA[i].B, point_pairsBA[i].A, ap.vertices[point_pairsBA[i].B], bp.vertices[point_pairsBA[i].A]));
            }
        }
        //pairs = pairs.Distinct().ToList();
        for (int i = 0; i < pairs.Count; i++)
        {
            DebugUtilities.DebugDrawCross(A.vertices[pairs[i].ca], Color.yellow, 1.0f);
            Debug.Log(pairs[i]);
        }

        return pairs;
    }

    private static (List<(CH2D_P_Index A, CH2D_P_Index B)>, List<ReturnPoint>) GetPairIntersectionsSimpler(CH2D_Chunk A, CH2D_Chunk B)
    {
        // Part responsible for finding intersections and adding them
        for (int a = 0; a < A.polygons.Count; a++)
        { // there is no need to perfor intersection and vertice insertion on both boxes. Intersection points are inserted only into A, as they will then be picked up by collinearity operation
            CH2D_Polygon ap = A.polygons[a];
            for (int b = 0; b < B.polygons.Count; b++)
            {
                CH2D_Polygon bp = B.polygons[b];
                if (!ap.BBox.Intersects(bp.BBox)) continue;
                CH2D_Chunk.PolyPolyOnlineIntersectionOnesided(A, B, a, b);
            }
        }

        // Part responsible for handling shared and collinear points that already exist, and incorporating them into both chunks
        HashSet<(CH2D_P_Index A, CH2D_P_Index B)> shared_point = new();
        HashSet<ReturnPoint> shared_segments = new();
        for (int a = 0; a < A.polygons.Count; a++)
        {
            CH2D_Polygon ap = A.polygons[a];
            for (int b = 0; b < B.polygons.Count; b++)
            {
                CH2D_Polygon bp = B.polygons[b];
                if (!ap.BBox.Intersects(bp.BBox)) continue;
                Debug.Log("vertice count before operation " + A.polygons[a].vertices.Count + " " + B.polygons[b].vertices.Count);
                List<Pair> point_pairsAB = CH2D_Chunk.Incorporate_B_to_A_GetPpolyPointPairs(A, B, a, b);
                List<Pair> point_pairsBA = CH2D_Chunk.Incorporate_B_to_A_GetPpolyPointPairs(B, A, b, a);
                Debug.Log("vertice count after operation " + A.polygons[a].vertices.Count + " " + B.polygons[b].vertices.Count);
                Debug.Log(a + DebugUtilities.DebugListString(point_pairsAB.ToArray()) + " " + b + " " + DebugUtilities.DebugListString(point_pairsBA.ToArray()));
                //pairs.AddRange(point_pairsAB);
                for (int i = 0; i < point_pairsAB.Count; i++)
                {
                    shared_point.Add((ap.vertices[point_pairsAB[i].A], bp.vertices[point_pairsAB[i].B]));
                    shared_segments.Add(new ReturnPoint(PGBelong.A, a, point_pairsAB[i].A, ap.vertices[point_pairsAB[i].A]));
                    shared_segments.Add(new ReturnPoint(PGBelong.B, b, point_pairsAB[i].B, ap.vertices[point_pairsAB[i].A]));
                }
                for (int i = 0; i < point_pairsBA.Count; i++)
                {
                    shared_point.Add((ap.vertices[point_pairsBA[i].B], bp.vertices[point_pairsBA[i].A]));
                    shared_segments.Add(new ReturnPoint(PGBelong.A, a, point_pairsBA[i].B, ap.vertices[point_pairsBA[i].B]));
                    shared_segments.Add(new ReturnPoint(PGBelong.B, b, point_pairsBA[i].A, ap.vertices[point_pairsBA[i].B]));
                }
            }
        }

        var unique_points = shared_point.ToList();
        var unique_segments= shared_segments.ToList();
        for (int i = 0; i < unique_points.Count; i++)
        {
            if (A.vertices[unique_points[i].A] != B.vertices[unique_points[i].B]) continue;
            DebugUtilities.DebugDrawCross(A.vertices[unique_points[i].A], Color.yellow, 1.0f);
            Debug.Log(unique_points[i].A + " " + unique_points[i].B);
        }
        for (int i = 0; i < unique_segments.Count; i++)
        {
            Debug.Log(unique_segments[i].belong + " " + unique_segments[i].polygon + " " + unique_segments[i].index);
        }

        return (unique_points, unique_segments);
    }

    /// <summary>
    /// This functions finds all shared points and intersections of two multileveled polygons. It just searches for them. 
    /// Not optimal algorithm. <br/>
    /// I begin to suspect that this function is shit as it overcomplicates the task that could be done simpler by not being greedy with memory
    /// </summary>
    /// <returns></returns>
    public static List<Level2IntersectionRatio> GetPairPairIntersections(List<CH2D_Polygon> groupA, List<CH2D_Polygon> groupB, List<Vector2> pointsA, List<Vector2> pointsB)
    {
        List<Level2IntersectionRatio> intersections = new();
        List<Pair> pairs = new();
        for (int a = 0; a < groupA.Count; a++)
            for (int b = 0; b < groupB.Count; b++)
                if (groupA[a].BBox.Intersects(groupB[b].BBox)) pairs.Add(new Pair(a, b, false));

        for (int i = 0; i < pairs.Count; i++)
        {
            CH2D_Polygon ap = groupA[pairs[i].A];
            CH2D_Polygon bp = groupB[pairs[i].B];
            // Здесь есть ошибка
            List<Vector2> av = CH2D_Chunk.GetVertices(ap.vertices, pointsA);
            List<Vector2> bv = CH2D_Chunk.GetVertices(bp.vertices, pointsB);
            List<Pair> edgesA = Poly2DToolbox.EdgesInsideBounds(av, bp.BBox.GetNewLargerBox(new Vector2(Geo3D.epsilon, Geo3D.epsilon)));
            List<Pair> edgesB = Poly2DToolbox.EdgesInsideBounds(bv, ap.BBox.GetNewLargerBox(new Vector2(Geo3D.epsilon, Geo3D.epsilon)));
            Debug.Log(av.Count + " " + bv.Count + " " + ap.BBox + " " + bp.BBox);
            Debug.Log(DebugUtilities.DebugListString(edgesA.ToArray()));
            Debug.Log(DebugUtilities.DebugListString(edgesB.ToArray()));
            for (int a = 0; a < edgesA.Count; a++)
            {
                Edge2D edgeA = new Edge2D(av[edgesA[a].A], av[edgesA[a].B]);
                
                for (int b = 0; b < edgesB.Count; b++)
                {
                    Edge2D edgeB = new Edge2D(bv[edgesB[b].A], bv[edgesB[b].B]);

                    if (pairs[i].A == 3 & pairs[i].B == 3) { DebugUtilities.DebugDrawLine(edgeA.A, edgeA.B, Color.red, 1.0f); DebugUtilities.DebugDrawLine(edgeB.A, edgeB.B, Color.yellow, 1.0f); }
                    
                    if (Poly2DToolbox.PointBelongToLine2D(edgeB.A, edgeB.B, edgeA.A))
                    {
                        float? Bratio_loc = Geo3D.GetVectorRatio(edgeB.A, edgeB.B, edgeA.A);
                        if (Bratio_loc.Value == 1) continue;
                        intersections.Add(new Level2IntersectionRatio(pairs[i].A, pairs[i].B, edgesA[a].A, edgesB[b].A, 0, Bratio_loc.Value));
                        continue;
                    }
                    if (Poly2DToolbox.PointBelongToLine2D(edgeA.A, edgeA.B, edgeB.A))
                    {
                        float? Aratio_loc = Geo3D.GetVectorRatio(edgeA.A, edgeA.B, edgeB.A);
                        if (Aratio_loc.Value == 1) continue;
                        intersections.Add(new Level2IntersectionRatio(pairs[i].A, pairs[i].B, edgesA[a].A, edgesB[b].A, Aratio_loc.Value, 0));
                        continue;
                    }


                    if (!Poly2DToolbox.LineLineIntersection(edgeA.A, edgeA.B, edgeB.A, edgeB.B, out Vector2 new_point)) continue;

                    float? Aratio = Geo3D.GetVectorRatio(edgeA.A, edgeA.B, new_point);
                    float? Bratio = Geo3D.GetVectorRatio(edgeB.A, edgeB.B, new_point);
                    if (Aratio == 1 | Bratio == 1 | Aratio == 0 | Bratio == 0) continue;
                    if (Aratio == null | Bratio == null) continue; 
                    intersections.Add(new Level2IntersectionRatio(pairs[i].A, pairs[i].B, edgesA[a].A, edgesB[b].A, Aratio.Value, Bratio.Value));
                    
                }
            }
        }
        Debug.Log(intersections.Count);
        for (int i = 0; i < intersections.Count; i++)
        {
            Debug.Log(intersections[i]);
            CH2D_P_Index p1 = groupA[intersections[i].A].vertices[intersections[i].a];
            CH2D_P_Index p2 = groupA[intersections[i].A].vertices[(intersections[i].a + 1) % groupA[intersections[i].A].vertices.Count];
            Vector2 inter = pointsA[p1] + (pointsA[p2] - pointsA[p1]) * intersections[i].Aratio;
            Debug.Log(inter);
            DebugUtilities.DebugDrawCross(inter, Color.red, 1.0f);
        }
        Debug.Log("This function may produce duplicate intersection points.");
        // This function has to be modified to fix duplicates at some point, but not now.
        // Не, это хрень. Я тут проверяю только edge/edge пересечения, а способов определить point/edge и point/point у меня нет. Да и не понятно что с ними делать.
        // point связана с двумя гранями минимум, получается edge/point будет иметь две связи в одной точке? Вообще логично
        // Как вариант можно представить грань как начальную точку до конечной точки не включая конечную. Так не будет двойственности.

        return intersections;
    }
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

    //public static 

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
        int safety_margin = 50;
        while (safety < safety_margin)
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
        if (safety >= safety_margin) Debug.Log("<color=orange>Ran out of safety margins, Comissar Yarrick, please consider rising safety limit here to a highert value</color>");
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