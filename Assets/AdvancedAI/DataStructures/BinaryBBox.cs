using System.Collections.Generic;
using UnityEngine;


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
    // Листики должны быть отсортированы по X Y!
    // Возвращает максимальную глубину дерева

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
            //Debug.Log(beg_i + " " + end_i +  " покинута рекурсия");
            nBBox.index = beg_i;
            return (nBBox, depth + 1);
        }

        nBBox.BBox = MergeBounds(sorted_leafs, beg_i, end_i);

        int middle_point = beg_i + (end_i - beg_i) / 2;
        Debug.Log(beg_i + " " + end_i + " " + middle_point);

        (BinaryBBox child1, int d1) = BuildBVH(sorted_leafs, beg_i, middle_point, depth + 1);
        (BinaryBBox child2, int d2) = BuildBVH(sorted_leafs, middle_point, end_i, depth + 1);
        nBBox.child1 = child1;
        nBBox.child2 = child2;

        return (nBBox, Mathf.Max(d1, d2));
    }
    private static Bounds MergeBounds(List<BBoxWrapper> sorted_leafs, int beg_i, int end_i)
    {
        Bounds nBBox = sorted_leafs[beg_i].BBox;
        for (int i = beg_i; i < end_i; i++)
            nBBox.Encapsulate(sorted_leafs[i].BBox);
        
        return nBBox;
    }

    private static float OverlapArea(Bounds a, Bounds b)
    {
        Bounds o = BoundsMathHelper.Intersect(a, b);
        return o.size.x * o.size.y;
    }
}
