using System.Collections.Generic;
using UnityEngine;


public class IndexPolygon
{   // Содержит только индексы, информация о позиции хранится в родительском объекте.
    public int my_index; public int my_offset;
    public List<int> vertices;
    public Bounds BBox;
    public bool isHole; 
    public IndexPolygon(List<int> vertices) { this.vertices = vertices; }
    public IndexPolygon(int my_index, int offset, int quantity, Bounds bbox, bool isHole)
    {
        this.my_index = my_index; this.my_offset = offset;
        this.BBox = bbox;
        this.vertices = new List<int>(quantity);
        this.isHole = isHole;
        for (int i = 0; i < quantity; i++)
        {
            this.vertices.Add(offset + i);
        }
    }
    public float BBoxArea()
    {
        return this.BBox.size.x * this.BBox.size.y;
    }
}

public class ComplexPolygon 
{
    // Комплексный полигон, состоящий из нескольких полигонов или дырок. 
    // Этот класс должен содержать в себе функции для саморазбиения на выпуклые треугольнички.
    // Этот класс отличается от SuperPoly2D тем что использует более оптимальные алгоритмы и несколько жесткотипизированных структур вместо векторов.
    // Предполагается что SuperPoly2D будет удален когда я завершу работу над этим классом - Ладно, удалять его не буду. Он нормально выполняет свою задачу
    // GHPolygonMerge - вроде бы хороший, нечего улучшить. Очень открыт, принимает чистые списки Vector2 точек и ничего больше.
    // Poly2DToolbox:
    // 1) Unite Holes (Сращивает полигоны оставляя шрамы) - Починено
    // 2) Earclip (В целом ничего, сейчас реализация очень наивная, есть что улучшать)
    // 3) HealScars  (Ненужная иперация которая производится из-за того что Unity Holdes работает так себе) - Починено                                                                                                                                                                                                           
    // 4) EstablishConnections (Не самая лучшая реализация)
    // 5) Iterative Voronoi (Вроде бы норм)
    // 6) ConvexPoly2D -> VolumeGrowth (нормальная) -> GrowBigPolygon (ужас из костылей) - Ну теперь чуть лучше

    public List<Vector2> vertices;
    public List<IndexPolygon> polygons;
    public int[] hierarchy;
    public int PolygonDepth;

    // 
    public ComplexPolygon(HierarchicalPoly2D poly2D)
    {
        this.UnitePolygons(poly2D);

    }

    public List<Triangle> GetTriangulation()
    {
        List<Triangle> triangles = new List<Triangle> ();
        for (int i = 0; i < this.polygons.Count; i++)
        {
            List<Triangle> t = EngulfEarclip(i);
            //Debug.Log(t.Count);
            triangles.AddRange(t);
        }
        return triangles;
    }
    public List<Vector2> GetVertices()
    {
        return new List<Vector2>(this.vertices);
    }

    // Смысл в том чтобы объединить все вершины в один список, а все полигоны корректно модифицировать. 
    // Вершины ни на одном этапе вроде не добавляются и не отпадают, должно сработать.
    // Цель - превратить супер полигон в интовый список, а затем итеративно, идя снизу вверх по иерархии, отдельно earclipp-нуть каждый из полигонов
    // По итогу должна получиться триангуляция каждого уровня полигона. Так как список вершин общий, проверка на соседство не должна быть слишком сложной
    public void UnitePolygons(HierarchicalPoly2D poly2D) 
    {
        int totalCount = 0;
        int[] offsetList = new int[poly2D.polygons.Count];
        this.polygons = new List<IndexPolygon>();
        for (int i = 0; i < poly2D.polygons.Count; i++)
        {
            totalCount += poly2D.polygons[i].vertices.Count;
            if (i == 0) offsetList[i] = 0;
            else offsetList[i] = offsetList[i - 1] + poly2D.polygons[i - 1].vertices.Count;
        }
        //string nstring = ""; for (int i = 0; i < offsetList.Length; i++) nstring += offsetList[i] + " "; Debug.Log(nstring);

        this.vertices = new List<Vector2>(totalCount);

        for (int i = 0; i < poly2D.polygons.Count; i++)
        {
            this.polygons.Add(new IndexPolygon(i, offsetList[i], poly2D.polygons[i].vertices.Count, poly2D.polygons[i].BBox, poly2D.polygons[i].isHole));
            for (int j = 0; j < poly2D.polygons[i].vertices.Count; j++)
            {
                this.vertices.Add(poly2D.polygons[i].vertices[j]);
            }
        }

        this.hierarchy = poly2D.hierarchy;
        this.PolygonDepth = poly2D.PolygonDepth;
    }



    public List<Triangle> EngulfEarclip(int thisPolyIndex)  // Объединяет бОльший полигон с его напрямую подчиненными. Полученный полигон нарезается на треугольники
    {
        List<IndexPolygon> engulfed = new List<IndexPolygon>();
        for (int i = 0; i < this.polygons.Count; i++)
            if (thisPolyIndex == hierarchy[i])
                engulfed.Add(polygons[i]);

        List<int> combinedPoly = CombinePolygons(polygons[thisPolyIndex].vertices, engulfed);

        //string nstring = ""; for (int i = 0; i < combinedPoly.Count; i++) nstring += combinedPoly[i] + " "; Debug.Log(nstring);

        List<Vector2> cpv = new List<Vector2>(combinedPoly.Count);
        for (int i = 0; i < combinedPoly.Count; i++) cpv.Add(vertices[combinedPoly[i]]);
        //nstring = ""; for (int i = 0; i < cpv.Count; i++) nstring += cpv[i] + " "; Debug.Log(nstring);

        List<Triangle> triangles = Poly2DToolbox.EarClip(cpv, polygons[thisPolyIndex].isHole);

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle t = triangles[i];
            triangles[i] = new Triangle(combinedPoly[t.a], combinedPoly[t.b], combinedPoly[t.c], t.isHole);
        }
        // DebugUnitilies Debug DebugDrawing
        /*
        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle t = triangles[i];
            DebugUtilities.DebugDrawLine(vertices[t.a], vertices[t.b], Color.purple);
            DebugUtilities.DebugDrawLine(vertices[t.b], vertices[t.c], Color.purple);
            DebugUtilities.DebugDrawLine(vertices[t.c], vertices[t.a], Color.purple);
            DebugUtilities.DebugDrawCross((vertices[t.a] + vertices[t.b] + vertices[t.c]) / 3, triangles[i].isHole ? Color.red : Color.green);
        }

        for (int i = 0; i < combinedPoly.Count - 1; i++) { DebugUtilities.DebugDrawLine(vertices[combinedPoly[i]], vertices[combinedPoly[i + 1]], Color.violet); }
        DebugUtilities.DebugDrawLine(vertices[combinedPoly[combinedPoly.Count - 1]], vertices[combinedPoly[0]], Color.violet);*/

        return triangles;
    }

    public List<int> CombinedPolygon(List<int> A, List<int> B)
    {
        (int mergeA, int mergeB) = UniteHole(A, B, vertices);
        List<int> combined = StitchHole(A, B, mergeA, mergeB);
        //string nstring = ""; for (int i = 0; i < combined.Count; i++) nstring += combined[i] + " "; Debug.Log(nstring);
        return combined;
    }

    public List<int> CombinePolygons(List<int> A, List<IndexPolygon> polygons)
    {
        if (polygons.Count == 0) return A;
        if (polygons.Count == 1) return CombinedPolygon(A, polygons[0].vertices);
        
        // Объединение множества полигонов
        int safety = 0; 
        List<int> toReturn = new List<int>(A);
        List<IndexPolygon> polygonsToMerge = new List<IndexPolygon>(polygons);
        int indexPolyToCheck = 0;
        while (polygonsToMerge.Count != 0)
        {
            safety += 1; 
            if (safety >= 15) break;
            // Найти линию между A и полигоном B
            (int mergeA, int mergeB) = UniteHole(toReturn, polygonsToMerge[indexPolyToCheck], vertices);
            // Проверить пересечение этой линии с другими полигонами Other
            int intersect = -1;
            for (int i = 0; i < polygonsToMerge.Count; i++)
            {
                if (i == indexPolyToCheck) continue;
                int real_mergeA = toReturn[mergeA]; // Это индекс внутри полигона, его нужно перевести в индекс общих вершин
                int real_mergeB = polygonsToMerge[indexPolyToCheck].vertices[mergeB];
                //Debug.Log(real_mergeA + " " + real_mergeB); 
                bool line_poly = DoesLineIntersectPolygon(real_mergeA, real_mergeB, polygonsToMerge[i], vertices);
                if (!line_poly) continue;
                intersect = i;
                break;
            }
            // Если есть пересечение, переключаюсь на полигон Other с которым было пересечение
            if (intersect != -1)
            {
                indexPolyToCheck = intersect;
            }
            // Иначе, соединить полигон А с B
            else
            {
                IndexPolygon polyToMerge = polygonsToMerge[indexPolyToCheck];
                toReturn = StitchHole(toReturn, polyToMerge.vertices, mergeA, mergeB);
                polygonsToMerge.RemoveAt(indexPolyToCheck);
                indexPolyToCheck = 0;
            }
        }
        return toReturn;
    }

    public static bool DoesLineIntersectPolygon(int A, int B, IndexPolygon ipoly, List<Vector2> v)
    {
        List<Vector2> polyA = new List<Vector2>(ipoly.vertices.Count);
        for (int i = 0; i < ipoly.vertices.Count; i++) polyA.Add(v[ipoly.vertices[i]]);

        return Poly2DToolbox.LinePolyIntersection(v[A], v[B], polyA, ipoly.BBox);
    }

    public static (int, int) UniteHole(List<int> A, IndexPolygon B, List<Vector2> v)
    {
        List<Vector2> polyA = new List<Vector2>(A.Count);
        for (int i = 0; i < A.Count; i++) polyA.Add(v[A[i]]);
        List<Vector2> polyB = new List<Vector2>(B.vertices.Count);
        for (int i = 0; i < B.vertices.Count; i++) polyB.Add(v[B.vertices[i]]);
        return Poly2DToolbox.UniteHoleOptimization(polyA, polyB, B.BBox);
    }

    public static (int,int) UniteHole(List<int> A, List<int> B, List<Vector2> v)
    {
        List<Vector2> polyA = new List<Vector2>(A.Count);
        for (int i = 0; i < A.Count; i++) polyA.Add(v[A[i]]);
        List<Vector2> polyB = new List<Vector2>(B.Count);
        for (int i = 0; i < B.Count; i++) polyB.Add(v[B[i]]);
        return Poly2DToolbox.UniteHole(polyA, polyB);
    }

    public static List<int> StitchHole(List<int> A, List<int> B, int Aindex, int Bindex)
    {   // TODO here, time to sleep;
        int[] stitched = new int[A.Count + B.Count + 2];
        //string newLoop = "";
        int newIndex;
        for (int a = 0; a < Aindex; a++)
        {
            newIndex = a;
            stitched[newIndex] = A[a]; //newLoop += "I" + newIndex + " A" + a + "\n";
        }
        stitched[Aindex] = A[Aindex];
        //newLoop += "I" + Aindex + "_AI_" + Aindex + "\n";
        for (int b = 0; b < B.Count - Bindex; b++)
        {
            newIndex = Aindex + b + 1;
            stitched[newIndex] = B[b + Bindex]; //newLoop += "I" + newIndex + " B" + (b + Bindex) + "\n";
        }

        for (int b = 0; b < Bindex; b++)
        {
            newIndex = Aindex + (B.Count - Bindex + 1) + b;
            stitched[newIndex] = B[b]; //newLoop += "I" + newIndex + " B" + b + "\n";
        }

        stitched[Aindex + B.Count + 1] = B[Bindex];
        //newLoop += "I" + (Aindex + B.Count + 1) + "_BI_" + Bindex + "\n";

        for (int a = 0; a < A.Count - Aindex; a++)
        {
            newIndex = Aindex + B.Count + 2 + a;
            stitched[newIndex] = A[a + Aindex]; //newLoop += "I" + newIndex + " A" + (a + Aindex) + "\n";
        }
        //Debug.Log(newLoop);
        return new List<int>(stitched);
    }
    public void UpwardRecursiveEarclip()
    {

    }



    

    
    /*
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
    }*/

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
