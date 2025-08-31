using UnityEngine;

public class ManualNavBoxPlacer : MonoBehaviour
{
    public Vector3Int min;
    public Vector3Int size;

    public bool KillYourself;

    private void Start()
    {
        //GraphManager // Регистрация у менеджера графов
        //GraphManager.StaticRegisterBox(min, min + size);
        if (KillYourself) Destroy(this.gameObject, 0.05f);
    }

    private void OnDrawGizmos()
    {
        BoundsMathHelper.DebugDrawBox(min, size);
    }
}
