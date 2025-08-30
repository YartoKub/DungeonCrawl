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
    private int totalSize;
    
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
    public void CreateArray() // Превращает подземелье в 
    {
        blockIDArray = new int[size.x * size.y * size.z];
        totalSize = size.x * size.y * size.z;
        InitializeArrayAir();
    }

    public void InitializeArrayAir()
    {
        for (int i = 0; i < totalSize; i++)
        {
            blockIDArray[i] = 0;
        }
    }

    public void spawnInstances()
    {
        for (int x = 0; x < this.size.x; x++)
        {
            for (int y = 0; y < this.size.y; y++)
            {
                for (int z = 0; z < this.size.z; z++)
                {
                    if (this.GetBlockID(x, y, z) == 1)
                    {
                        GameObject newObject = Instantiate(this.debugPrefabBox, new Vector3(x, y, z), new Quaternion(0,0,0,0));
                        newObject.GetComponent<MeshRenderer>().material = this.debugMaterial;
                    }
                }
            }
        }
    }

    public void BSPDungeonToBlocks(BSPDungeon dungeon)
    {
        foreach (DefaultRoom room in dungeon.actualRooms)
        {
            room.ImprintAtArray(this);
        }
    }

    public void TheTestThing(BSPDungeon dungeon)
    {
        CreateArray();

        BSPDungeonToBlocks(dungeon);

        this.spawnInstances();
    }

    void Start()
    {
    }
    void Update()
    { 
    }


}
