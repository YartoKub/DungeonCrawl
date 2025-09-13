using System.Collections.Generic;
using UnityEngine;

public abstract class GraphDataStorage
{
    //public NavBoxInt[] vertices;
    public int vCount;

    
    // доступ к изменению навигационного пути должен быть только у графа
    // Изменение связей должно происходить когда добавляется новая комната, либо же изменяется размер старой
    // Пока что графы реализованы как статические, неизменные конструкции
    public abstract int NaiveBoxFinder(Transform asker);

    // GETTERS
    public abstract bool GetValue(int a_ID, int b_ID); // Наличие связи комнаты А и Б
    public abstract bool[] GetSliceArray(int rowID);
    public abstract List<int> GetSliceIDList(int rowID);
    //public abstract List<NavBoxInt> GetSliceBoxList(int rowID);
    public abstract float[] GetSliceDistance(int rowID);
    public abstract int GetNodeEdgeCount(int rowID);

    // INITIALIZATION
    protected abstract void SetValue(bool newValue, int x, int y);
    protected abstract void setConnectionMatrix(bool newValue);
    protected abstract void establishConnections();
    // DEBUG
    public abstract void DumpSelf();
    public abstract List<Vector3> DumpConnectionPairs();
}
