using UnityEngine;
// Примитивный тип. Создается когда NavBoxEncapsulator находит валидный NavBoxCandidate
public struct NavBoxRoom
{
    public Bounds myBounds;
    public  int myId;

    public NavBoxRoom(BoxCollider collider)
    {
        myBounds = collider.bounds;
        Debug.Log(myBounds.min);
        Debug.Log(myBounds.max);
        this.myId = -1;
    }

    public override string ToString()
    {
        return "ID: " + ((myId / 100 == 0) ? "" : "_") + ((myId / 10 == 0) ? "" : "_") + myId + " pos: " + this.myBounds.min.ToString() + "size: " + this.myBounds.size.ToString();
    }

    public Vector3[] ConnectionShape(NavBoxRoom otherRoom)
    {
        Vector3 myA = this.myBounds.min; Vector3 myB = this.myBounds.max;
        Vector3 otherA = otherRoom.myBounds.min; Vector3 otherB = otherRoom.myBounds.max;

        Vector3[] preview = new Vector3[2];
        preview[0] = new Vector3(
            Mathf.Clamp(myB.x, otherA.x, otherB.x),
            Mathf.Clamp(myB.y, otherA.y, otherB.y),
            Mathf.Clamp(myB.z, otherA.z, otherB.z)
            );
        preview[1] = new Vector3(
            Mathf.Clamp(otherA.x, myA.x, myB.x),
            Mathf.Clamp(otherA.y, myA.y, myB.y),
            Mathf.Clamp(otherA.z, myA.z, myB.z)
            );

        Vector3[] toReturn = new Vector3[2];
        toReturn[0] = new Vector3(
            Mathf.Min(preview[0].x, preview[1].x),
            Mathf.Min(preview[0].y, preview[1].y),
            Mathf.Min(preview[0].z, preview[1].z)
            );
        toReturn[1] = new Vector3(
            Mathf.Max(preview[0].x, preview[1].x),
            Mathf.Max(preview[0].y, preview[1].y),
            Mathf.Max(preview[0].z, preview[1].z)
            );
        return toReturn;
    }
        
    public float CenterDistance(NavBoxRoom otherRoom)
    {
        return (this.myBounds.center - otherRoom.myBounds.center).magnitude;
    }
}
