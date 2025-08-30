using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NavigationSpace : MonoBehaviour
{
    public NavBoxRoom[] boxes;
    public bool compiled = false;
    public int boxCount { get; private set; }

    private bool[] connectionMatrix;

    void Start()
    {
        Initialization();
        this.DumpSelf();
    }

    public void Initialization()
    {
        List<NavBoxCandidate>  candidateBoxes = new List<NavBoxCandidate>();
        foreach (Transform child in this.transform)
        {
            NavBoxCandidate candidate = child.GetComponent<NavBoxCandidate>();
            if (candidate != null)
            {
                Debug.Log(child.name);
                candidateBoxes.Add(candidate);
            } 
        }
        boxCount = candidateBoxes.Count;
        boxes = new NavBoxRoom[boxCount];

        for (int i = 0; i < boxCount; i++)
        {
            boxes[i] = candidateBoxes[i].GetRoom();
            boxes[i].myId = i;
        }

        connectionMatrix = new bool[boxCount * boxCount];
        SetConnectionMatrix(false);

        CheckForConnections();
        compiled = true;
    }

    public void SetConnectionMatrix(bool newValue)
    {
        for (int i = 0; i < connectionMatrix.Length; i++)
        {
            connectionMatrix[i] = newValue;
        }
    }

    public void CheckForConnections()
    {
        for (int i = 0; i < boxes.Length; i++)
        {
            for (int j = i; j < boxes.Length; j++)
            {
                NavBoxRoom boxA = this.boxes[i];
                NavBoxRoom boxB = this.boxes[j];
                this.ChangeValue(boxA.myBounds.Intersects(boxB.myBounds), i, j); 
            }
        }
    }
    
    public void ChangeValue(bool newValue, int x, int y)
    {
        this.connectionMatrix[x + y * this.boxCount] = newValue;
        this.connectionMatrix[y + x * this.boxCount] = newValue;
    }

    public bool DoesConnect(int a_ID, int b_ID)
    {
        return this.connectionMatrix[a_ID + b_ID * boxCount];
    }

    public void DumpSelf()
    {
        string newString = "ConnMatrix | " + this.boxCount + "\n";
        for (int i = 0; i < boxCount; i++)
        {
            for (int j = 0; j < boxCount; j++)
            {
                newString += (DoesConnect(i, j) ? "X" : "_") + " ";
            }
            newString += "\n";
        }

        Debug.Log(newString);
        for (int i = 0; i < boxes.Length; i++)
        {
            Debug.Log(boxes[i]);
        }
    }

    public bool[] GetNeighbours(int roomID)
    {
        bool[] row = new bool[boxCount];
        for (int i = 0; i < boxCount; i++)
        {
            row[i] = connectionMatrix[roomID * boxCount + i];
        }
        return row;
    }
    ///<summary>
    /// Находит расстояния от центра каждой коробки до целевой коробки. 
    /// Несвязанные коробки удалены на положительную бесконечность.
    ///</summary>
    ///<returns>float[]</returns>
    public float[] GetNeighboursDistance(int targetBoxID) 
    {
        float incorrect_value = float.PositiveInfinity;
        float[] row = new float[boxCount];
        for (int i = 0; i < boxCount; i++)
        {
            row[i] = this.connectionMatrix[targetBoxID * boxCount + i] ? boxes[targetBoxID].CenterDistance(boxes[i]) : incorrect_value;
        }
        row[targetBoxID] = incorrect_value; // коробка с текущим ID считается недостижимой.
        return row;
    }

    public List<int> GetNeighboursList(int targetBoxID)
    {
        List<int> list = new List<int>();
        for (int i = 0; i < boxCount; i++)
        {
            //Debug.Log(targetBoxID * boxCount + i + " " + this.connectionMatrix[targetBoxID * boxCount + i].ToString());
            if (this.connectionMatrix[targetBoxID * boxCount + i])
            {
                list.Add(i);
            }
        }
        return list;
    }


    public int NaiveBoxFinder(Transform asker)
    {
        for (int i = 0; i < boxCount; i++)
        {
            if (boxes[i].myBounds.Contains(asker.position)) return i;
        }
        return -1;
    }



}
