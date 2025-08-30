using UnityEngine;

public class NavBoxCandidate : MonoBehaviour
{
    public int priority; // Большие значения имеют больший приоритет
    public BoxCollider myCollider; // Желательно чтобы был


    // Личные переменные, запонляются внутри родительского NavBoxEncapsulator
    public NavBoxRoom GetRoom()
    {
        return new NavBoxRoom(this.myCollider);
    }

    public void OnDrawGizmos()
    {
        Vector3 center = myCollider.center;
        Vector3 size = myCollider.size;
        Vector3 pos = this.transform.position;

        Vector3 xyz = new Vector3(-size.x, -size.y, -size.z) * 0.5f + pos + center;

        Vector3 ayz = new Vector3(size.x, -size.y, -size.z) * 0.5f + pos + center;
        Vector3 xbz = new Vector3(-size.x, size.y, -size.z) * 0.5f + pos + center;
        Vector3 xyc = new Vector3(-size.x, -size.y, size.z) * 0.5f + pos + center;

        Vector3 abc = new Vector3(size.x, size.y, size.z) * 0.5f + pos + center;

        Vector3 xbc = new Vector3(-size.x, size.y, size.z) * 0.5f + pos + center;
        Vector3 ayc = new Vector3(size.x, -size.y, size.z) * 0.5f + pos + center;
        Vector3 abz = new Vector3(size.x, size.y, -size.z) * 0.5f + pos + center;

        Debug.DrawLine(xyz, ayz);
        Debug.DrawLine(xyz, xbz);
        Debug.DrawLine(xyz, xyc);

        Debug.DrawLine(abc, xbc);
        Debug.DrawLine(abc, ayc);
        Debug.DrawLine(abc, abz);

        Debug.DrawLine(ayz, ayc);
        Debug.DrawLine(ayz, abz);

        Debug.DrawLine(xbz, xbc);
        Debug.DrawLine(xbz, abz);

        Debug.DrawLine(xyc, ayc);
        Debug.DrawLine(xyc, xbc);
    }



}
