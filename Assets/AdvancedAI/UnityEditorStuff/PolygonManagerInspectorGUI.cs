using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[CustomEditor(typeof(PolygonManager))]
public class PointManagerInspectorGUI : Editor
{
    public GUIStateMachine[] stateMachines = {
        new GUI_NothingMachine(), 
        new GUI_TestPlacePointStateMachine(), 
        new GUI_TestDeletePointStateMachine(), 
        new GUI_AddPolygonStateMachine(), 
        new GUI_SelectPolygonStateMachine()};
    // Это начальные машины состояний которые можно выбрать через меню.
    // Есть недоступные машины состояний, доступ к которым производится толькко взаимодействуя с этиим машинами.

    public string[] options; //= new string[] { "None", "Draw", "Knife", "Select", "Grab", "TestPlacePoint", "TestDeletePoint" };
    public int current_action_index = 0;
    public GUIStateMachine stateMachine;
    public string current_comment = "";

    public override bool RequiresConstantRepaint() { return true; }
    //Inspector
    public override void OnInspectorGUI()
    {
        PolygonManager manager = (PolygonManager)target;
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape 
            && stateMachine.GetType() != new GUI_NothingMachine().GetType())
        {
            Debug.Log("Завершена постройка полигона");
            e.Use();
            ChangeState(0, manager);
        }

        options = PopupOptions();
        if (stateMachine == null) ChangeState(0, manager);

        GUIStyle wrappedTextStyle = new GUIStyle(EditorStyles.textField);
        wrappedTextStyle.wordWrap = true;
        wrappedTextStyle.richText = true;

        EditorGUILayout.LabelField("Choose action to edit polygons");

        EditorGUILayout.TextField(current_comment, wrappedTextStyle, GUILayout.Height(150));

        int prev_action_index = current_action_index;
        current_action_index = EditorGUILayout.Popup(current_action_index, options);
        if (prev_action_index != current_action_index)
        {
            Debug.Log("inner state machine change");
            if (stateMachine != null) stateMachine.EndStateMachine(manager);
            ChangeState(current_action_index, manager);
        }
        EditorGUILayout.LabelField("Purging");
        if (GUILayout.Button("purge polygons")) manager.PurgePolygons();
        if (GUILayout.Button("Purge Chunk")) manager.PurgeChunk();
        EditorGUILayout.LabelField("Binary Hierarchies");
        if (GUILayout.Button("Point BVH, naive")) manager.CalculatePointBVH_Naive();
        if (GUILayout.Button("Point BVH, side growing")) manager.CalculatePointBVH_SideGrowing();
        if (GUILayout.Button("Polygon BVH, naive")) manager.CalculatePolygonBVH_Naive();
        if (GUILayout.Button("Polygon BVH, side growing")) manager.CalculatePolygonBVH_SideGrowing();
        EditorGUILayout.LabelField("Debug Tests");
        if (GUILayout.Button("Dump Chunk Data")) manager.my_chunk.DebugDumpChunkData();
        if (GUILayout.Button("Get Intersections of 0 and 1")) manager.DebugIntersection();
        if (GUILayout.Button("Get PolyPoly of 0 and 1")) manager.HighlightInnsAndOuts();
        if (GUILayout.Button("Test degenerate polygons")) manager.DebugAddTestPolygon();
        if (GUILayout.Button("Activate Chosen Action on A and B")) manager.CallFunctionOnChosen();
        base.OnInspectorGUI();
    }

    public string[] PopupOptions()
    {
        string[] options = new string[this.stateMachines.Length];
        for (int i = 0; i < options.Length; i++) { options[i] = this.stateMachines[i].GetOptionName(); }
        return options;
    }

    public void ChangeState(int ca_index, PolygonManager target)
    {
        if (ca_index < 0 | ca_index >= stateMachines.Length) ca_index = 0;
        stateMachine = stateMachines[ca_index];
        current_comment = stateMachine.GetDescription();
        current_action_index = ca_index;
        stateMachine.InitStateMachine(target);
    }

    public void ChangeState(GUIStateMachine guism, PolygonManager target)
    {
        if (guism == null) return;
        stateMachine = guism;
        current_comment = guism.GetDescription();
        for (int i = 0; i < stateMachines.Length; i++)
        {
            if (guism.GetType() == stateMachines[i].GetType()) current_action_index = i;
        }
        stateMachine.InitStateMachine(target);
    }

    void OnSceneGUI()
    {
        PolygonManager manager = (PolygonManager)target;
        if (stateMachine != null)
        {
            GUIStateMachine sm = stateMachine.OnSceneGUI(manager);
            if (stateMachine.NeedRefresh()) { SceneView.RepaintAll(); }
            if (sm != null) {this.stateMachine.EndStateMachine(manager); this.ChangeState(sm, manager); }
            if (stateMachine.description_changed) { stateMachine.description_changed = false; current_comment = stateMachine.GetDescription(); }
        } 
    }
    private void OnDestroy() => stateMachine.EndStateMachine((PolygonManager)target);
}

public abstract class GUIStateMachine
{
    public bool description_changed = false;
    public virtual bool NeedRefresh() { return false; }
    public abstract string GetDescription();
    public abstract string GetOptionName();
    public abstract void InitStateMachine(PolygonManager manager);// Обнуление переменных
    public abstract GUIStateMachine OnSceneGUI(PolygonManager manager);
    public abstract void EndStateMachine(PolygonManager manager); // Завершение сложной операции и сохранение всего
}

public class GUI_NothingMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=green>Nothing</color></b> \nSelect an option and click 'Draw' button to start an operation";
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "None";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PolygonManager manager) { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager){ return this;}
    public override void EndStateMachine(PolygonManager manager) { return; }
}

public class GUI_GrabStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=yellow>Grab</color></b> \nDrag point to move it";
    public override string GetDescription(){ return generic_description; }
    private const string generic_option = "Grab";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PolygonManager manager) { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager) { return null; }
    public override void EndStateMachine(PolygonManager manager) { return; }
}
public class GUI_TestPlacePointStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=yellow>Test Place Point</color></b> \nClick to place a point";
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Place";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PolygonManager manager) { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {
        Event e = Event.current;

        if (!(e.type == EventType.MouseDown && e.button == 0)) return null;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return null;
        
        Vector3 point = ray.direction * t + ray.origin;
        Debug.Log(t + " " + point);
        DebugUtilities.DebugDrawCross(point, Color.red, 2.0f);
        manager.AddPoint(point);
        EditorUtility.SetDirty(manager);

        e.Use();

        return null;
    }
    public override void EndStateMachine(PolygonManager manager) { return; }
}

public class GUI_TestDeletePointStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=orange>Test Delete Point</color></b> \nClick at highlighted point to delete it, closest point to cursor will be highlighted. \nRight click to cancel this tool";
    public override bool NeedRefresh() { return true; }
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Delete";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PolygonManager manager) { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Debug.Log("ПКМ кликнуто");
            e.Use();
            return new GUI_NothingMachine();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return new GUI_NothingMachine();
        Vector3 point = ray.direction * t + ray.origin;

        (int index, float distance) = manager.ClosestPoint(point);
        if (distance >= 0.25f) return this;
        manager.HandlesHighLightPoint(index);

        if (!(e.type == EventType.MouseDown && e.button == 0)) return this;
        manager.RemovePoint(index);
        EditorUtility.SetDirty(manager);
        e.Use();

        return this;
    }
    public override void EndStateMachine(PolygonManager manager) { return; }
}

public class GUI_AddPolygonStateMachine : GUIStateMachine
{
    private const string generic_description = 
        "State: <b><color=orange>Draw Polygon</color></b> \nClick to place points, point will be connected sequentially." +
        "\nPress Right Click to approve a polygon." +
        "\nInvalid polygons will not be allowed to exist." +
        "\nPress <b><color=white>Escape</color></b> to cancel this tool";
    public override bool NeedRefresh() { return true; }
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Draw Polygon";
    public override string GetOptionName() { return generic_option; }
    private List<Vector2> points;
    public override void InitStateMachine(PolygonManager manager) 
    {
        if (points == null) points = new List<Vector2>();
        points.Clear();
        Debug.Log("state machine initialized");
    }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {
        //if (points == null) InitStateMachine();
        DrawPolygon();
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Debug.Log("Отмена действия");
            e.Use();
            return new GUI_NothingMachine();
        }
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Debug.Log("Завершение постройки полигона");
            e.Use();
            CompilePolygon(manager);
            return new GUI_NothingMachine();
        }
        

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return new GUI_NothingMachine();
        Vector3 point = ray.direction * t + ray.origin;

        if (!(e.type == EventType.MouseDown && e.button == 0)) return null;
        this.points.Add(new Vector2(point.x, point.y));
        e.Use();

        return null;
    }
    
    private void CompilePolygon(PolygonManager manager)
    {
        Poly2D poly;
        bool has_compiled = Poly2D.CompilePolygon(this.points, out poly);
        if (!has_compiled) return;

        manager.AddPolygon(poly);
        EditorUtility.SetDirty(manager);
        return;
    }

    private void DrawPolygon()
    {
        Color tmp_color = Handles.color;
        if (this.points.Count >= 3) 
        {
            Handles.color = Color.blue;
            for (int i = 0; i < points.Count - 1; i++)
                Handles.DrawLine(points[i], points[i + 1]);
            Handles.color = Color.cyan;
            Handles.DrawLine(points[points.Count - 1], points[0]);
        }
        Handles.color = tmp_color;

        Color point_color = (points.Count == 1) ? Color.red : (points.Count == 2 ? Color.orange : Color.green);
        for (int i = 0; i < points.Count; i++)
            DebugUtilities.HandlesDrawCross(points[i], point_color);
    }

    public override void EndStateMachine(PolygonManager manager) 
    {
        Debug.Log("Остановленна машина полигонов");
        return; 
    }
}
public class GUI_SelectPolygonStateMachine : GUIStateMachine
{
    private const string generic_description =
        "State: <b><color=green>Select Polygon</color></b> \nClick to select a polygon and see it's data" +
        "\nLeft click to select a polygon" +
        "\nPress <b><color=red>letter D</color></b> to delete a polygon (cancels tool)" +
        "\nPress <b><color=white>Escape</color></b> to cancel this tool\n";
    public override string GetDescription() { return generic_description + PolygonData; }
    private const string generic_option = "Select Polygon";
    public override string GetOptionName() { return generic_option; }

    public override void InitStateMachine(PolygonManager manager)
    {
        Debug.Log("state machine initialized");
    }
    public string PolygonData = "";

    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Debug.Log("Отмена действия");
            e.Use();
            return new GUI_NothingMachine();
        }

        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.D))
        {
            Debug.Log("Удаление полигона");
            e.Use();
            manager.DeleteSelectedPolygon();
            
            return new GUI_NothingMachine();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return new GUI_NothingMachine();
        Vector3 point = ray.direction * t + ray.origin;

        if (!(e.type == EventType.MouseDown && e.button == 0)) return null;
        manager.SelectPolygon(point);
        PolygonData = manager.GetPolygonDataDelegate();
        e.Use();
        this.description_changed = true;

        return null;
    }

    public override void EndStateMachine(PolygonManager manager)
    {
        manager.SelectionPurge();
        Debug.Log("Остановленна машина выбора полигонов");
        return;
    }
}