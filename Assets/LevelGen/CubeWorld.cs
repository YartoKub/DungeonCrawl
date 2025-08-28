using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Это класс для создания блочного мира.
// Каждый блок - это отдельная отдельная сущность. Неэффективно с точки зрения памяти. 
public class CubeWorld : MonoBehaviour
{
    public GameObject debugPrefabBox;
    public Material debugMaterial;

    public Vector3Int size;
    private int[] blockIDArray; // У каждого вида блока будет свой уникальный ID, который хранится в этом массиве.  
    public int totalSize;
    
    public int GetBlockID(int x, int y ,int z)
    {
        int valueToGet = x + y * size.x + z * size.y * size.x;
        if (valueToGet < totalSize) return blockIDArray[valueToGet];
        return -1;
    }

    public void ModifyValue(int newValue, int x, int y, int z)
    {
        int valueToChange = x + y * size.x + z * size.y * size.x;
        if (valueToChange < totalSize) blockIDArray[valueToChange] = newValue;
    }

    void Start()
    {
        blockIDArray = new int[size.x * size.y * size.z];
        totalSize = size.x * size.y * size.z;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
