using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

// Содержит в себе структуру из Chunk-ов
// Каждый CH2D_Chunk имеет форму выпуклого полигона.
// Каждый чанк как выпуклый полигон - слишком сложно. Тут будет проблемма с излишней динамичностью структуры и неравномерным распределением детализации.
// Также при разделении чанка на меньшие части его сложность наоборот может вырасти если его содержимое состоит из сложных полигонов
// Сначала сделаю регулярные чанки в форме клеточек или сот, потом как попрет.
// Плюсом чанковой системы является относительность точек к центру чанка. Надеюсь это поможет избежать неточности float на больших масштабах
[Serializable]
public class ConvexHierarchy2D
{   
    public List<CH2D_Chunk> regular_chunks;
    public List<CH2D_LeveledChunk> complex_chunks;
    public IntMatrixGraph connections;

    public void DrawWorld()
    {
        if (regular_chunks != null)
        for (int i = 0; i < regular_chunks.Count; i++)
        {
            regular_chunks[i].DebugDrawSelf();
        }
        if (complex_chunks != null)
        for (int i = 0; i < complex_chunks.Count; i++)
        {
            complex_chunks[i].DebugDrawSelf();
        }
    }
    public void DefaultRegeneration()
    {
        RegenerateChunks(3, 3, 5, 5);
    }
    public void RegenerateChunks(int x_count, int y_count,  int x_size, int y_size)
    {
        PurgeSelf();
        for (int x = 0; x < x_count; x++)
        {
            for (int y = 0; y < y_count; y++)
            {
                List<Vector2> points = new() {new Vector2(x_size * x, y_size * y), new Vector2(x_size * x, y_size * (y + 1)), new Vector2(x_size * (x+1), y_size * (y+1)), new Vector2(x_size * (x + 1), y_size * y)};

                regular_chunks.Add(new CH2D_Chunk(points, true));
                regular_chunks.Add(new CH2D_Chunk(points, true));

            }
        }
    }
    public void PurgeSelf()
    {
        if ( this.regular_chunks == null) this.regular_chunks = new();
        else this.regular_chunks.Clear();
        if ( this.complex_chunks == null) this.complex_chunks = new();
        else this.complex_chunks.Clear();
    }





}


