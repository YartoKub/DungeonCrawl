using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

// Содержит в себе структуру из Chunk-ов
// Каждый CH2D_Chunk имеет форму выпуклого полигона.



public class ConvexHierarchy2D
{   
    public List<CH2D_Chunk> chunks;
    public IntMatrixGraph connections;

}


