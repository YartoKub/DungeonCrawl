using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Это родительский объект для работы с графами.
// Он должен брать на себя хранение связей и объектов.
// Поддерживаемые объекты должны предоставлять интерфейсы для проверки связанности двух объектов этого типа.
// Но мне лень делать интерфейсы, потом реализую

// Также он должен искать путь от комнаты А до комнаты Б. 
// Возвращаться должен следующий шаг к цели. 
// Перемещением сущности внутри комнаты занимается сама сущность, на нее также будет повещена работа обходить других сущностей и объекты внутри комнаты
public class GraphManager : MonoBehaviour
{
    public static GraphManager mainGraph { get; private set; }
    // Родительский супер-граф. Самый главный. 
    // Вообще план такой: для каждой локации создается граф из всех нод
    // Далее он разбивается на меньшие подграфы, каждый меньший подграф является вершиной изначального графа.
    // Так происходит рекурсивное разбитие на маленькие кусочки
    // Изначальные графы тоже группируются вместе
    // Также каждая отдельная комната будет иметь свой граф. Так, комната с колонной в центре будет разделена на 4 части, связанные в бублик
    // Хотя для каждой отдельной комнаты можно использовать и A*

    public List<NavBoxInt> boxes;
    public int myId;
    public GraphDataStorage graph;

    public bool DebugDrawBoxes;
    public bool DebugDrawConnections;


    public GraphManager()
    {
        this.myId = 0;
        this.boxes = new List<NavBoxInt>();
    }




    // ACTUAL РАБОТА С ГРАФАМИ




    public void CompileGraph()
    { // Компилирует локальный граф. Предполагается что когда блок комнат закончит генерацию, он создаст свой граф, загрузит в него комнаты, а затем скомпилирует.
      // То же самое может сработать с блочными пространствами
        this.graph = new FullMatrixGraph(boxes);
    }

    public static void StaticRegisterBox(Vector3Int A, Vector3Int B)
    {
        if (mainGraph == null) return;
        mainGraph.RegisterBox(A, B);
    }
    public void RegisterBox(Vector3Int A, Vector3Int B)
    {
        //Debug.Log(" " + A + " " + B + " " + this.boxes.Count);
        int listCount = this.boxes.Count;
        this.boxes.Add(new NavBoxInt(A, B, listCount, this.myId));
    }
    /*
    public void DumpBoxList() {
        Debug.Log("У меня " + this.boxes.Count + " коробок");
        foreach (var item in graph.vertices) Debug.Log(item);
    }*/

// Прочее
    private void Start() {
        // DEbug generator, заменить позже
        foreach (Transform child in this.transform) {
            ManualNavBoxPlacer candidate = child.GetComponent<ManualNavBoxPlacer>();
            if (candidate != null)
                this.RegisterBox(candidate.min, candidate.min + candidate.size);
        }
        NominateSelf();
        CompileGraph();
        graph.DumpSelf();
    }
    private void NominateSelf() {
        if (GraphManager.mainGraph == null) GraphManager.mainGraph = this;
    }
    private void Update() {
        if (DebugDrawBoxes) foreach (NavBoxInt item in boxes) BoundsMathHelper.DebugDrawBox(item.A, item.size);
        if (DebugDrawConnections) {
            List<Vector3> pairs = graph.DumpConnectionPairs();
            if (pairs.Count != 0) {
                for (int i = 0; i < pairs.Count / 2; i++)
                    Debug.DrawLine(pairs[i * 2], pairs[i * 2 + 1], Color.yellow);
            }
            
        }

    }
}


/*
[CustomEditor(typeof(GraphManager))] 
public class MyComponentEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector(); 
        GraphManager myComponent = (GraphManager)target;
        if (GUILayout.Button("Dump boxes"))
        {
            myComponent.DumpBoxList();
        }
    }
}
*/

