using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PointCloudManager))]
public class PointCloudManagerEditor : AbstractManagerEditor<PointCloudManager>
{
    public GUIStateMachine<PointCloudManager>[] myStateMachines = {
        new GUI_PCM_NothingMachine(),
        new GUI_PCM_TestPlacePointStateMachine(),
        new GUI_PCM_TestDeletePointStateMachine(),
    };
    protected override GUIStateMachine<PointCloudManager>[] stateMachines { get { return myStateMachines; } }

    public override void OnInspectorGUI()
    {
        PointCloudManager manager = (PointCloudManager)target;
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape
            && stateMachine.GetType() != new GUI_NothingMachine().GetType())
        {
            Debug.Log("Действие завершено");
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
        EditorGUILayout.LabelField("New Ghost points");
        if (GUILayout.Button("Generate new points")) manager.GeneratePseudoNewPoints();
        if (GUILayout.Button("Purge new points")) manager.PurgePNPoints();
        if (GUILayout.Button("Singular Value Decomposition")) manager.ClosestPointMatching();

        base.OnInspectorGUI();
    }

    void OnSceneGUI()
    {
        PointCloudManager manager = (PointCloudManager)target;
        if (stateMachine != null)
        {
            GUIStateMachine<PointCloudManager> sm = stateMachine.OnSceneGUI(manager);
            if (stateMachine.NeedRefresh()) { SceneView.RepaintAll(); }
            if (sm != null) { this.stateMachine.EndStateMachine(manager); this.ChangeState(sm, manager); }
            if (stateMachine.description_changed) { stateMachine.description_changed = false; current_comment = stateMachine.GetDescription(); }
        }
    }
    private void OnDestroy() => stateMachine.EndStateMachine((PointCloudManager)target);
}
public class GUI_PCM_NothingMachine : GUIStateMachine<PointCloudManager>
{
    private const string generic_description = "State: <b><color=green>Nothing</color></b> \nSelect an option to start an operation";
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "None";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PointCloudManager manager) { return; }
    public override GUIStateMachine<PointCloudManager> OnSceneGUI(PointCloudManager manager) { return this; }
    public override void EndStateMachine(PointCloudManager manager) { return; }
}
public class GUI_PCM_TestPlacePointStateMachine : GUIStateMachine<PointCloudManager>
{
    private const string generic_description = "State: <b><color=yellow>Test Place Point</color></b> \nClick to place a point";
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Place";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PointCloudManager manager) { return; }
    public override GUIStateMachine<PointCloudManager> OnSceneGUI(PointCloudManager manager)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Debug.Log("ПКМ кликнуто");
            e.Use();
            return new GUI_PCM_NothingMachine();
        }

        if (!(e.type == EventType.MouseDown && e.button == 0)) return null;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return null;

        Vector3 point = ray.direction * t + ray.origin;

        manager.AddPoint(point);
        EditorUtility.SetDirty(manager);

        e.Use();

        return null;
    }
    public override void EndStateMachine(PointCloudManager manager) { return; }
}

public class GUI_PCM_TestDeletePointStateMachine : GUIStateMachine<PointCloudManager>
{
    private const string generic_description = "State: <b><color=orange>Test Delete Point</color></b> \nClick at highlighted point to delete it, closest point to cursor will be highlighted. \nRight click to cancel this tool";
    public override bool NeedRefresh() { return true; }
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Delete";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine(PointCloudManager manager) { return; }
    public override GUIStateMachine<PointCloudManager> OnSceneGUI(PointCloudManager manager)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Debug.Log("ПКМ кликнуто");
            e.Use();
            return new GUI_PCM_NothingMachine();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return new GUI_PCM_NothingMachine();
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
    public override void EndStateMachine(PointCloudManager manager) { return; }
}