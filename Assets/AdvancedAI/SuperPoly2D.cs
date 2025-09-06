using System.Collections.Generic;
using UnityEngine;

public class SuperPoly2D
{
    // Супер полигон, состоящий из нескольких полигонов или дырок. 
    // Этот класс должен содержать в себе функции для саморазбиения на выпуклые треугольнички.
    public List<Poly2D> polygons;
    // public List<Poly2D> holes // Нет нужды в отдельном списке. То, что является дыркой можно определить по глубине в иерархии
    public bool compiled; // Перед тем как использовать этого чувака надо скомпилировать
    public int[] hierarchy;

    public SuperPoly2D()
    {
        polygons = new List<Poly2D>();
        compiled = false;
    }


    public void DebugDraw()
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            for (int p1 = 0; p1 < polygons[i].vertices.Count; p1++)
            {
                int p2 = (p1 + 1) % polygons[i].vertices.Count;
                Color loccolor = polygons[i].isHole ? Color.red : Color.green;

                DebugUtilities.DebugDrawLine(polygons[i].vertices[p1], polygons[i].vertices[p2], loccolor);
            }
        }
    }

    public void Compile() {
        hierarchy = new int[polygons.Count];
        for (int i = 0; i < hierarchy.Length; i++) hierarchy[i] = -1;
        if (polygons.Count < 2) { this.compiled = true; SetOrientation(); return; }

        polygons.Sort((a, b) =>
        {   // Descending sort. Largest one is almost always a parent poly
            return a.BBoxArea().CompareTo(b.BBoxArea()) * -1;
        }); 

        //for (int i = 0; i < polygons.Count; i++) Debug.Log(polygons[i].BBox.min.ToString() + " " + polygons[i].BBox.max + " " + polygons[i].BBoxArea());
        // Ставит каждому полигону родителя, поддерживает множественную вложеннсоть
        for (int i = 0; i < polygons.Count; i++) 
            for (int j = i + 1; j < polygons.Count; j++)
                if (Poly2DToolbox.DoesPolygonContainOtherBool(polygons[i], polygons[j]))
                    hierarchy[j] = i;

        //string mystrnjng = "";for (int i = 0; i < hierarchy.Length; i++) mystrnjng += hierarchy[i] + " "; Debug.Log(mystrnjng);
        SetOrientation();
    }

    public void SetOrientation()
    {
        // Начинает с 1 Т.К. самый большой полигон гарантировано является родителем
        for (int i = 0; i < hierarchy.Length; i++)
        {
            int current_id = i; bool isHole = false;
            while (hierarchy[current_id] != -1) // Путешествует от текущей ноды к другим нодам
            {
                current_id = hierarchy[current_id];
                isHole = !isHole;
            }
            polygons[i].isHole = isHole; // Hole - CW, no hole - CCW
            polygons[i].Orient(isHole);
        }
    }

}
