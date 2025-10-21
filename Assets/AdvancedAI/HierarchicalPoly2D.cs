using System.Collections.Generic;
using UnityEngine;
// я запрещаю кому-либо использовать написанный мной код дл€ обучени€ нейросетей. Ёто мо€ интеллектуальна€ собственность.
// I forbid anyone to use code, written by me, to train neural networks. It is my intellectual property.


public class HierarchicalPoly2D
{
    // —упер полигон, состо€щий из нескольких полигонов или дырок. 
    // Ётот класс должен содержать в себе функции дл€ саморазбиени€ на выпуклые треугольнички.
    public List<Poly2D> polygons;
    // public List<Poly2D> holes // Ќет нужды в отдельном списке. “о, что €вл€етс€ дыркой можно определить по глубине в иерархии
    public bool compiled; // ѕеред тем как использовать этого чувака надо скомпилировать
    public int[] hierarchy;
    public int PolygonDepth;


    public HierarchicalPoly2D()
    {
        polygons = new List<Poly2D>();
        compiled = false;
    }


    public void RecalculateDepth()
    {
        if (hierarchy == null) return;
        int maxDepth = 1;
        for (int i = 0; i < hierarchy.Length; i++)
        {
            int depthCounter = 1; int safety = 0;
            int currPolyIndex = hierarchy[i];
            while (currPolyIndex != -1 && safety < 20) { safety += 1;
                depthCounter += 1;
                currPolyIndex = hierarchy[currPolyIndex];
            }
            maxDepth = Mathf.Max(maxDepth, depthCounter);
        }
        PolygonDepth = maxDepth;
    }

    public void Compile() {
        PolygonDepth = 1;
        hierarchy = new int[polygons.Count];
        for (int i = 0; i < hierarchy.Length; i++) hierarchy[i] = -1;
        if (polygons.Count < 2) { this.compiled = true; SetOrientation(); return; }

        polygons.Sort((a, b) =>
        {   // Descending sort. Largest one is almost always a parent poly
            return a.BBoxArea().CompareTo(b.BBoxArea()) * -1;
        }); 

        //for (int i = 0; i < polygons.Count; i++) Debug.Log(polygons[i].BBox.min.ToString() + " " + polygons[i].BBox.max + " " + polygons[i].BBoxArea());
        // —тавит каждому полигону родител€, поддерживает множественную вложеннсоть
        for (int i = 0; i < polygons.Count; i++) 
            for (int j = i + 1; j < polygons.Count; j++)
                if (Poly2DToolbox.DoesPolygonContainOtherBool(polygons[i], polygons[j]))
                    hierarchy[j] = i;

        //string mystrnjng = "";for (int i = 0; i < hierarchy.Length; i++) mystrnjng += hierarchy[i] + " "; Debug.Log(mystrnjng);
        SetOrientation();
        RecalculateDepth();
    }

    public void SetOrientation()
    {
        // Ќачинает с 1 “. . самый большой полигон гарантировано €вл€етс€ родителем
        for (int i = 0; i < hierarchy.Length; i++)
        {
            int current_id = i; bool isHole = false;
            while (hierarchy[current_id] != -1) // ѕутешествует от текущей ноды к другим нодам
            {
                current_id = hierarchy[current_id];
                isHole = !isHole;
            }
            polygons[i].isHole = isHole; // Hole - CW, no hole - CCW
            polygons[i].Orient(isHole);
        }
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

    public void DebugDumpHierarchy()
    {
        List<int>[] list = new List<int>[polygons.Count];
        for (int i = 0; i < list.Length; i++) list[i] = new List<int>(0);
        for (int i = 0; i < hierarchy.Length; i++)
        {
            if (hierarchy[i] == -1) continue;
            list[hierarchy[i]].Add(i);
        }

        string toDebug = "";
        for (int i = 0; i < list.Length; i++)
        {
            toDebug += "(" + i + ")";
            if (list[i].Count == 0)
            {
                toDebug += " Leaf";
            }
            else
            {
                toDebug += " [";
                for (int j = 0; j < list[i].Count; j++)
                {
                    toDebug += list[i][j] + ", ";
                }
                toDebug += "]";
            }
            toDebug += "\n";
        }
        Debug.Log(toDebug);
    }

}
