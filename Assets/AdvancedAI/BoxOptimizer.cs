using System.Collections.Generic;
using System;
using UnityEngine;

// Цель класса - оптимизировать пересекающиеся коробки.
// Коробки не должны иметь объемных пересечений, только плоские

// Ух, надо взять: пересечение и объединение двух коробок.
// Узнать размер объединения, лучшее пересечение
public static class BoxOptimizer
{
    public static List<NavBoxInt> DecideBoxSplit(NavBoxInt A, NavBoxInt B)
    {
        List<NavBoxInt> result = new List<NavBoxInt>();
        //Debug.Log("Not implemented");
        return result;
    }

    // Поиск объемного пересечения между членами списка
    // Если возвращает валидное значение значит еще есть коробки которые можно оптимизировать
    public static Vector2Int FindBoxIntersection(List<BoundsInt> boxes)
    {
        Vector2Int result = new Vector2Int(-1, -1); // Пересечение id_I и id_J
        if (boxes.Count < 2) return result;
        for (int i = 0; i < boxes.Count - 1; i++)
    {
            for (int j = i + 1; j < boxes.Count; j++)
            {
                //Debug.Log(BoundsMathHelper.Intersects(boxes[i], boxes[j]).ToString() + " " + i + " " + j);
                //Debug.Log(BoundsMathHelper.Intersect(boxes[i], boxes[j]).ToString() + " " + BoundsMathHelper.IsBox(BoundsMathHelper.Intersect(boxes[i], boxes[j])).ToString());
                if (BoundsMathHelper.Intersects(boxes[i], boxes[j]) && BoundsMathHelper.IsBox(BoundsMathHelper.Intersect( boxes[i], boxes[j])))
                {
                    return new Vector2Int(i, j);
                }
            }
        }
        return result;
    }

    // Перед тем как вызывать функцию надо убедиться в том что пересечение есть
    // Полное поглощение одной коробки другой коробкой не является пересечением и приведет к некорректному результату

    public static List<BoundsInt> SequentialBoxOptimization(NavBoxInt A, NavBoxInt B)
    {
        //Debug.Log("SEQUENTIAL OPTIMIZATION");
        List<BoundsInt> boxes = new List<BoundsInt>() { A.bounds, B.bounds};
        bool reason_to_continue = true;
        int safety = 0;
        while (safety < 50 && boxes.Count != 1 && reason_to_continue)
        {
            //string testtest = "";
            //foreach (BoundsInt item in boxes)  testtest += item.ToString() + "\n"; 
            //Debug.Log(testtest);

            safety += 1;
            Vector2Int next_pair = FindBoxIntersection(boxes);
            //Debug.Log(next_pair);
            if (next_pair == new Vector2Int(-1,-1)) return boxes;

            BoundsInt locA = boxes[next_pair[0]];
            BoundsInt locB = boxes[next_pair[1]];
            boxes.RemoveAt(next_pair[0] > next_pair[1] ? next_pair[0] : next_pair[1]);
            boxes.RemoveAt(next_pair[0] < next_pair[1] ? next_pair[0] : next_pair[1]);

            boxes.AddRange(OptimizeIntersections(locA, locB));
        }
        return boxes;
    }

    // Берет две Коробки на вход
    // Делит одну из коробок пополам, поитогу возвращает 3 коробки
    // Иожет вернуть 1 коробку, в случае если одна коробка полностью поглотила другую
    public static List<BoundsInt> OptimizeIntersections(BoundsInt A, BoundsInt B)
    {
        //BoundsInt union = A.ExpandToInclude(B.bounds);
        BoundsInt intersect = BoundsMathHelper.Intersect(A, B);
        Vector3Int[] bestCuttingPlane = GetBestAxis(intersect, A, B);

        // Первое значение - идентификатор оси, второе минимальные или максимальные координаты
        List<BoundsInt> boundsList = new List<BoundsInt>();

        //BoundsMathHelper.DebugDrawBox(intersect.min, intersect.size, Color.red);

        if (bestCuttingPlane[0] == Vector3Int.zero) return boundsList; // очень экзотический случай, вероятно вызван тем что А и В имеют одинаковый размер

        //Debug.Log(intersect.min.ToString() + A.min.ToString() + B.min.ToString());
        //Debug.Log(intersect.size.ToString() + A.size.ToString() + B.size.ToString());
        //Debug.Log((intersect == A | intersect == B).ToString());
        if (intersect == A | intersect == B)
        {
            boundsList.Add(BoundsMathHelper.ExpandToInclude(A, B));
            return boundsList;
        }


        if (BoundsMathHelper.CanGetCut(A, bestCuttingPlane[0], bestCuttingPlane[1]))
        {
            BoundsInt[] newBounds = BoundsMathHelper.GetCut(A, bestCuttingPlane[0], bestCuttingPlane[1]);
            boundsList.Add(newBounds[0]);
            boundsList.Add(newBounds[1]);
            boundsList.Add(B);
            return boundsList;
        } 
        if (BoundsMathHelper.CanGetCut(B, bestCuttingPlane[0], bestCuttingPlane[1]))
        {
            BoundsInt[] newBounds = BoundsMathHelper.GetCut(B, bestCuttingPlane[0], bestCuttingPlane[1]);
            boundsList.Add(newBounds[0]);
            boundsList.Add(newBounds[1]);
            boundsList.Add(A);
            return boundsList;
        }

        return boundsList;
    }
    
    // Эта функция использует каждую из сторон прямоугольника пересечения чтобы разделить А и В и узнать суммарную площадь разрезщов
    // Наименьшая площадь считается более оптимальной, т.к. большие комнаты остаются большими
    public static Vector3Int[] GetBestAxis(BoundsInt intersect, BoundsInt A, BoundsInt B)
    {
        Span<Vector3Int> span = stackalloc Vector3Int[6] {
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1)
        };

        Span<int> areas = stackalloc int[6] {
            BoundsMathHelper.GetCutAreaSafe(A, span[0], intersect.min) + BoundsMathHelper.GetCutAreaSafe(B, span[0], intersect.min),
            BoundsMathHelper.GetCutAreaSafe(A, span[1], intersect.min) + BoundsMathHelper.GetCutAreaSafe(B, span[1], intersect.min),
            BoundsMathHelper.GetCutAreaSafe(A, span[2], intersect.min) + BoundsMathHelper.GetCutAreaSafe(B, span[2], intersect.min),
            BoundsMathHelper.GetCutAreaSafe(A, span[3], intersect.max) + BoundsMathHelper.GetCutAreaSafe(B, span[3], intersect.max),
            BoundsMathHelper.GetCutAreaSafe(A, span[4], intersect.max) + BoundsMathHelper.GetCutAreaSafe(B, span[4], intersect.max),
            BoundsMathHelper.GetCutAreaSafe(A, span[5], intersect.max) + BoundsMathHelper.GetCutAreaSafe(B, span[5], intersect.max),
        };

        int min = int.MaxValue;
        int min_id = -1;
        string testetst = "";
        for (int i = 0; i < 6; i++)
        {
            testetst += areas[i] + " ";
            if (areas[i] < min && areas[i] != 0) { 
                min_id = i;
                min = areas[i];
            }
        }
        //Debug.Log(testetst);

        if (min_id == -1)
        {
            Debug.Log("GetBestAxis - что то пошло очень не так");
            return new Vector3Int[2] {Vector3Int.zero, Vector3Int.zero};
        }
        // Возвращает пару of axis-identifier и intersect min/max
        return new Vector3Int[2] { span[min_id], min_id < 3 ? intersect.min : intersect.max};
    }

    public static BoundsInt GetBestAxis(BoundsInt IntersectBox, BoundsInt UnionBox)
    { // axis? axy? axi? axises? axisesi? axisisy?

        Vector3Int axis0YZ = new Vector3Int(0, 1, 1);
        Vector3Int axisX0Z = new Vector3Int(1, 0, 1);
        Vector3Int axisXY0 = new Vector3Int(1, 1, 0);

        Vector3Int minD = UnionBox.min - IntersectBox.min;
        Vector3Int maxD = UnionBox.max - IntersectBox.max;
        // Меняются все координаты кроме одной
        BoundsInt bb_expandYZ = new BoundsInt();
        BoundsInt bb_expandXZ = new BoundsInt();
        BoundsInt bb_expandXY = new BoundsInt();
        bb_expandYZ.SetMinMax(IntersectBox.min + (minD * axis0YZ), IntersectBox.max + (maxD * axis0YZ));
        bb_expandXZ.SetMinMax(IntersectBox.min + (minD * axisX0Z), IntersectBox.max + (maxD * axisX0Z));
        bb_expandXY.SetMinMax(IntersectBox.min + (minD * axisXY0), IntersectBox.max + (maxD * axisXY0));
        // Лучшая ось - та, в которой будет наименьший объем. 
        int volumeYZ = BoundsMathHelper.CalculateVolume(bb_expandYZ);
        int volumeXZ = BoundsMathHelper.CalculateVolume(bb_expandXZ);
        int volumeXY = BoundsMathHelper.CalculateVolume(bb_expandXY);
        Debug.LogFormat("{0} {1} {2}", bb_expandYZ, bb_expandXZ, bb_expandXY);
        Debug.LogFormat("{0} {1} {2}", volumeYZ, volumeXZ, volumeXY);
        if (volumeYZ <= volumeXZ && volumeYZ <= volumeXY) return bb_expandYZ;
        if (volumeXY <= volumeXZ) return bb_expandXY;
        return bb_expandXZ; // в последнюю очередь XZ, потому что при делении будет получаться комнаты как башня из блинов
    }



}
