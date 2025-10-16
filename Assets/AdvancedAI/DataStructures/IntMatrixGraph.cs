using System.Collections.Generic;
using UnityEngine;

public class IntMatrixGraph : GraphDataStorage
{
    //public int vCount; // У родительского касса
    public bool[] connections;
    //public List<IGraphNode> vertices;

    public IntMatrixGraph(List<IGraphNode> _vertices)
    {
        vCount = _vertices.Count;
        connections = new bool[vCount * vCount];

        this.setConnectionMatrix(false);
        this.establishConnections();
    }

    public IntMatrixGraph(int VCount)
    {
        vCount = VCount;
        connections = new bool[vCount * vCount];

        //this.setConnectionMatrix(false);
        //this.establishConnections();
    }

    public override void SetValue(bool newValue, int x, int y)
    {
        this.connections[x + y * this.vCount] = newValue;
        this.connections[y + x * this.vCount] = newValue;
    }

    public void SetValueSafe(bool newValue, int x, int y)
    {
        if (x == -1 | y == -1) return;
        if (x >= vCount | y >= vCount) return;
        this.connections[x + y * this.vCount] = newValue;
        this.connections[y + x * this.vCount] = newValue;
    }

    public override bool GetValue(int a_ID, int b_ID)
    {
        return this.connections[a_ID + b_ID * vCount];
    }
    
    public override int NaiveNodeFinder(Transform asker)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].IDoesContainPoint(asker.position) )
            {
                return i;
            }
        }
        return -1;
    }

    public override bool[] GetSliceArray(int rowID)
    {
        bool[] toReturn = new bool[vCount];
        for (int i = 0; i < vCount; i++)
        {
            toReturn[i] = GetValue(i, rowID);
        }
        return toReturn;
    }

    public override float[] GetSliceDistance(int rowID)
    {
        
        float[] toReturn = new float[vCount];
        for (int i = 0; i < vCount; i++)
        {
            toReturn[i] = GetValue(i, rowID) ? vertices[rowID].IGetDistance(vertices[i]) : float.PositiveInfinity;
        }
        return toReturn;
    }

    public override List<int> GetSliceIDList(int rowID)
    {
        List<int> toReturn = new List<int>();
        for (int i = 0; i < vCount; i++)
        {
            if (GetValue(i, rowID))
            {
                toReturn.Add(i);
            }
        }
        return toReturn;
    }

    public override int GetNodeEdgeCount(int rowID)
    {
        int toReturn = 0;
        for (int i = 0; i < vCount; i++)
        {
            toReturn += GetValue(i, rowID) ? 1 : 0;
        }
        return toReturn;
    }

    protected override void establishConnections()
    { // пока идея в том что пути не будут меняться
        for (int i = 0; i < vertices.Length; i++)
        {
            for (int j = i; j < vertices.Length; j++)
            {
                IGraphNode nodeA = this.vertices[i];
                IGraphNode nodeB = this.vertices[j];
                this.SetValue(nodeA.ICheckAdjacency(nodeB), i, j);
            }
        }
    }
    protected override void setConnectionMatrix(bool newValue)
    {
        for (int i = 0; i < connections.Length; i++) connections[i] = newValue;
        // Не самая полезная функция, по дефолту bool в C# равен false. 
        // Наличие этой функции обусловленно наличием PTSD после работы с C++
    }

    public override void DumpSelf()
    {
        string newString = "ConnMatrix | " + this.vCount + "\n";
        for (int i = 0; i < vCount; i++)
        {
            for (int j = 0; j < vCount; j++)
            {
                newString += (GetValue(i, j) ? "X" : "O") + " ";
            }
            newString += "\n";
        }
        Debug.Log(newString);
    }
}
