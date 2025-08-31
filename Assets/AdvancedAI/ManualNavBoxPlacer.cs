using UnityEngine;

public class ManualNavBoxPlacer : MonoBehaviour
{
    public Vector3Int min;
    public Vector3Int size;

    public bool KillYourself;
    public bool ShowYourself;

    private void Start()
    {
        //GraphManager // Регистрация у менеджера графов
        //GraphManager.StaticRegisterBox(min, min + size);
        if (KillYourself) Destroy(this.gameObject, 0.05f);
    }

    private void OnDrawGizmos()
    {
        if (ShowYourself) BoundsMathHelper.DebugDrawBox(min, size);
    }
}
