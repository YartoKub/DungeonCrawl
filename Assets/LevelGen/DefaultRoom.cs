using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultRoom
{
    public Rect3D myRect;

    public BSPDungeon parent;

    public int parentID;
    public int depth;

    public int[] myChildren;

    public List<int> myNeighbours = new List<int>();

    public DefaultRoom(Vector3Int A, Vector3Int B, int parentID, int depth = -1)
    {
        myRect = new Rect3D(A, B);
        this.parentID = parentID;
        this.depth = depth;

    }

    public override string ToString()
    {
        string toReturn = "DefaultRoom of size: " + myRect.size.ToString() + " at: " + myRect.A.ToString() + " parent: " + parentID.ToString() + " depth: " + depth;
        if (myChildren != null)
        {
            toReturn += " Children: " + myChildren[0] + " " + myChildren[1];
        }
        return toReturn;
    }

    public bool Intersects(DefaultRoom otherRoom)
    {
        return this.myRect.bounds.Intersects(otherRoom.myRect.bounds);
    }

    // Возвращает форму соприкосновения двух прямоугольников.
    // Если комнаты касаются уголками  - получается точка.
    // Если комнаты касаются гранями   - получается линия.
    // Если комнаты касаются сторонами - получается квадрат.
    // Если два прямоугольника пересекаются - получится кубоид.
    // Код не проверяет наличие пересечения между кубоидами. Желательно использовать вместе с функцией Intersects
    public Vector3Int[] ConnectionShape(DefaultRoom otherRoom) 
    {
        Vector3Int myA = this.myRect.A; Vector3Int myB = this.myRect.B;
        Vector3Int otherA = otherRoom.myRect.A; Vector3Int otherB = otherRoom.myRect.B;

        Vector3Int[] preview = new Vector3Int[2];
        preview[0] = new Vector3Int(
            (int)Mathf.Clamp(myB.x, otherA.x, otherB.x),
            (int)Mathf.Clamp(myB.y, otherA.y, otherB.y),
            (int)Mathf.Clamp(myB.z, otherA.z, otherB.z)
            );
        preview[1] = new Vector3Int(
            (int)Mathf.Clamp(otherA.x, myA.x, myB.x),
            (int)Mathf.Clamp(otherA.y, myA.y, myB.y),
            (int)Mathf.Clamp(otherA.z, myA.z, myB.z)
            );

        Vector3Int[] toReturn = new Vector3Int[2];
        toReturn[0] = new Vector3Int(
            (int)Mathf.Min(preview[0].x, preview[1].x),
            (int)Mathf.Min(preview[0].y, preview[1].y),
            (int)Mathf.Min(preview[0].z, preview[1].z)
            );
        toReturn[1] = new Vector3Int(
            (int)Mathf.Max(preview[0].x, preview[1].x),
            (int)Mathf.Max(preview[0].y, preview[1].y),
            (int)Mathf.Max(preview[0].z, preview[1].z)
            );
        //Debug.Log();

        return toReturn;
    }

    public void ImprintAtArray(CubeWorld cubeWorld) // Комната должна использовать функцию ModifyValue для установки идентификаторов клетки
    {
        for (int x = 0; x < this.myRect.size.x; x++)
        {
            for (int y = 0; y < this.myRect.size.y; y++)
            {
                for (int z = 0; z < this.myRect.size.z; z++)
                {
                    if (x == 0 | x == this.myRect.size.x |
                        y == 0 | y == this.myRect.size.y |
                        z == 0 | z == this.myRect.size.z)
                    {
                        cubeWorld.ModifyValue(1, this.myRect.A.x + x, this.myRect.A.y + y, this.myRect.A.z + z);
                    }
                }
            }
        }
    }
}
