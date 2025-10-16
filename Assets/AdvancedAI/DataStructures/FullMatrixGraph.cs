using System.Collections.Generic;
using UnityEngine;

public class FullMatrixGraph : GraphDataStorage
{
    public new NavBoxInt[] vertices; // ” родительского класса
    //public int vCount; // ” родительского касса
    public bool[] connections;

    public FullMatrixGraph(List<NavBoxInt> _vertices)
    {
        vCount = _vertices.Count;
        vertices = new NavBoxInt[vCount];
        connections = new bool[vCount * vCount];

        for (int i = 0; i < vCount; i++)
        {
            vertices[i] = _vertices[i];
        }
        this.setConnectionMatrix(false);
        this.establishConnections();
    }

    public override void SetValue(bool newValue, int x, int y)
    {
        this.connections[x + y * this.vCount] = newValue;
        this.connections[y + x * this.vCount] = newValue;
    }

    public override bool GetValue(int a_ID, int b_ID)
    {
        return this.connections[a_ID + b_ID * vCount];
    }

    public override int NaiveNodeFinder(Transform asker)
    {
        for (int i = 0; i < vCount; i++)
        {
            if (vertices[i].DoesContainPoint(asker.position)) return i;
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
            toReturn[i] = GetValue(i, rowID) ? BoundsMathHelper.CenterDistance( vertices[i].bounds, vertices[rowID].bounds) : float.PositiveInfinity;
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

    public List<NavBoxInt> GetSliceBoxList(int rowID)
    {
        List<NavBoxInt> toReturn = new List<NavBoxInt>();
        for (int i = 0; i < vCount; i++)
        {
            if (GetValue(i, rowID))
            {
                toReturn.Add(vertices[i]);
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

    protected override void establishConnections() { // пока иде€ в том что пути не будут мен€тьс€
        for (int i = 0; i < vertices.Length; i++) {
            for (int j = i; j < vertices.Length; j++) {
                NavBoxInt boxA = this.vertices[i];
                NavBoxInt boxB = this.vertices[j];
                this.SetValue(boxA.DoesIntersect(boxB.bounds), i, j);
            }
        }
    }
    protected override void setConnectionMatrix(bool newValue) {
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
                newString += (GetValue(i, j) ? "X" : "_") + " ";
            }
            newString += "\n";
        }

        Debug.Log(newString);
        for (int i = 0; i < vertices.Length; i++)
        {
            Debug.Log(vertices[i]);
        }
    }

    public List<Vector3> DumpConnectionPairs(){
        List<Vector3> pairs = new List<Vector3>();
        for (int i = 0; i < vCount; i++) {
            for (int j = 0; j < vCount; j++) {
                if (GetValue(i, j)) {
                    pairs.Add(this.vertices[i].bounds.center);
                    pairs.Add(this.vertices[j].bounds.center);
                }
            }
        }

        return pairs;
    }
}
