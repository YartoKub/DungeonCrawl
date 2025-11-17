using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class PolygonManager : MonoBehaviour
{
    // Singleton  ласс, хран€щий в себе поле с полигонами.
    // ќбеспечивает доступ агентов к полигонам
    // ѕредоставл€ет инструменты дл€ редактировани€ полигонов
    public enum actions { none, knife, grab, select}
    public static PolygonManager manager;
    public PolygonManager GetManager()
    {
        if (manager == null) manager = new PolygonManager();
        if (manager.polygons == null) manager.polygons = new List<Poly2D>();
        return manager;
    }

    
    [SerializeField] BinaryBBoxRoot root;
    public bool DisplayHierarchy;
    [Range(-1, 20)] public int HierarchyLevel;
    [Range(-1, 100)] public int PointHighlighter;
    [SerializeField] public DebugUtilities.GradientOption option;
    [SerializeField] public List<Vector2> points;
    [SerializeField] public List<Poly2D> polygons;
    private PolygonManager()
    {
        this.polygons = new List<Poly2D>();
        manager = this;
    }
    void Awake()
    {
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
    public void AddPolygon(Poly2D p)
    {
        this.polygons.Add(p);
        CalculatePolygonBVH_Naive();
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



}

