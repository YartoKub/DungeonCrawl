using System.Collections.Generic;
using UnityEngine;

// Это объект содержащий в себе отношения между нодами графа.
// Его наследники сами имплементируют структуру хранения, и функции доступа
public abstract class GraphDataStorage
{
    public IGraphNode[] vertices;
    public abstract int vCount { get; }


    // доступ к изменению навигационного пути должен быть только у графа
    // Изменение связей должно происходить когда добавляется новая комната, либо же изменяется размер старой
    // Пока что графы реализованы как статические, неизменные конструкции
    /// <summary>
    /// Goal of this function is to identify to which node does transform belong, and return -1 if it is outside of nodes
    /// </summary>
    /// <param name="asker"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract int NaiveNodeFinder(Transform asker);   // Находит ноду, в которой находится объект

    // GETTERS
    /// <summary>
    /// Function to get a value of whetether node X connects to node Y
    /// </summary>
    /// <param name="a_ID"></param>
    /// <param name="b_ID"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract bool GetValue(int a_ID, int b_ID); // Наличие связи комнаты А и Б
    /// <summary>
    /// This function returns all connections that node X (one out of N) has with (0-N) nodes
    /// <br/>So with every node before X, with X itself, and every node after X. 
    /// <br/>
    /// </summary>
    /// <param name="rowID"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract bool[] GetSliceArray(int rowID);
    /// <summary>
    /// Returns a list of nodes to which X is connected to. 
    /// </summary>
    /// <param name="rowID"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract List<int> GetSliceIDList(int rowID);
    /// <summary>
    /// Returns a slice of all connections that node X (one out of N) has with (0-N) nodes, but measures distance.
    /// <br/> If nodes are not connected, it must return PositiveInfinity
    /// </summary>
    /// <param name="rowID"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract float[] GetSliceDistance(int rowID);
    /// <summary>
    /// Counts the amount of connection node X has with all other nodes
    /// </summary>
    /// <param name="rowID"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract int GetNodeEdgeCount(int rowID);

    // INITIALIZATION
    /// <summary>
    /// Sets whether an X vertice has a connection to Y vertice. 
    /// </summary>
    /// <param name="newValue"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract void SetValue(bool newValue, int x, int y);
    /// <summary>
    /// Sets all connections of a matrix to a certain value, used mainly for setting a default value. 
    /// <br/> This function is a remnant of first iteration of matrix graph class, in C# all bool values are false by default, and it is bool type, not int or float, not so useful
    /// </summary>
    /// <param name="newValue"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    protected abstract void setConnectionMatrix(bool newValue);
    /// <summary>
    /// it checks every possible pair of nodes to see whetehr they are connected, and sets this value into the graph via SetValue function.
    /// <br/> Basically, it is a fire and forget function that does an expensive check to set the Graph into a filled state.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    protected abstract void establishConnections();
    // DEBUG
    /// <summary>
    /// This thing has to somehow Debug.Log itself
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
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