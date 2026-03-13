using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
[Serializable]
public class GraphDynamicList : GraphDataStorageDynamic
{
    List<InnerList> connections;
    public override int vCount { get { return connections.Count; } }

    public GraphDynamicList()
    {
        if (this.connections == null) this.connections = new List<InnerList>();
    }
    // =======================================
    // Бонусные штуки
    // =======================================
    [Serializable]
    private class InnerList
    {
        public List<int> c;
        public int cluster_color; // Если существуют отдельные несвязанные группы вершин, то используя это поле можно окрашивать их в разные цвета
        public InnerList()
        {
            this.c = new List<int>();
            cluster_color = -1;
        }
    }
    public void DumpRow(int rowID)
    {
        this.connections[rowID].c.Clear();
    }
    // =======================================
    // STANDARD STUFF
    // =======================================
    public override bool ValidityCheck()
    {
        if (connections == null) return false;
        for (int i = 0; i < connections.Count; i++)
        {
            if (this.connections[i] == null) return false;
        }
        return true;
    }
    public override void DumpSelf()
    {
        string list_form = "List form: " + this.connections.Count +" \n";
        for (int c = 0; c < this.connections.Count; c++)
        {
            string list_of_connections = " ";
            for (int i = 0; i < this.connections[c].c.Count; i++) list_of_connections += this.connections[c].c[i] + ",";
            list_form += "(" + c + (this.connections[c].c == null ? " null" : " okay") + " | " + list_of_connections + ")\n";
        }
        Debug.Log(list_form);
    }
    
    public override int GetNodeEdgeCount(int rowID) { return this.connections[rowID].c.Count; }
    
    public override bool[] GetSliceArray(int rowID)
    {
        bool[] row_cons = new bool[this.connections.Count];
        for (int i = 0; i < this.connections[rowID].c.Count; i++) row_cons[this.connections[rowID].c[i]] = true;
        return row_cons;
    }
    
    public override float[] GetSliceDistance(int rowID)
    {
        // Пока нет смысла в этой функции, CH2D polygon не содержит информации о вершинах.
        throw new System.NotImplementedException();
    }
    
    public override List<int> GetSliceIDList(int rowID) {return new List<int>(this.connections[rowID].c); }
    public override bool GetValue(int a_ID, int b_ID) { return this.connections[a_ID].c.Contains(b_ID); }

    public override int NaiveNodeFinder(Transform asker)
    {
        // Пока я не работаю с трансформами, так что тут смысла нет ничего писать пока
        throw new System.NotImplementedException();
    }
    
    public override void SetValue(bool newValue, int x, int y)
    {
        if (newValue)   // Добавление связи
        {
            AddConnction(x, y);
            AddConnction(y, x);
        }
        else // Удаление связи
        {
            this.connections[x].c.Remove(y);
            this.connections[y].c.Remove(x);
        }
    }

    private void AddConnction(int x, int y)
    {   // Вставляет индекс вершины так, чтобы не нарушать упорядоченность массива.
        int index = 0;
        //Debug.Log("Current list " + x  +": " + DebugUtilities.DebugListString(this.connections[x].c.ToArray()));
        for (int i = 0; i < this.connections[x].c.Count; i++)
        {
            if (this.connections[x].c[i] == y) return;
            if (this.connections[x].c[i] > y) break;
            index = i + 1;
        }
        this.connections[x].c.Insert(index, y);
    }
    
    protected override void establishConnections()
    {
        // Эта штука тоже в некотором роде рудементарна, и она не работает без IGraphNode.
        // Проблема в том что IGraph node сложно прикрутить к CH2D_Polygon, так как он не хранит вершины, а только их индексы в чанковом массиве вершин
        throw new System.NotImplementedException();
    }
   
    protected override void setConnectionMatrix(bool newValue)
    {
        // Рудиментарна, тут даже матрицы нет и bool не хранятся
        throw new System.NotImplementedException();
    }
    public override int AddPoint()
    {
        this.connections.Add(new InnerList());
        //this.vCount = this.connections.Count;
        return this.connections.Count - 1; 
        //throw new System.NotImplementedException();
    }
    public override bool DeletePoint(int index)
    {
        if (index < 0 | index >= this.connections.Count) return false;
        // Удаление существующих связей между index и другими вершинами
        List<int> remove_connection = this.connections[index].c;
        for (int i = 0; i < remove_connection.Count; i++) this.connections[remove_connection[i]].c.Remove(index);
        // Перепись всех связей что идут после index
        for (int c = 0; c < connections.Count; c++)
            for (int i = 0; i < connections[c].c.Count; i++)
                if (connections[c].c[i] > index) connections[c].c[i] = connections[c].c[i] - 1; ;

        this.connections.RemoveAt(index);
        //this.vCount = this.connections.Count;
        return true;
    }
    
}
