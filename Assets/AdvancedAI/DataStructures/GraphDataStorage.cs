using System.Collections.Generic;
using UnityEngine;

// Это объект содержащий в себе отношения между нодами графа.
// Его наследники сами имплементируют структуру хранения, и функции доступа
public abstract class GraphDataStorage
{
    public IGraphNode[] vertices;
    public int vCount;

    
    // доступ к изменению навигационного пути должен быть только у графа
    // Изменение связей должно происходить когда добавляется новая комната, либо же изменяется размер старой
    // Пока что графы реализованы как статические, неизменные конструкции
    public abstract int NaiveNodeFinder(Transform asker);   // Находит ноду, в которой находится объект

    // GETTERS
    public abstract bool GetValue(int a_ID, int b_ID); // Наличие связи комнаты А и Б
    public abstract bool[] GetSliceArray(int rowID);
    public abstract List<int> GetSliceIDList(int rowID);
    public abstract float[] GetSliceDistance(int rowID);
    public abstract int GetNodeEdgeCount(int rowID);

    // INITIALIZATION
    public abstract void SetValue(bool newValue, int x, int y);
    protected abstract void setConnectionMatrix(bool newValue);
    protected abstract void establishConnections();
    // DEBUG
    public abstract void DumpSelf();
}

public interface IGraphNode
{
    public bool IDoesContainPoint(Vector3 pos);
    public bool IDoesContainPoint2D(Vector2 pos);
    public bool ICheckAdjacency(IGraphNode other);
    public float IGetDistance(IGraphNode other);
    public string IDebugSelf();
}