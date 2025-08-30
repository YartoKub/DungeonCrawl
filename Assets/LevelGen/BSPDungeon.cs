using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Если немного модифицировать код то это можно преобразовать в независимый блок
// И из многих таких независимых блоков построить более сложное подземелье
public class BSPDungeon : MonoBehaviour
{
    [SerializeField]
    public Vector3Int GridSizeXYZ;
    public Vector3Int perfectRoomSize;
    public Vector3Int minimalRoomSize;

    public Vector3 splitMaxDeviation; // Значения должны быть меньше 0.5
    public int MaxDepth;


    public int debugDepthInt;
    public GameObject debugPrefabBox;
    public Material debugMaterial;
    public CubeWorld debugCubeworld;

    public List<DefaultRoom> BSPHierarchy;
    public List<DefaultRoom> actualRooms;

    void Start()
    {
        //Rect3D startRoom = new Rect3D(Vector3Int.zero, GridSizeXYZ);
        //BSPHierarchy.Clear();
        GenerateBSP();
    }

    void Update()
    {
        
    }

    void GenerateBSP()
    {
        BSPHierarchy = new List<DefaultRoom>();

        BSPHierarchy.Add(new DefaultRoom(Vector3Int.zero, GridSizeXYZ, -1, 0));

        bool validCandidatesPresent = true; // Это означает что есть комнаты которые можно разделить.
        int safety = 0;
        int start = 0; int end = 1; // эти значения будут меняться динамически
        while (safety < MaxDepth && validCandidatesPresent) 
        {
            safety += 1;
            for (int i = start; i < end; i++)
            {
                int lastId = BSPHierarchy.Count;
                Vector3Int axis = simpleAxisSplitDecider(BSPHierarchy[i].myRect);
                //Debug.Log(axis);
                if (axis == Vector3Int.zero) continue;
                List<DefaultRoom> newRooms = RandomSplitOneToTwo(BSPHierarchy[i], axis, i, lastId);
                BSPHierarchy.Add(newRooms[0]);
                BSPHierarchy.Add(newRooms[1]);
            }
            start = end;
            end = BSPHierarchy.Count;
        }

        PickActualRooms();

        //Debug.Log("listing begun");
        foreach (DefaultRoom item in actualRooms)
        {
            if (debugDepthInt == -1) {
                DebugPreviewInstantiate(item);
                Debug.Log(item);
            }

            else if (item.depth == debugDepthInt) {
                DebugPreviewInstantiate(item);
                Debug.Log(item);
            }
        }



    }

    private void DebugPreviewInstantiate(DefaultRoom roomToCreate)
    {
        GameObject newObject = Instantiate(debugPrefabBox, Vector3.zero + roomToCreate.myRect.bounds.min * 0.1f, new Quaternion(0,0,0,0));
        newObject.GetComponent<MeshFilter>().mesh = GeometryGeneration.AxisAlignedBox(roomToCreate.myRect);
        newObject.GetComponent<MeshRenderer>().material = debugMaterial;
    }

    // Довольно сложный код. Здесь нет разделения на X Y и Z, все зависит от входного параметра axis.
    private List<DefaultRoom> RandomSplitOneToTwo(DefaultRoom roomToSplit, Vector3Int axis, int parentID, int last_id)
    { // axis - вектор с одной единицей и двумя нулями.

        // ===== Рассчет случайной величены =====
        int totalSideLength = (int)XYZ_Summ(roomToSplit.myRect.size * axis);
        float RandomOffset = Random.value;
        RandomOffset = (RandomOffset * 2 - 1) * (axis.x * this.splitMaxDeviation.x + axis.y * this.splitMaxDeviation.y + axis.z * this.splitMaxDeviation.z);
        RandomOffset = 0.5f + RandomOffset;

        int child1_side_length = (int)Mathf.Ceil(RandomOffset * totalSideLength);
        int child2_side_length = totalSideLength - child1_side_length;

        // ===== Слишком мелкие комнаты увеличиваются до требуемых значений за счет братской комнаты =====
        int minsummsize = (int)XYZ_Summ(axis * minimalRoomSize);
        //Debug.Log("Child side lengths:nch" + child1_side_length.ToString() + " " + child2_side_length.ToString());
        if (child1_side_length < minsummsize)
        {
            int diff = minsummsize - child1_side_length;
            child1_side_length = child1_side_length + diff;
            child2_side_length = child2_side_length - diff;
        }
        if (child2_side_length < minsummsize)
        {
            int diff = minsummsize - child2_side_length;
            child2_side_length = child2_side_length + diff;
            child1_side_length = child1_side_length - diff;
        }
        //Debug.Log("Child side lengths:   " + child1_side_length.ToString() + " " + child2_side_length.ToString());
        
        // ===== Назначение родителей и детей =====
        List<DefaultRoom> splits = new List<DefaultRoom>();
        splits.Add(new DefaultRoom(roomToSplit.myRect.A, roomToSplit.myRect.B - axis * child1_side_length, parentID, roomToSplit.depth + 1));
        splits.Add(new DefaultRoom(roomToSplit.myRect.A + axis * child2_side_length, roomToSplit.myRect.B, parentID, roomToSplit.depth + 1));
        roomToSplit.myChildren =  new int[2] { last_id, last_id  + 1};

        return splits;
    }

    private Vector3Int simpleAxisSplitDecider(Rect3D originalRoom)
    {
        int optionX = 0; int optionY = 0; int optionZ = 0;

        if (originalRoom.size.x >= minimalRoomSize.x * 2) optionX = 4;
        if (originalRoom.size.y >= minimalRoomSize.y * 2) optionY = 2;
        if (originalRoom.size.z >= minimalRoomSize.z * 2) optionZ = 1;

        float choice = Random.value;
        float[] distribution;
        switch (optionX | optionY | optionZ)
        {
            case 0: return Vector3Int.zero; // Ничего

            case 1: return new Vector3Int(0, 0, 1); // Только Z
            case 2: return new Vector3Int(0, 1, 0); // Только Y
            case 4: return new Vector3Int(1, 0, 0); // Только X

            case 3:  // Только Y или Z 
                distribution = scaledDistribution(new float[2] { originalRoom.size.y, originalRoom.size.z});
                //Debug.Log(distribution[0].ToString() + " " + distribution[1].ToString());
                if (choice <= distribution[0]) return new Vector3Int(0, 1, 0);
                else return new Vector3Int(0, 0, 1);
            case 5: // Только X или Z
                distribution = scaledDistribution(new float[2] { originalRoom.size.x, originalRoom.size.z});
                //Debug.Log(distribution[0].ToString() + " " + distribution[1].ToString());
                if (choice <= distribution[0]) return new Vector3Int(1, 0, 0);
                else return new Vector3Int(0, 0, 1);
            case 6: // Только X или Y
                distribution = scaledDistribution(new float[2] { originalRoom.size.x, originalRoom.size.y});
                //Debug.Log(distribution[0].ToString() + " " + distribution[1].ToString());
                if (choice <= distribution[0]) return new Vector3Int(1, 0, 0);
                else return new Vector3Int(0, 1, 0); 

            case 7: // Только X или Y или Z
                distribution = scaledDistribution(new float[3] { originalRoom.size.x, originalRoom.size.y, originalRoom.size.z });
                //Debug.Log(distribution[0].ToString() + " " + distribution[1].ToString() + " " + distribution[2].ToString());
                if (choice <= distribution[0]) return new Vector3Int(1, 0, 0);
                if (choice <= distribution[0] + distribution[1]) return new Vector3Int(0, 1, 0);
                return new Vector3Int(0, 0, 1); 
            default: return Vector3Int.zero; // Ничего
        }
    }

    public float[] equalDistribution(float[] floatArray) 
    {
        int length = floatArray.Length;
        float[] result = new float[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = 1.0f / length;
        }
        return result;
    }

    public float[] scaledDistribution(float[] floatArray)
    {
        int length = floatArray.Length;
        float total_sum = 0;
        for (int i = 0; i < length; i++)
        {
            total_sum += floatArray[i];
        }

        float[] result = new float[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = floatArray[i] / total_sum;
        }
        return result;
    }

    private void PickActualRooms()
    {
        if (actualRooms == null)
        {
            actualRooms = new List<DefaultRoom>();
        } 
        else
        {
            actualRooms.Clear();
        }
        for (int i = 0; i < BSPHierarchy.Count; i++)
        {
            if (BSPHierarchy[i].myChildren == null)
            {
                actualRooms.Add(BSPHierarchy[i]);
            }
        }
    }

    private void InitializeNeighboursNaive()
    { // Наивная реализация, очень неэффективная.
        for (int i = 0; i < actualRooms.Count; i++)
        {
            for (int j = i + 1; j < actualRooms.Count; j++)
            {
                
            }
        }
    }

    private void splitAxisDecider(Rect3D originalRoom)
    {
        Vector3Int dimensions = originalRoom.B - originalRoom.A;


        // Деление по Y
        //if (dimensions.y >= )
        int wholes = dimensions.y / perfectRoomSize.y;
        int remainder = dimensions.y % perfectRoomSize.y;

        int halfsize1 = dimensions.y / 2;
        int halfsize2 = dimensions.y - halfsize1;

        int wholes1 = halfsize1 / perfectRoomSize.y;
        int wholes2 = halfsize2 / perfectRoomSize.y;

        Debug.Log("Wholes " + wholes + " remainder " + remainder);
        Debug.Log("Half1 " + halfsize1 + " Half2 " + halfsize2);
        Debug.Log("wholes1 " + wholes1 + " wholes2 " + wholes2);

        if (wholes1 + wholes2 < wholes)
        {
            Debug.Log("Mass split");

        } 
        else
        {
            Debug.Log("Typical split");
        }
        //if (perfectRoomSize.y)
    }

    private List<Rect3D> EqualSplitOneToMany(Rect3D roomToSplit, Vector3Int axis, int parts)
    {
        Debug.Log("EqualSplitOneToMany Not implemented");
        List<Rect3D> splits = new List<Rect3D>();

        return splits;
    }

    private float XYZ_Summ(Vector3 inputV)
    {
        return inputV.x + inputV.y + inputV.z;
    }







}
