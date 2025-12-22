using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


public abstract class AbstractManagerEditor<T> : Editor where T : class
{
    // public abstract void OnInspectorGUI() { } // Этот уже абстрактный
    protected abstract GUIStateMachine<T>[] stateMachines { get; }

    public string[] options; //= new string[] { "None", "Draw", "Knife", "Select", "Grab", "TestPlacePoint", "TestDeletePoint" };
    public int current_action_index = 0;
    public GUIStateMachine<T> stateMachine;
    public string current_comment = "";
    public string[] PopupOptions()
    {
        string[] options = new string[this.stateMachines.Length];
        for (int i = 0; i < options.Length; i++) { options[i] = this.stateMachines[i].GetOptionName(); }
        return options;
    }

    public void ChangeState(int ca_index, T target)
    {
        if (ca_index < 0 | ca_index >= stateMachines.Length) ca_index = 0;
        stateMachine = stateMachines[ca_index];
        current_comment = stateMachine.GetDescription();
        current_action_index = ca_index;
        stateMachine.InitStateMachine(target);
    }
    public void ChangeState(GUIStateMachine<T> guism, T target)
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


}



public abstract class GUIStateMachine<T> where T : class
{
    public bool description_changed = false;
    public virtual bool NeedRefresh() { return false; }
    public abstract string GetDescription();
    public abstract string GetOptionName();
    public abstract void InitStateMachine(T manager);// Обнуление переменных
    public abstract GUIStateMachine<T> OnSceneGUI(T manager);
    public abstract void EndStateMachine(T manager); // Завершение сложной операции и сохранение всего
}
