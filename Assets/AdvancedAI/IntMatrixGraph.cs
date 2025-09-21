using System.Collections.Generic;
using UnityEngine;

public class IntMatrixGraph : GraphDataStorage
{
    //public int vCount; // У родительского касса
    public bool[] connections;

    public IntMatrixGraph(List<NavBoxInt> _vertices)
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
    
    public override int NaiveBoxFinder(Transform asker)
    {
        Debug.Log("UNILMPLEMENTED");
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
        /*
        float[] toReturn = new float[vCount];
        for (int i = 0; i < vCount; i++)
        {
            toReturn[i] = GetValue(i, rowID) ? BoundsMathHelper.CenterDistance(vertices[i].bounds, vertices[rowID].bounds) : float.PositiveInfinity;
        }*/
        Debug.Log("NOT IMPLEMENTED");
        return new float[1];
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
        /*
        for (int i = 0; i < vertices.Length; i++)
        {
            for (int j = i; j < vertices.Length; j++)
            {
                NavBoxInt boxA = this.vertices[i];
                NavBoxInt boxB = this.vertices[j];
                this.SetValue(boxA.DoesIntersect(boxB.bounds), i, j);
            }
        }*/
        Debug.Log("Does not have an ability to establih commnenetsts");
    }
    protected override void setConnectionMatrix(bool newValue)
    {
        for (int i = 0; i < connections.Length; i++)
            connections[i] = newValue;
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

    public override List<Vector3> DumpConnectionPairs()
    {
        Debug.Log("Does not have an ability to establih commnenetsts");
        List<Vector3> pairs = new List<Vector3>();
        /*
        for (int i = 0; i < vCount; i++)
        {
            for (int j = 0; j < vCount; j++)
            {
                if (GetValue(i, j))
                {
                    pairs.Add(this.vertices[i].bounds.center);
                    pairs.Add(this.vertices[j].bounds.center);
                }
            }
        }*/
        return pairs;
    }
}
