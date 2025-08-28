using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomInitializer : MonoBehaviour
{
    public MeshFilter objectToRoom;
    public GameObject debugPrefabBox;
    public Material debugMaterial;
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("Hello ");

        DefaultRoom myRect1 = new DefaultRoom(new Vector3Int(0,0,0), new Vector3Int(3, 3, 3), -1);
        DefaultRoom myRect2 = new DefaultRoom(new Vector3Int(1,2,1), new Vector3Int(4, 5, 4), -1);

        //Debug.Log(myRect1.bounds.Intersects(myRect2.bounds));

        Vector3Int[] intersectingShape = myRect1.ConnectionShape(myRect2);
        Debug.Log("" + intersectingShape[0] + " " + intersectingShape[1]);

        DebugPreviewInstantiate(myRect1.myRect);
        DebugPreviewInstantiate(myRect2.myRect);
        DebugPreviewInstantiate(new Rect3D(intersectingShape[0], intersectingShape[1]));

        //objectToRoom.mesh = GeometryGeneration.AxisAlignedBox( myRect);
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    private void DebugPreviewInstantiate(Rect3D roomToCreate)
    {
        GameObject newObject = Instantiate(debugPrefabBox, Vector3.zero + roomToCreate.bounds.min * 0.1f, new Quaternion(0, 0, 0, 0));
        newObject.GetComponent<MeshFilter>().mesh = GeometryGeneration.AxisAlignedBox(roomToCreate);
        newObject.GetComponent<MeshRenderer>().material = debugMaterial;
    }

    public void BlockWorldCreate()
    {

    }
}
