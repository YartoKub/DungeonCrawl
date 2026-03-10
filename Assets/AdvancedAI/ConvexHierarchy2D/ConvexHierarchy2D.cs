using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

// Содержит в себе структуру из Chunk-ов
// Каждый CH2D_Chunk имеет форму выпуклого полигона.
// Каждый чанк как выпуклый полигон - слишком сложно. Тут будет проблемма с излишней динамичностью структуры и неравномерным распределением детализации.
// Сначала сделаю регулярные чанки в форме клеточек или сот, потом как попрет.
// Плюсом чанковой системы является относительность точек к центру чанка. Надеюсь это поможет избежать неточности float на больших масштабах

public class ConvexHierarchy2D
{   
    public List<CH2D_Chunk> complex_chunks;
    public IntMatrixGraph connections;

    //public 





}


