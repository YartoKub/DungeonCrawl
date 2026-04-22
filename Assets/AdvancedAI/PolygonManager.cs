using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class PolygonManager : MonoBehaviour
{
    // Singleton Класс, хранящий в себе поле с полигонами.
    // Обеспечивает доступ агентов к полигонам
    // Предоставляет инструменты для редактирования полигонов
    
    public enum TargetDebugTestChunk { global, first_leveled, second_leveled, convex_hierarchy } // Выбор места куда добавляется полигон
    private static PolygonManager manager;
    public static PolygonManager GetManager()
    {
        if (manager == null)
        {
            GameObject managerGameObject = new GameObject("PolygonManager"); 
            manager = managerGameObject.AddComponent<PolygonManager>();
        }
        if (manager.polygons == null) manager.polygons = new List<Poly2D>();
        return manager;
    }

    
    [SerializeField] BinaryBBoxRoot root;
    public bool DisplayHierarchy;
    public bool DebugDisplayLeveledPolygons;
    [Range(-1, 20)] public int HierarchyLevel;
    [Range(-1, 100)] public int PointHighlighter;
    [SerializeField] public DebugUtilities.GradientOption option;
    [SerializeField] public Orientation orientation;
    [SerializeField] public TargetDebugTestChunk target_chunk;
    [SerializeField] public CH2D_Chunk.PolygonAddMode polygonAddMode;
    [SerializeField] public List<Vector2> points;
    [SerializeField] public List<Poly2D> polygons;


    public CH2D_Chunk my_chunk;
    public ConvexHierarchy2D world_map;
    public CH2D_LeveledChunk leveled_one;
    public CH2D_LeveledChunk leveled_two;
    public Vector2 level1_offset;
    public Vector2 level2_offset;
    public int selected1;
    public int selected2;
    [SerializeField] SelectionColorScheme selectionColorScheme;
    private List<int> selection;
    enum SelectionColorScheme { GreenRedDirected, Rainbow}
    [SerializeField] private ChunkAction SelectedChunkAction;

    public bool Union, Inter, Aonly, Bonly;
    private enum ChunkAction
    {
        Nothing,
        IncorporateMutualPoints, 
        IncorporateBPointsToA,
        PolyMergeDelegate,
        RainbowColor,
        GetNewVectorForTests,
        RecalculateConnectionGraph,
        FindSharedEdgeContinuities,
        PathfindNodeAB,
        ChunkChunkIntersections

    }
    public void CallFunctionOnChosen()
    {

        switch (SelectedChunkAction) {
            case ChunkAction.IncorporateMutualPoints:
                if (selected1 == -1 | selected2 == -1) { Debug.Log("Нужно выбрать два полигона!"); break; }
                if ((selected1 >= my_chunk.polygons.Count) | (selected2 >= my_chunk.polygons.Count)) { Debug.Log("Есть запредельный полигон!"); break; }
                my_chunk.MutualVerticeIncorporation(selected1, selected2);
                break;
            case ChunkAction.IncorporateBPointsToA:
                if (selected1 == -1 | selected2 == -1) { Debug.Log("Нужно выбрать два полигона!"); break; }
                if ((selected1 >= my_chunk.polygons.Count) | (selected2 >= my_chunk.polygons.Count)) { Debug.Log("Есть запредельный полигон!"); break; }
                if (selected1 == selected2) { Debug.Log("Выбран один и тот же полигон!"); break; }
                my_chunk.Incorporate_Bvertice_To_PolyA(selected1, selected2);
                break;
            case ChunkAction.RainbowColor:
                CH2D_Chunk tchunk = my_chunk;
                switch (target_chunk)
                {
                    case TargetDebugTestChunk.global: tchunk = my_chunk; break;
                    case TargetDebugTestChunk.first_leveled: tchunk = leveled_one; break;
                    case TargetDebugTestChunk.second_leveled: tchunk = leveled_two; break;
                }
                if (selected1 == -1 | selected1 >= tchunk.polygons.Count) { Debug.Log("Запредельный полигон"); break; }
                tchunk.DebugRainbowPolygon(selected1, 5.0f, 0.3f);
                break;
            case ChunkAction.PolyMergeDelegate:
                if (selected1 == -1 | selected2 == -1) { Debug.Log("Нужно выбрать два полигона!"); break; }
                if ((selected1 >= my_chunk.polygons.Count) | (selected2 >= my_chunk.polygons.Count)) { Debug.Log("Есть запредельный полигон!"); break; }
                if (selected1 == selected2) { Debug.Log("Выбран один и тот же полигон!"); break; }
                my_chunk.PolyMergeDelegate(selected1, selected2, Union, Aonly, Bonly, Inter);
                break;
            case ChunkAction.GetNewVectorForTests:
                if (selected1 == -1) { Debug.Log("Нужно один полигон!"); break; }
                if ((selected1 >= my_chunk.polygons.Count)) { Debug.Log("Выбран полигон с индексом превышающий количество полигонов в чанке!"); break; }
                this.GetNewVectorsForTests(selected1);
                break;
            case ChunkAction.FindSharedEdgeContinuities:
                if (selected1 == -1 | selected2 == -1) { Debug.Log("Нужно выбрать два полигона!"); break; }
                if ((selected1 >= my_chunk.polygons.Count) | (selected2 >= my_chunk.polygons.Count)) { Debug.Log("Есть запредельный полигон!"); break; }
                my_chunk.GetSharedEdgeContinuities(selected1, selected2);
                break;
            case ChunkAction.RecalculateConnectionGraph:
                this.my_chunk.ConnectionsRecalculateAll();
                break;
            case ChunkAction.PathfindNodeAB:
                if (selected1 == -1 | selected2 == -1) { Debug.Log("Нужно выбрать два полигона!"); break; }
                if ((selected1 >= my_chunk.polygons.Count) | (selected2 >= my_chunk.polygons.Count)) { Debug.Log("Есть запредельный полигон!"); break; }
                List<int> nodes = this.my_chunk.PathfindWithinChunk(selected1, selected2);
                if (nodes == null) { Debug.Log("Path not found"); return; }
                List<Vector2> centers = new List<Vector2>();
                for (int i = 0; i < nodes.Count; i++) centers.Add(this.my_chunk.polygons[nodes[i]].BBox.center);
                DebugUtilities.DrawPath(centers, Color.orange, 5f);
                break;
            case ChunkAction.ChunkChunkIntersections:
                LipomaPolygonClipping.GetGraph(this.leveled_one, this.leveled_two, HierarchyLevel);
                break;
            default:
                break;
        }
        selected1 = -1; selected2 = -1;
        SelectedChunkAction = ChunkAction.Nothing;
    }
    private void GetNewVectorsForTests(int polyindex)
    {
        if (polyindex < 0 | polyindex >= this.my_chunk.polygons.Count) return;
        List<CH2D_P_Index> pi = this.my_chunk.polygons[polyindex].vertices;
        string to_debug = "";
        for (int i = 0; i < pi.Count; i++)
        {
            to_debug += "new Vector2(" + this.my_chunk.vertices[pi[i]].x + ", " + this.my_chunk.vertices[pi[i]].y + "), ";
        }
        Debug.Log(to_debug);
    }
    private PolygonManager()
    {
        my_chunk = new CH2D_Chunk();
        this.polygons = new List<Poly2D>();
        manager = this;
    }
    void Awake()
    {
        if (my_chunk == null) my_chunk = new CH2D_Chunk();
        if (polygons == null) polygons = new List<Poly2D>();
    }
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {

        
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < points.Count; i++)
        {
            DebugUtilities.HandlesDrawCross(points[i], Color.cyan);
        }
        //if (polygons != null) 
            for (int i = 0; i < polygons.Count; i++)
                polygons[i].HandlesDrawSelf(polygons[i].isHole ? Color.red : Color.blue);
            
        HandlesDrawHierarchy(HierarchyLevel);
        if (PointHighlighter != -1 & PointHighlighter < points.Count) DebugUtilities.HandlesDrawCross(points[PointHighlighter], Color.red);

        this.my_chunk.HandlesDrawSelf(DisplayHierarchy);
        HandlesDrawSelectionSecondary();
        HandlesDrawSelection();

        world_map.DrawWorld();

        if (DebugDisplayLeveledPolygons)
        {
            this.leveled_one.HandlesDrawSelf(DisplayHierarchy);
            this.leveled_two.HandlesDrawSelf(DisplayHierarchy);
        }
        
    }

    public void AddPoint(Vector2 p)
    {
        this.points.Add(p);
        Geo3D.SortPoints(this.points);
        //CalculatePointBVH_Naive();
    }
    public void RemovePoint(int p_index)
    {
        if (p_index < 0 || p_index >= this.points.Count) return;
        this.points.RemoveAt(p_index);
        Geo3D.SortPoints(this.points);
        //CalculatePointBVH_Naive();
    }
    public void PurgeChunk()
    {
        this.my_chunk = new CH2D_Chunk();
        this.leveled_one = new CH2D_LeveledChunk();
        this.leveled_two = new CH2D_LeveledChunk();
        this.level1_offset = Vector2.zero;
        this.level2_offset = Vector2.zero;
        
    }
    public void PurgeWorld()
    {
        this.world_map.PurgeSelf();
        Debug.Log("Сюда бы функцию для полного выноса всех сохранений на диске");
    }
    public void RegenerateWorld()
    {
        this.world_map.DefaultRegeneration();
    }
    public void DebugIntersection()
    {
        this.my_chunk.DebugGetIntersections(true, false);
    }
    public void HighlightInnsAndOuts()
    {
        this.my_chunk.DebugGetIntersections(false, true);
    }

    public void AddPolygon(Poly2D p)
    {
        string p1 = ""; foreach (var item in p.vertices) p1 += "new Vector2(" + item.x + ", " + item.y + "),"; Debug.Log(p1);
        Debug.Log(polygonAddMode);
        switch (target_chunk)
        {
            case TargetDebugTestChunk.global: this.my_chunk.AddPolygon(p, polygonAddMode); break;
            case TargetDebugTestChunk.second_leveled: this.leveled_two.AddPolygon(p, polygonAddMode); break;
            case TargetDebugTestChunk.first_leveled: this.leveled_one.AddPolygon(p, polygonAddMode); break;
        }
        
    }

    public void AddPolygon(Poly2D p, TargetDebugTestChunk target, CH2D_Chunk.PolygonAddMode mode)
    {
        string p1 = ""; foreach (var item in p.vertices) p1 += "new Vector2(" + item.x + ", " + item.y + "),"; Debug.Log(p1);
        Debug.Log(mode + " " + target);
        switch (target)
        {
            case TargetDebugTestChunk.global: this.my_chunk.AddPolygon(p, mode); break;
            case TargetDebugTestChunk.second_leveled: this.leveled_two.AddPolygon(p, mode); break;
            case TargetDebugTestChunk.first_leveled: this.leveled_one.AddPolygon(p, mode); break;
        }

    }

    public void AddPolygon(List<Vector2> p)
    {
        this.my_chunk.AddPolygon(p, polygonAddMode);
    }
    public void RemovePolygon(int p_index)
    {
        if (p_index < 0 || p_index >= this.polygons.Count) return;
        this.polygons.RemoveAt(p_index);
        CalculatePolygonBVH_Naive();
    }
    public void PurgePolygons()
    {
        this.polygons.Clear();
    }

    public (int, float) ClosestPoint(Vector2 p)
    {
        int min = -1; float min_d = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float d = (p - points[i]).magnitude;
            if (d < min_d)
            {
                min = i;
                min_d = d;
            }
        }
        return (min, min_d);
    }

    public void CalculatePolygonBVH_Naive()
    {
        Poly2D.SortListByCenters(polygons);
        if (polygons.Count == 0)
        {
            this.root = null;
            return;
        }
        this.root = BinaryBBoxToolbox.BuildBHVNaive(new List<I_BBoxSupporter>(polygons));
        Debug.Log(this.root.max_depth);
    }
    public void CalculatePolygonBVH_SideGrowing()
    {

        if (polygons.Count == 0)
        {
            this.root = null;
            return;
        }
        this.root = BinaryBBoxToolbox.BuildBVHSideGrowing(new List<I_BBoxSupporter>(polygons));
        Debug.Log(this.root.max_depth);
    }

    public void CalculatePointBVH_Naive()
    {
        List<Bounds> bounds_list = new List<Bounds>(points.Count);
        if (points.Count == 0)
        {
            this.root = null; 
            return;
        }
        for (int i = 0; i < points.Count; i++)
            bounds_list.Add(new Bounds(points[i], Vector2.one));

        this.root = BinaryBBoxToolbox.BuildBHVNaive(bounds_list);
        Debug.Log(this.root.max_depth);
    }
    public void CalculatePointBVH_SideGrowing()
    {
        List<Bounds> bounds_list = new List<Bounds>(points.Count);
        if (points.Count == 0)
        {
            this.root = null; 
            return;
        }
        
        for (int i = 0; i < points.Count; i++)
            bounds_list.Add(new Bounds(points[i], Vector2.one));
        
        this.root = BinaryBBoxToolbox.BuildBVHSideGrowing(bounds_list);
        //Debug.Log(this.root.max_depth);
    }
    public void HandlesDrawSelection()
    {
        if (selected1 == -1 | selected1 >= my_chunk.polygons.Count) return;
        my_chunk.HandlesDrawPolyBBox(selected1, Color.yellow);
        if (selectionColorScheme == SelectionColorScheme.GreenRedDirected) my_chunk.HandlesDrawPolyOutlineDirected(selected1, Color.green, Color.red);
        else
        {
            List<Vector2> points = my_chunk.GetPolyVertices(selected1);
            for (int i = 0; i < points.Count - 1; i++)
                DebugUtilities.HandlesDrawLine(points[i], points[i + 1], DebugUtilities.RainbowGradient_Red2Violet(i, points.Count - 1));
            DebugUtilities.HandlesDrawLine(points[points.Count - 1], points[0], DebugUtilities.RainbowGradient_Red2Violet(points.Count - 1, points.Count - 1));
        }
        my_chunk.HandlesDrawPolyPoints(selected1, Color.cyan);
    }
    public void HandlesDrawSelectionSecondary()
    {
        if (selected2 == selected1) selected2 = -1;
        if (selected2 == -1 | selected2 >= my_chunk.polygons.Count) return;
        my_chunk.HandlesDrawPolyBBox(selected2, Color.orange);
        my_chunk.HandlesDrawPolyOutlineDirected(selected2, Color.lightGray, Color.red);
        my_chunk.HandlesDrawPolyPoints(selected2, Color.cyan);
    }
    public void HandlesDrawHierarchy(int target)
    {
        if (this.root == null) return;
        if (!DisplayHierarchy) return;
        if (target == -1) HandlesDrawHierarchyFull(root.bbox, 0);
        HandlesDrawHierarchyLevel(root.bbox, 0, target);
    }
    public void HandlesDrawHierarchyLevel(BinaryBBox b, int cur_depth, int target)
    {
        if (cur_depth > 20) return; 
        if (cur_depth == target) {
            DebugUtilities.HandlesDrawRectangle(b.BBox.min, b.BBox.max, DebugUtilities.PickGradient(cur_depth, this.root.max_depth, option));
            return;
        }
        if (b.child1 != null) HandlesDrawHierarchyLevel(b.child1, cur_depth + 1, target);
        if (b.child2 != null) HandlesDrawHierarchyLevel(b.child2, cur_depth + 1, target);
    }
    public void HandlesDrawHierarchyFull(BinaryBBox b, int cur_depth)
    {
        if (cur_depth > 20) return;
        DebugUtilities.HandlesDrawRectangle(b.BBox.min, b.BBox.max, DebugUtilities.PickGradient(cur_depth, this.root.max_depth, option));

        if (b.child1 != null) HandlesDrawHierarchyFull(b.child1, cur_depth + 1);
        if (b.child2 != null) HandlesDrawHierarchyFull(b.child2, cur_depth + 1);
    }
    public void DebugHighLightPoint(int index) { DebugUtilities.DrawCube(points[index], Vector3.one * 0.2f, Color.white); }
    public void HandlesHighLightPoint(int index) { DebugUtilities.HandlesDrawCube(points[index], Vector3.one * 0.2f, Color.yellow); }

    private bool SelectionSimilar(List<int> new_selection)
    {
        if (new_selection == null | this.selection == null) return false;
        if (new_selection.Count != selection.Count) return false;
        bool different = false;
        for (int i = 0; i < new_selection.Count; i++)
        {
            different |= (new_selection[i] != selection[i]);
        }
        return !different;
    }
    public void SelectPolygon(Vector2 point)
    {
        List<int> new_selection = my_chunk.PolygonPointIntersection(point);
        if (new_selection == null) { SelectionPurge(); return; }
        if (new_selection.Count == 0) { SelectionPurge(); return; }
        //string n = "old selection "; for (int i = 0; i < selection.Count; i++) n += selection[i] + " "; Debug.Log(n);
        //n = "new selection "; for (int i = 0; i < new_selection.Count; i++) n += new_selection[i] + " "; Debug.Log(n);
        if (SelectionSimilar(new_selection))
        {   // Выборка идентична предыдущей, значит чювак спускается вниз по списку полигонов. Надо найти текущий полигон в выборке, и выбрать следующий.
            if (selected1 == -1) { SetSelection(new_selection[0], new_selection); return; }
            int old_index = -1;
            for (int i = 0; i < new_selection.Count; i++)
            {
                //Debug.Log(selected1 + " " + new_selection[i]);
                if (selected1 == new_selection[i]) { old_index = i; break; }
            }
            //Debug.Log(old_index);
            int new_index = (old_index + 1) % new_selection.Count;
            SetSelection(new_selection[new_index], new_selection);
        }
        else
        {   // Выборки разнятся, просто выбираем самый первый полигон
            SetSelection(new_selection[0], new_selection);
            return;
        }
        return;
    }
    public void DeleteSelectedPolygon()
    {
        my_chunk.DeletePolygon(selected1);
    }
    public string GetPolygonDataDelegate()
    {
        return my_chunk.GetDebugData(selected1);
    }
    private void SetSelection(int new_selected, List<int> new_selection)
    {
        this.selected1 = new_selected;
        this.selection = new_selection;
    }
    public void SelectionPurge()
    {
        selected1 = -1; selection = null;
    }
}

