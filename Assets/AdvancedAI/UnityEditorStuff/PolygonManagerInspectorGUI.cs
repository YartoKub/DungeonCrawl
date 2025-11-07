using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
[CustomEditor(typeof(PolygonManager))]
public class PointManagerInspectorGUI : Editor
{
    public string[] options = new string[] { "None", "Draw", "Knife", "Select", "Grab", "TestPlacePoint", "TestDeletePoint" };
    public int current_action_index = 0;
    public GUIStateMachine stateMachine;
    public string current_comment = "";

    //Inspector
    public override void OnInspectorGUI()
    {
        // Документация отвратительная, ничего не показывают и не рассказывают. Вроде надо использоватб WhiteSpace, но нигде не написано куда его применять
        ChangeState(current_action_index);
        base.OnInspectorGUI();

        GUIStyle wrappedTextStyle = new GUIStyle(EditorStyles.textField);
        wrappedTextStyle.wordWrap = true;
        wrappedTextStyle.richText = true;

        EditorGUILayout.LabelField("Choose action to edit polygons");

        EditorGUILayout.TextField(current_comment, wrappedTextStyle, GUILayout.Height(150));

        int prev_action_index = current_action_index;
        current_action_index = EditorGUILayout.Popup(current_action_index, options);
        if (prev_action_index != current_action_index)
        {
            if (stateMachine != null) stateMachine.EndStateMachine();
            ChangeState(current_action_index);
        }

    }

    public void ChangeState(int ca_index)
    {

        switch (ca_index) {
            case 0: 
                stateMachine = new GUI_NothingMachine();
                break;
            case 4:
                stateMachine = new GUI_GrabStateMachine();
                break;
            case 5:
                stateMachine = new GUI_TestPlacePointStateMachine();
                break;
            case 6:
                stateMachine = new GUI_TestDeletePointStateMachine();
                break;
            default:
                stateMachine = new GUI_NothingMachine();
                break;
        }
        current_comment = stateMachine.GetDescription();
    }

    public void ChangeState(GUIStateMachine guism)
    {
        if (guism == null) guism = new GUI_NothingMachine();
        stateMachine = guism;
        current_comment = stateMachine.GetDescription();
    }


    void OnSceneGUI()
    {
        PolygonManager manager = (PolygonManager)target;

        //if (!manager.acceptNewPoints) return;
        if (stateMachine != null)
        {
            GUIStateMachine sm = stateMachine.OnSceneGUI(manager);
            if (sm.NeedRefresh()) SceneView.RepaintAll();
            this.ChangeState(sm);
        }
        

    }
}

public abstract class GUIStateMachine
{
    public virtual bool NeedRefresh() { return false; }
    public abstract string GetDescription();
    public abstract GUIStateMachine OnSceneGUI(PolygonManager manager);
    public abstract void EndStateMachine();
}

public class GUI_NothingMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=green>Nothing</color></b> \nSelect an option and click 'Draw' button to start an operation";
    public override string GetDescription() { return generic_description; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager){ return this;}
    public override void EndStateMachine() { return; }
}

public class GUI_GrabStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=yellow>Grab</color></b> \nDrag point to move it";
    public override string GetDescription(){ return generic_description; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager) { return null; }
    public override void EndStateMachine() { return; }
}
public class GUI_TestPlacePointStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=yellow>Test Place Point</color></b> \nClick to place a point";
    public override string GetDescription() { return generic_description; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {
        Event e = Event.current;

        if (!(e.type == EventType.MouseDown && e.button == 0)) return this;
        
        Debug.Log("Попытка добавить новую точку к объекту " + manager);

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return null;
        
        Vector3 point = ray.direction * t + ray.origin;
        Debug.Log(t + " " + point);
        DebugUtilities.DebugDrawCross(point, Color.red, 2.0f);
        manager.AddPoint(point);

        e.Use();

        return this;
    }
    public override void EndStateMachine() { return; }
}

public class GUI_TestDeletePointStateMachine : GUIStateMachine
{
    private const string generic_description = "State: <b><color=orange>Test Delete Point</color></b> \nClick at highlighted point to delete it, closest point to cursor will be highlighted";
    public override bool NeedRefresh() { return true; }
    public override string GetDescription() { return generic_description; }
    public override GUIStateMachine OnSceneGUI(PolygonManager manager)
    {
        //Handles.BeginGUI();

        Event e = Event.current;


        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane planeXY = new Plane(new Vector3(0, 0, 1), 0);

        if (!planeXY.Raycast(ray, out float t)) return this;
        Vector3 point = ray.direction * t + ray.origin;
        
        //DebugUtilities.DebugDrawCross(point, Color.yellow, 2.0f);
        

        (int index, float distance) = manager.ClosestPoint(point);
        if (distance >= 0.25f) return this;
        manager.HighLightPointHandles(index);

        

        if (!(e.type == EventType.MouseDown && e.button == 0)) return this;
        manager.RemovePoint(index);
        e.Use();
        //Handles.EndGUI();
        return this;
    }
    public override void EndStateMachine()
    {
        return;
    }
}