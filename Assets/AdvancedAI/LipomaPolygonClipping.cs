using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// Прототипирование превратилось в раковую опухоль, пришлось вырезать.
// Но это моя единственная и потому сама любимая раковая опухоль, поэтомуо на заслуживает отдельного файлика для роста.
public static class LipomaPolygonClipping
{
    // Да даже чанки брать не обязательно. Просто скормить список полигонов. Дальше оно само рассосется, система хорошая. 
    // Надо получить общее облако точек, и общие индексы для вершин. Обновить каждый полигон с учетом точек пересечения.
    // Наверное полигоны можно не обновлять. Достаточно найти пересекающиеся вершины и сохранить их пары.
    // Затем, если с одной гранью есть несколько пересечений, можно по данным текущего пересечения определить какое пересечение должно быть следующим.
    // По итогу нет нужды обновлять полигоны. Тоесть тут выбор между вычислительной нагрузкой и нагрузкой на пасять.
    // Либо я обновляю полигоны, произвожу много простых операций и может быть имею нужду расширить массиф
    // Либо я не обновляю полигоны, и проверяю вообще все пересечения с каждой из граней. Из плюсов получается то что два изначальных полигона остаются неизменны и операцию можно в любой момент отменить.
    // Внутри каждой из групп не должно быть переесчений граней. Может быть допустимы касания вершин, вообще штука должна с ними справиться, но тут хз.
    

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
        string n = "PGPoints " + PGPoints.Count + ": ";
        for (int i = 0; i < PGPoints.Count; i++) n += "\n" + PGPoints[i].ToString() + " " + A.vertices[PGPoints[i].Aindex];
        Debug.Log(n);
        // Маркировка

        List<PGEdge> edges = GetEdges(A, B, shared_segments, PGPoints);

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
            if (p.isHole)
            {
                PGPoints[edges[i].start].con_list.Add(new PGConnection(edges[i], PGDirection.Outgoing, out_angle));
                PGPoints[edges[i].end].con_list.Add(new PGConnection(edges[i], PGDirection.Ingoing, inn_angle));
            }
            else
            {
                PGPoints[edges[i].end].con_list.Add(new PGConnection(edges[i], PGDirection.Ingoing, inn_angle));
                PGPoints[edges[i].start].con_list.Add(new PGConnection(edges[i], PGDirection.Outgoing, out_angle));
            }
            
            Debug.Log(edges[i].ToString() + " " + (Mathf.Rad2Deg * out_angle).ToString("0.0000") + " " + (Mathf.Rad2Deg * inn_angle).ToString("0.0000"));
        }
        // Связанные грани должны быть расположены согласно правилу. 
        // Затем в каждой точке отсортировать эджи по глобальному углу
        //for (int i = 0; i < PGPoints.Count; i++) PGPoints[i].con_list.Sort((a, b) => a.angle.CompareTo(b.angle)); // (-90 -x), (0 +y), (90 +x), (+-180 -y)
        Debug.Log("<color=orange> НАЧАЛАСЬ СОРТИРОВКА <color/>");
        for (int i = 0; i < PGPoints.Count; i++)
        {
            SafeSort(PGPoints[i]);
        }
        Debug.Log("<color=orange> КОНЧИЛАСЬ СОРТИРОВКА <color/>");
        // Объединение было ошибкой. Я теряю информацию для определения принадлежности грани к внутреннему или наружнему списку.
        /*Debug.Log("<color=orange> ДО ОБЪЕДИНЕНИЯ <color/>");
        for (int i = 0; i < PGPoints.Count; i++) Debug.Log(PGPoints[i]);
        for (int i = 0; i < PGPoints.Count; i++) TryUnifyEdgesInPoint(i, PGPoints, edges);*/
        Debug.Log("<color=orange> ТОЧКИ И ГРАНИ <color/>");
        for (int i = 0; i < PGPoints.Count; i++) Debug.Log(PGPoints[i]);
        Debug.Log("<color=orange> КОНЕЦ СПИСКА ТОЧЕК <color/>");
        if (draw_connection >= 0 && draw_connection < PGPoints.Count) DrawChaosStar(draw_connection, PGPoints, edges, A, B);
        // Когда все отсортировано, будет легко определить принадлежность каждой из эджей.

        return null;
    }
    // К моему удивлению, все сегменты идут парочками из входящего и исходящего. Этого стоило ожидать, они созданы последовательно и потому тоже будут расположены последовательно.
    // Но сегменты разных полигонов все еще разрозненны, и порядок зависит от порядка полигонов внутри чанка. 
    private static void SafeSort(PGPointIntwise PGPoints)
    {
        // Как вариант можно разбить задачу сортировки списка на две отдельных задачи сортировки списка для чанка А и для чанка Б.
        // Сейчас не поддерживается иерархическая сортировка.
        if (PGPoints.con_list.Count % 2 != 0) throw new System.Exception(" Количество элементов в списоке входящих и выходящих точек должено быть кратено двум. ");
        List<(float angle1, float angle2, int original_i)> pairs_A = new(PGPoints.con_list.Count / 2);
        List<(float angle1, float angle2, int original_i)> pairs_B = new(PGPoints.con_list.Count / 2);
        for (int i = 0; i < PGPoints.con_list.Count / 2; i++)
        {
            int pair_i_a = i * 2;
            int pair_i_b = i * 2 + 1;
            if (PGPoints.con_list[pair_i_a].edge.belong == PGBelong.A) // Belong для pair_i_a и pair_i_b одинаков.
                pairs_A.Add(new (PGPoints.con_list[pair_i_a].angle, PGPoints.con_list[pair_i_b].angle, i) );
            else
                pairs_B.Add(new (PGPoints.con_list[pair_i_a].angle, PGPoints.con_list[pair_i_b].angle, i));
        }

        //pairs_A.Sort((a, b) => a.angle1.CompareTo(b.angle2));
        //pairs_B.Sort((a, b) => a.angle1.CompareTo(b.angle2));
        List<int> linker = ArrayAndListToolbox.NonOverlappingIntervalLinker(pairs_A);
        string int_linker = "Intlinker: ";
        for (int i = 0; i < linker.Count; i++)
            int_linker += linker[i] + " ";
        Debug.Log(int_linker);
        //pairs_A.Reverse();
        //pairs_B.Reverse();
        string n = "Result: " + " \n";
        for (int i = 0; i < pairs_A.Count; i++)
        {
            n += PGPoints.con_list[pairs_A[i].original_i * 2].edge.ToString() + " " + PGPoints.con_list[pairs_A[i].original_i * 2].ToString() + "\n";
            n += PGPoints.con_list[pairs_A[i].original_i*2+1].edge.ToString() + " " + PGPoints.con_list[pairs_A[i].original_i*2+1].ToString() + "\n";
        }
        Debug.Log(n);

    }

    private static List<PGEdge> GetEdges(CH2D_Chunk A, CH2D_Chunk B, List<ReturnPoint> shared_segments, List<PGPointIntwise> PGPoints )
    {
        List<int[]> chunkA_marks = new(A.polygons.Count);
        List<int[]> chunkB_marks = new(B.polygons.Count);
        for (int i = 0; i < A.polygons.Count; i++)
            chunkA_marks.Add(Matrix.GetSetArray(A.polygons[i].vertices.Count, -1));
        for (int i = 0; i < B.polygons.Count; i++)
            chunkB_marks.Add(Matrix.GetSetArray(B.polygons[i].vertices.Count, -1));

        for (int i = 0; i < shared_segments.Count; i++)
        {
            ReturnPoint p = shared_segments[i];
            int point_link = PGPoints.FindIndex(v => v.Aindex == p.chunk_A_index);
            if (p.belong == PGBelong.A) chunkA_marks[p.polygon][p.index] = point_link;
            else chunkB_marks[p.polygon][p.index] = point_link;
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
        return edges;
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
    private static void TryUnifyEdgesInPoint(int target_p, List<PGPointIntwise> PGPoints, List<PGEdge> edges)
    {   // Сравнение соседних углов, объединение одинаковых в один.
        // Граф разнится если граф пересечений построен на пересечениях чанков, или же на пересечении всех полигонов вовсе. 
        // В пересечении чанков могут появляться грани, начинающиеся коллинеарно, а заканчивающиеся разрозненно.
        // Это происходит когда чанк состоит из отдельных, но касающихся полигонов. В этом случае есть выбор отрезать начальную коллинеарную часть, или же оставить дегенеративную грань

        // Я слишком сильно абстрагировал задачу от входных данных. Объединяя грани я теряю информацию о принадлежности к полигону. 
        // Я понял что мой подход имеет проблемы в случае когда соседние грани внутри точки оба bidirectional. Я теряю информацию о том как определить грань внутри или снаружи полигона.

        // Тут я хрень сделал. Надо хранить не грани связанные с пересечением, а полигоны. Я верю иерархическим чанкам, обоим из них, поэтому вся эта абстракция никому не сдалась.
        // Нужно сохранить информацию о пересечении, и все-таки надо сохранять структуру на чанк+полигон+индекс. Я так смогу определять в какую сторону направлена исходящая грань.
        // Структура содержит ссылку на чанк, и проэтому надо будет провести проверку всеА * всеБ для определения принадлежности грани. 
        // Подход со звездой векторов работает, но требуется определять иерархию полигонов.
        // Тоесть грани грани одного полигона одного чанка должны идти соседствующими парочками. 
        // Вопрос только в том как определить правильный порядок граней когда обе грани biderectional. 
        // На бумажке видно что для этого нужна информация об порядке обоих полигонов одного чанка. Это можно выдавить из иерархии или из объединение Chunk+Poly+Index структур в список.

        // Самая вонючая проблема в этом случае - полностью двунаправленный треугольник.
        // Без информации об иерархии не понятно, это CCW внутри которого CW (дырка в пустоте), или CW у которого внутри CCW (трава в траве)

        // Есть решение: отдельнная сортировка для каждого из полигонов касающихся точки. Полигоны А сортируются отдельно, согласно правилу и иерархии:
        // Входящая и исходящая грани полигона соседствуют друг с другом. Внутренние полигона, ниже по иерархии, содержатся между жвух соседних граней. Щас я работаю с плоскими полигонами, поэтому это не важно.
        // В целом, если соседние грани однонаправлены и идентичны, то их действительно можно объединить без потери информации.
        // Уменьшить количество граней возможно после классификаци граней на внутренние/наружнгые

        // Тоесть вся это что я сделал хуйня полная.
        PGPointIntwise tp = PGPoints[target_p];
        for (int i = 0; i < tp.con_list.Count; i++)
        {
            int edge1 = i;
            int edge2 = (i + 1) % tp.con_list.Count;
            if (tp.con_list[edge1].angle != tp.con_list[edge2].angle) continue;

            {
                PGEdge e1 = tp.con_list[edge1].edge; PGEdge e2 = tp.con_list[edge2].edge;
                //Debug.Log(e1.start + " " + e2.start + " " + e1.end + " " +e2.end + " SAME: " + (e1.start == e2.start & e1.end == e2.end) + " same swap: " + (e1.start == e2.end & e1.end == e2.start));
                if (!((e1.start == e2.start & e1.end == e2.end) | (e1.start == e2.end & e1.end == e2.start))) continue;
            }

            int other_point = tp.con_list[i].edge.start == target_p ? tp.con_list[i].edge.end : tp.con_list[i].edge.start;

            int index_to_edit = PGPoints[other_point].con_list.FindIndex(v => v.edge == tp.con_list[edge1].edge);
            int index_to_remove = PGPoints[other_point].con_list.FindIndex(v => v.edge == tp.con_list[edge2].edge);

            // Обновление оригинального и удаление дубликата в соседней вершиен
            if (index_to_remove != -1)
            {
                PGPoints[other_point].con_list[index_to_edit] = PGPoints[other_point].con_list[index_to_edit].UpdateDirection(PGPoints[other_point].con_list[index_to_remove].dir);
                PGPoints[other_point].con_list.RemoveAt(index_to_remove);
            }
            // Обновление оригинального и удаление дубликата у себя дома

            tp.con_list[edge1] = tp.con_list[edge1].UpdateDirection(tp.con_list[edge2].dir); // Тут копируется сущность из массива, оперируется, и вставляется обратно. Фигня полная но пох.
            tp.con_list.RemoveAt(edge2);
            edges.RemoveAt(edge2);
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
        for (int i = 0; i < point.con_list.Count; i++) valid_edges.Add(point.con_list[i].edge);
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
                DebugUtilities.DebugDrawLine(chunk.vertices[edge_end.B], chunk.vertices[edge_end.A], color, 4f);
                DebugUtilities.DebugDrawSquare(chunk.vertices[edge_end.A], square_color, 0.3f, 4f);
            }
            int other_point = valid_edges[i].start == point_i ? valid_edges[i].end : valid_edges[i].start;
            DebugUtilities.DebugDrawSquare(A.vertices[points[other_point].Aindex], square_color, square_size, 4f);
            //DebugUtilities.DebugDrawSquare(chunk.vertices[edge_end.B], DebugUtilities.RainbowGradient_Red2Violet(i, point.con_list.Count - 1), 0.1f, 4f);
            //DebugUtilities.DebugDrawSquare(chunk.vertices[edge_start.A], DebugUtilities.RainbowGradient_Red2Violet(i, point.con_list.Count - 1), 0.1f, 4f);
        }
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
                n += "\n" + con_list[i].edge;
                n += " " + con_list[i].ToString();
            }
            return n;
        }
    }

    protected struct PGConnection
    {
        public PGEdge edge; // грань графа может начаться  изакончиться в одной и той же точке. ПОэтому индекс живет в связи.
        public PGDirection dir; // каждая грань одномвременно входная и выходная, поэтому направление живет в связи. 
        public float angle;// каждая грань одномвременно входная и выходная, и имеет два угла на вход и на выход, поэтому угол живет в связи. 
        public PGConnection(PGEdge edge, PGDirection dir, float angle) { this.edge = edge; this.dir = dir; this.angle = angle; }
        public PGConnection UpdateDirection(PGDirection o_dir) { this.dir = this.dir | o_dir; return this; }
        public override string ToString()
        {
            return dir + " angle " + (angle * Mathf.Rad2Deg).ToString("0000.0000");
        }
    }
    protected class PGEdge
    {   // Надо жестче разделить вершины и грани. 
        public GHPolygonMerge.EdgeSide side; public PGBelong belong;
        public int poly_id, segment_start, segment_length;
        public int start, end;
        public PGEdge(PGBelong belong, int poly_id, int segment_start, int segment_length, int start_PGPoint, int end_PGPoint)
        {
            this.belong = belong; this.poly_id = poly_id; this.segment_start = segment_start; this.segment_length = segment_length; this.start = start_PGPoint; this.end = end_PGPoint;
            this.side = GHPolygonMerge.EdgeSide.None;
        }
        public void SetEdgeSide(GHPolygonMerge.EdgeSide side) { this.side = side; }
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
        public PGCon(PGDirection dir, PGBelong belong, float global_anlge, int poly_index, int edge_index, int segment_length, int prev_PGPoint_index, int next_PGPoint_index)
        {
            this.dir = dir; this.belong = belong; this.global_angle = global_anlge; this.poly_index = poly_index; this.edge_index = edge_index; this.segment_length = segment_length; this.prev_PGPoint_index = prev_PGPoint_index; this.next_PGPoint_index = next_PGPoint_index;
        }
        public PGCon(PGDirection dir, PGBelong belong, int poly_index, int edge_index)
        {
            this.dir = dir; this.belong = belong; this.global_angle = 0f; this.poly_index = poly_index; this.edge_index = edge_index; this.segment_length = -1; this.prev_PGPoint_index = -1; this.next_PGPoint_index = -1;
        }
        public PGCon(PGBelong belong, int poly_index, int edge_index)
        {
            this.dir = PGDirection.None; this.belong = belong; this.global_angle = 0f; this.poly_index = poly_index; this.edge_index = edge_index; this.segment_length = -1; this.prev_PGPoint_index = -1; this.next_PGPoint_index = -1;
        }
        public void SetAngle(float angle) { this.global_angle = angle; }
        public void SetSegmentLength(int length) { this.segment_length = length; }
        public void SetNextPGPoint(int next) { this.next_PGPoint_index = next; }
        public void SetPrevPGPoint(int prev) { this.prev_PGPoint_index = prev; }
        public void SetPGDirection(PGDirection dir) { this.dir = dir; }
    }
    protected enum PGDirection : sbyte { Ingoing = 1, Outgoing = 2, Bidirectional = 3, None = -1 }
    protected enum PGBelong : byte { None = 0, A, B, Both }
    protected struct ReturnPoint
    {
        public PGBelong belong; public int polygon, index;
        public CH2D_P_Index chunk_A_index;
        public ReturnPoint(PGBelong belong, int polygon, int index, CH2D_P_Index chunk_A_index) { this.belong = belong; this.polygon = polygon; this.index = index; this.chunk_A_index = chunk_A_index; }
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
        var unique_segments = shared_segments.ToList();
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
    // ТУТ ВСЕ МЕРТВО

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
                    new_point.connections.Add(new PGCon(PGDirection.Ingoing, PGBelong.A, polyA_index, (unique_va[ua] - 1 + polygon.vertices.Count) % polygon.vertices.Count));
                }

                for (int ub = 0; ub < unique_vb.Count; ub++)
                {
                    CH2D_Polygon polygon = B.polygons[polyB_index];
                    new_point.connections.Add(new PGCon(PGDirection.Outgoing, PGBelong.B, polyB_index, unique_vb[ub]));
                    new_point.connections.Add(new PGCon(PGDirection.Ingoing, PGBelong.B, polyB_index, (unique_vb[ub] - 1 + polygon.vertices.Count) % polygon.vertices.Count));
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
                Debug.Log("vertice count before operation " + A.polygons[a].vertices.Count + " " + B.polygons[b].vertices.Count);
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
}
