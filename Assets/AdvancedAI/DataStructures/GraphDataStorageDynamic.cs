using System.Collections;
using UnityEngine;


public abstract class GraphDataStorageDynamic : GraphDataStorage
{   // Этот отличается от Graph Data Storage динамичностью - можно добавлять новые точки не пересоздавая матрицу связей.
    /// <summary>
    /// Adds point
    /// </summary>
    /// <returns></returns>
    public abstract int AddPoint();

    /// <summary>
    /// Deletes point, so it is either gone or not accessible
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public abstract bool DeletePoint(int index);

    /// <summary>
    /// Serializables in Unity work strange: when unity is running, List of Lists saves and loads correctly, but when i launch unity, something goes wrong and inner lists turns into null
    /// Which i weird, as List of Lists of CH2D_P_Indexes saves and loads correctly, but List of Lists of integers fails to do so.
    /// <br/>So this function needs to do something to check whether data is correct, so a function that recalculates graph can be launched
    /// </summary>
    /// <returns></returns>
    public abstract bool ValidityCheck();
   
}
