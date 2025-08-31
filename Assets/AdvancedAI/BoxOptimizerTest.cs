using System.Collections.Generic;
using UnityEngine;

public class BoxOptimizerTest : MonoBehaviour
{
    public ManualNavBoxPlacer A;
    public ManualNavBoxPlacer B;

    List<NavBoxInt> startBoxes = new List<NavBoxInt>();
    public bool DrawStarterBoxes;

    List<NavBoxInt> optimizedBoxes = new List<NavBoxInt>();

    private void Start()
    {
        
    }

    private void Update()
    {
        startBoxes.Clear();
        startBoxes.Add(new NavBoxInt(A.min, A.min + A.size));
        startBoxes.Add(new NavBoxInt(B.min, B.min + B.size));
        //if (DrawStarterBoxes) foreach (NavBoxInt item in startBoxes) BoundsMathHelper.DebugDrawBox(item.A, item.size);
        List<BoundsInt> newbounds =  BoxOptimizer.OptimizeIntersections(startBoxes[0], startBoxes[1]);

        Debug.Log(newbounds.Count);
        if (DrawStarterBoxes) foreach (BoundsInt item in newbounds) BoundsMathHelper.DebugDrawBox(item.min, item.size, Color.purple);
    }
}
