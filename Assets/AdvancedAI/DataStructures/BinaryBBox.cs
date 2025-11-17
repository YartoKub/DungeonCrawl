using System.Collections.Generic;
using UnityEngine;
using System;


public class BinaryBBox
{   // Содержит BBox и список индексов
    public Bounds BBox;
    public BinaryBBox child1;
    public BinaryBBox child2;
    public int index;
}
public class BinaryBBoxList
{   // Содержит BBox и список элементов, в некоторых случаях быстрее проверить несколько элементов чем дальше спускаться по дереву, и само дерево меньше по размеру и памяти
    Bounds BBox;
    BinaryBBox child1;
    BinaryBBox child2;
    List<int> indices;
}
public class BinaryBBoxRoot
{
    public BinaryBBox bbox;
    public int max_depth;
}

public interface I_BBoxSupporter
{
    public Bounds i_bounds { get; set; }
}

public static class BinaryBBoxToolbox
{
    private struct BBoxWrapper
    {   // Внутренняя структура. Хранит BBox и позицию в изначальном списке.
        public Bounds BBox;
        public int originalIndex;
        public BBoxWrapper(Bounds BBox, int or_index) {
            this.BBox = BBox; this.originalIndex = or_index;
        }
        public enum SortSetting { X_min, X_center, X_max, Y_min, Y_center, Y_max, XY_ordering}
        public static void Sort(List<BBoxWrapper> wrappers, SortSetting sort)
        {
            switch (sort)
            {
                case BBoxWrapper.SortSetting.X_min:
                    wrappers.Sort((a, b) => { return a.BBox.min   .x.CompareTo(b.BBox.min   .x); });
                    break;
                case BBoxWrapper.SortSetting.X_center:
                    wrappers.Sort((a, b) => { return a.BBox.center.x.CompareTo(b.BBox.center.x); });
                    break;
                case BBoxWrapper.SortSetting.X_max:
                    wrappers.Sort((a, b) => { return a.BBox.max   .x.CompareTo(b.BBox.max   .x); });
                    break;
                case BBoxWrapper.SortSetting.Y_min:
                    wrappers.Sort((a, b) => { return a.BBox.min   .y.CompareTo(b.BBox.min   .y); });
                    break;
                case BBoxWrapper.SortSetting.Y_center:
                    wrappers.Sort((a, b) => { return a.BBox.center.y.CompareTo(b.BBox.center.y); });
                    break;
                case BBoxWrapper.SortSetting.Y_max:
                    wrappers.Sort((a, b) => { return a.BBox.max   .y.CompareTo(b.BBox.max   .y); });
                    break;
                case BBoxWrapper.SortSetting.XY_ordering:
                    wrappers.Sort(
                        (a, b) => {
                        int x_com = a.BBox.center.x.CompareTo(b.BBox.center.x);
                        if (x_com != 0) return x_com;
                        return a.BBox.center.y.CompareTo(b.BBox.center.y);
                        });
                    break;
            }
        }
    }
    // ===*=== Naive Binary Volume Hierarchy ===*===
    public static BinaryBBoxRoot BuildBHVNaive(List<I_BBoxSupporter> supporters)
    {
        List<BBoxWrapper> wrapped = new List<BBoxWrapper>(supporters.Count);
        BBoxWrapper.Sort(wrapped, BBoxWrapper.SortSetting.XY_ordering);
        for (int i = 0; i < supporters.Count; i++)
            wrapped.Add(new BBoxWrapper(supporters[i].i_bounds, i));

        return ActuallyBuildNaive(wrapped);
    }
    public static BinaryBBoxRoot BuildBHVNaive(List<Bounds> bounds)
    {
        List<BBoxWrapper> wrapped = new List<BBoxWrapper>(bounds.Count);
        BBoxWrapper.Sort(wrapped, BBoxWrapper.SortSetting.XY_ordering);
        for (int i = 0; i < bounds.Count; i++)
            wrapped.Add(new BBoxWrapper(bounds[i], i));

        return ActuallyBuildNaive(wrapped);
    }
    private static BinaryBBoxRoot ActuallyBuildNaive(List<BBoxWrapper> bbox_list)
    {
        (BinaryBBox root_node, int depth) = BuildBVH(bbox_list, 0, bbox_list.Count, 0);
        BinaryBBoxRoot bbox_root = new BinaryBBoxRoot();
        bbox_root.bbox = root_node;
        bbox_root.max_depth = depth;
        return bbox_root;
    }

    private static (BinaryBBox, int) BuildBVH(List<BBoxWrapper> sorted_leafs, int beg_i, int end_i, int depth)
    {
        BinaryBBox nBBox = new BinaryBBox();
        if (end_i - beg_i == 0 | depth > 15) return (null, depth);
        if (end_i - beg_i == 1)
        {
            nBBox.BBox = sorted_leafs[beg_i].BBox;
            nBBox.index = beg_i;
            return (nBBox, depth + 1);
        }

        nBBox.BBox = GrowBounds(sorted_leafs, beg_i, end_i);

        int middle_point = beg_i + (end_i - beg_i) / 2;
        (BinaryBBox child1, int d1) = BuildBVH(sorted_leafs, beg_i, middle_point, depth + 1);
        (BinaryBBox child2, int d2) = BuildBVH(sorted_leafs, middle_point, end_i, depth + 1);
        nBBox.child1 = child1;
        nBBox.child2 = child2;

        return (nBBox, Mathf.Max(d1, d2));
    }

    // ===*=== Side Growing Binary Volume Hierarchy ===*===
    public static BinaryBBoxRoot BuildBVHSideGrowing(List<Bounds> bounds)
    {
        List<BBoxWrapper> wrapped = new List<BBoxWrapper>(bounds.Count);
        BBoxWrapper.Sort(wrapped, BBoxWrapper.SortSetting.XY_ordering);
        for (int i = 0; i < bounds.Count; i++)
            wrapped.Add(new BBoxWrapper(bounds[i], i));

        return ActuallyBuildSideGrowingBVH(wrapped);
    }
    public static BinaryBBoxRoot BuildBVHSideGrowing(List<I_BBoxSupporter> supporters)
    {
        List<BBoxWrapper> wrapped = new List<BBoxWrapper>(supporters.Count);
        BBoxWrapper.Sort(wrapped, BBoxWrapper.SortSetting.XY_ordering);
        for (int i = 0; i < supporters.Count; i++)
            wrapped.Add(new BBoxWrapper(supporters[i].i_bounds, i));

        return ActuallyBuildSideGrowingBVH(wrapped);
    }
    private static BinaryBBoxRoot ActuallyBuildSideGrowingBVH(List<BBoxWrapper> bbox_list)
    {   // Bounds тут пустые, потому что их можно рассчитать от от детей
        (BinaryBBox root_node, int depth) = SideGrowingBHV(0, new Bounds(), bbox_list); 
        BinaryBBoxRoot bbox_root = new BinaryBBoxRoot();
        bbox_root.bbox = root_node;
        bbox_root.max_depth = depth;
        root_node.BBox = root_node.child1.BBox;
        root_node.BBox.Encapsulate(root_node.child2.BBox);
        return bbox_root;
    }
    private static (BinaryBBox, int) SideGrowingBHV(int depth, Bounds my_bounds, List<BBoxWrapper> boxes)
    {
        if (boxes.Count == 0 | depth > 25) { Debug.Log("искусственная остановка по глубине"); return (null, depth); }
        BinaryBBox nBBox = new BinaryBBox();
        if (boxes.Count == 1)
        {
            nBBox.BBox = boxes[0].BBox;
            //Debug.Log(boxes[0].originalIndex +  " покинута рекурсия " + depth);
            nBBox.index = boxes[0].originalIndex;
            return (nBBox, depth + 1);
        }
        nBBox.BBox = my_bounds;
        if (boxes.Count == 2)
        {
            //Debug.Log(boxes[0].originalIndex + " " + boxes[1].originalIndex + "Покинута рекурсия через пару коробок ");
            (BinaryBBox s_child1, int sd1) = SideGrowingBHV(depth, boxes[0].BBox, boxes.GetRange(0, 1));
            (BinaryBBox s_child2, int sd2) = SideGrowingBHV(depth, boxes[1].BBox, boxes.GetRange(1, 1));
            nBBox.child1 = s_child1;
            nBBox.child2 = s_child2;
            return (nBBox, Mathf.Max(sd1, sd2));
        }
        Bounds A; Bounds B; int middle_point = 0;
        if (boxes.Count < 150 | (my_bounds.size.x * 1.9f < my_bounds.size.y))
        {
            List<BBoxWrapper> boxesY = new List<BBoxWrapper>(boxes);
            (Bounds Ax, Bounds Bx, int middle_point_x) = SplitStrategySideGrowing(boxes , BBoxWrapper.SortSetting.X_center);
            (Bounds Ay, Bounds By, int middle_point_y) = SplitStrategySideGrowing(boxesY, BBoxWrapper.SortSetting.Y_center);
            float X_overlap = OverlapArea(Ax, Bx);
            float Y_overlap = OverlapArea(Ay, By);
            //Debug.Log(X_overlap + " " + Y_overlap);
            if (Y_overlap < X_overlap) { A = Ay; B = By; middle_point = middle_point_y; boxes = boxesY; }
            else { A = Ax; B = Bx; middle_point = middle_point_x; }
        } else (A, B, middle_point) = SplitStrategySideGrowing(boxes, BBoxWrapper.SortSetting.X_center);

        
        //Debug.Log("Boxes " + boxes.Count + " mp: " + middle_point + " depth: " + depth );
        (BinaryBBox child1, int d1) = SideGrowingBHV(depth + 1, A, boxes.GetRange(0, middle_point));
        (BinaryBBox child2, int d2) = SideGrowingBHV(depth + 1, B, boxes.GetRange(middle_point, boxes.Count - middle_point ));
        nBBox.child1 = child1;
        nBBox.child2 = child2;
        return (nBBox, Mathf.Max(d1, d2));
    }
    private static (Bounds, Bounds, int split_point) SplitStrategySideGrowing(List<BBoxWrapper> boxes, BBoxWrapper.SortSetting setting)
    {   // Отдельно проверяет эффективность этой стратегии для X и для Y
        if (boxes.Count <= 2) throw new Exception("Количество коробок должно быть равно треи или больше!!!"); // Должна проводиться внешняя проверка.
        BBoxWrapper.Sort(boxes, setting);
        int safety = 0;
        Bounds A = boxes[0].BBox; Bounds B = boxes[boxes.Count - 1].BBox;
        int beg_i = 1; int end_i = boxes.Count - 1;
        int step = stepFormula_Fraction(end_i - beg_i);
        //Debug.Log("Boxes count " + boxes.Count + " step: " + step);
        while (safety < 50) { safety += 1;
            if (end_i == beg_i) break;
            //Debug.Log(beg_i + " " + end_i + " step: " + step);
            Bounds Aplus = GrowBounds(boxes, beg_i, beg_i + step);
            Bounds Bplus = GrowBounds(boxes, end_i - step, end_i);
            bool AorB = SSS_SideGrowingDemanding(A, B, Aplus, Bplus);
            if (AorB) { A.Encapsulate(Aplus); beg_i += step; } // A grown
            else      { B.Encapsulate(Bplus); end_i -= step; } // B grown
            step = stepFormula(end_i - beg_i);
        }
        return (A, B, beg_i);
    }
    private static int stepFormula(int range) { return range > 10 ? (int)Mathf.Ceil(Mathf.Pow(range, 0.66f)) : 1; }
    private static int stepFormula_Fraction(int range) { return Mathf.CeilToInt(range > 12 ? range * 0.1f : 1); }
    // Step Split Strategy
    private static bool SSS_SideGrowingDemanding(Bounds A, Bounds B, Bounds Aplus, Bounds Bplus)
    {   // true = A, false = B // This strategy prioritizes growth, and discourages big boxes from gobbling up smaller boxes
        Bounds ACombined = A; ACombined.Encapsulate(Aplus);
        Bounds BCombined = B; BCombined.Encapsulate(Bplus);
        float A_overlap_area = OverlapArea(A, Aplus) * 2;
        float B_overlap_area = OverlapArea(B, Bplus) * 2;

        float A_extra_area = ACombined.size.x * ACombined.size.y;
        float B_extra_area = BCombined.size.x * BCombined.size.y;
        //Debug.Log(A_extra_area + " " + B_extra_area + " " + A_overlap_area + " " + B_overlap_area);
        if (-A_extra_area - A_overlap_area >= -B_extra_area - B_overlap_area) return true;
        return false;
    }

    // ===*=== Support Functions ===*===
    private static (Bounds, float) GrowBounds(Bounds original, List<BBoxWrapper> to_consume)
    {   // Возвращает новую границу и приращение
        Bounds copy = original;
        for (int i = 0; i < to_consume.Count; i++)
            copy.Encapsulate(to_consume[i].BBox);
        return (copy, copy.size.x * copy.size.y - original.size.x * original.size.y);
    }
    private static Bounds GrowBounds(List<BBoxWrapper> to_consume, int beg_i, int end_i)
    {
        //Debug.Log(beg_i + " " + end_i + " " + to_consume.Count);
        if (beg_i < 0 | end_i > to_consume.Count) throw new ArgumentException("Отрицательные или запредельные значения в векторе beg_end");
        Bounds copy = to_consume[beg_i].BBox;
        for (int i = beg_i + 1; i < end_i; i++)
        {   //Debug.Log("iteration " + i);
            copy.Encapsulate(to_consume[i].BBox);
        }
        return copy;
    }


    private static float OverlapArea(Bounds a, Bounds b)
    {
        Bounds o = BoundsMathHelper.Intersect(a, b);
        //DebugUtilities.DebugDrawSquare(o.min, o.max, a.Intersects(b) ? Color.white : Color.pink, 10.0f);
        //Debug.Log(o.size.x * o.size.y * (a.Intersects(b) ? 1 : -1));
        //if (!a.Intersects(b)) return 0.0f;
        return o.size.x * o.size.y * (a.Intersects(b) ? 1 : -1);
    }
}
