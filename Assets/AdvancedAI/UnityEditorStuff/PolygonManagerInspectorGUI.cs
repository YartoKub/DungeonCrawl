using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[CustomEditor(typeof(PolygonManager))]
public class PointManagerInspectorGUI : Editor
{
    public GUIStateMachine[] stateMachines = {new GUI_NothingMachine(), new GUI_TestPlacePointStateMachine(), new GUI_TestDeletePointStateMachine(), new GUI_AddPolygonStateMachine()};
    // Это начальные машины состояний которые можно выбрать через меню.
    // Есть недоступные машины состояний, доступ к которым производится толькко взаимодействуя с этиим машинами.

    public string[] options; //= new string[] { "None", "Draw", "Knife", "Select", "Grab", "TestPlacePoint", "TestDeletePoint" };
    public int current_action_index = 0;
    public GUIStateMachine stateMachine;
    public string current_comment = "";

    //public override bool RequiresConstantRepaint() { return true; }
    //Inspector
    public override void OnInspectorGUI()
    {
        options = PopupOptions();
        // Документация отвратительная, ничего не показывают и не рассказывают. Вроде надо использоватб WhiteSpace, но нигде не написано куда его применять
        //ChangeState(current_action_index);
        if (stateMachine == null) ChangeState(0);

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
            if (stateMachine != null) stateMachine.EndStateMachine();
            ChangeState(current_action_index);
        }
        base.OnInspectorGUI();
    }

    public string[] PopupOptions()
    {
        string[] options = new string[this.stateMachines.Length];
        for (int i = 0; i < options.Length; i++) { options[i] = this.stateMachines[i].GetOptionName(); }
        return options;
    }

    public void ChangeState(int ca_index)
    {
        if (ca_index < 0 | ca_index >= stateMachines.Length) ca_index = 0;
        stateMachine = stateMachines[ca_index];
        current_comment = stateMachine.GetDescription();
        current_action_index = ca_index;
    }

    public void ChangeState(GUIStateMachine guism)
    {
        if (guism == null) return;
        stateMachine = guism;
        current_comment = guism.GetDescription();
        for (int i = 0; i < stateMachines.Length; i++)
        {
            if (guism.GetType() == stateMachines[i].GetType()) current_action_index = i;
        }
    }

    void OnSceneGUI()
    {
        PolygonManager manager = (PolygonManager)target;
        if (stateMachine != null)
        {
            GUIStateMachine sm = stateMachine.OnSceneGUI(manager);
            if (stateMachine.NeedRefresh()) { SceneView.RepaintAll(); }
            if (sm != null) this.ChangeState(sm);
        }
    }
}

public abstract class GUIStateMachine
{
    public virtual bool NeedRefresh() { return false; }
    public abstract string GetDescription();
    public abstract string GetOptionName();
    public abstract void InitStateMachine();// Обнуление переменных
    public abstract GUIStateMachine OnSceneGUI(PolygonManager manager);
    public abstract void EndStateMachine(); // Завершение сложной операции и сохранение всего
}

public class GUI_NothingMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=green>Nothing</color></b> \nSelect an option and click 'Draw' button to start an operation";
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "None";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine() { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager){ return this;}
    public override void EndStateMachine() { return; }
}

public class GUI_GrabStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=yellow>Grab</color></b> \nDrag point to move it";
    public override string GetDescription(){ return generic_description; }
    private const string generic_option = "Grab";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine() { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager) { return null; }
    public override void EndStateMachine() { return; }
}
public class GUI_TestPlacePointStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=yellow>Test Place Point</color></b> \nClick to place a point";
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Place";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine() { return; }
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

        e.Use();

        return null;
    }
    public override void EndStateMachine() { return; }
}

public class GUI_TestDeletePointStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=orange>Test Delete Point</color></b> \nClick at highlighted point to delete it, closest point to cursor will be highlighted. \nRight click to cancel this tool";
    public override bool NeedRefresh() { return true; }
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Delete";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine() { return; }
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
        manager.HighLightPointHandles(index);

        

        if (!(e.type == EventType.MouseDown && e.button == 0)) return this;
        manager.RemovePoint(index);
        e.Use();

        return this;
    }
    public override void EndStateMachine() { return; }
}

public class GUI_AddPolygonStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=orange>Draw Polygon</color></b> \nClick to place points, point will be connected sequentially.\nPress Enter to approve a polygon.\nInvalid polygons will not be allowed to exist. \nRight click to cancel this tool";
    public override bool NeedRefresh() { return true; }
    public override string GetDescription() { return generic_description; }
    private const string generic_option = "Draw Polygon";
    public override string GetOptionName() { return generic_option; }
    public override void InitStateMachine() { return; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Debug.Log("ПКМ кликнуто");
            e.Use();
            return new GUI_NothingMachine();
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.KeypadEnter)
        {

        }
        /*
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return new GUI_NothingMachine();
        Vector3 point = ray.direction * t + ray.origin;

        (int index, float distance) = manager.ClosestPoint(point);
        if (distance >= 0.25f) return this;
        manager.HighLightPointHandles(index);



        if (!(e.type == EventType.MouseDown && e.button == 0)) return this;
        manager.RemovePoint(index);*/
        //e.Use();

        return this;
    }
    public override void EndStateMachine() { return; }
}