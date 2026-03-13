using System.Collections.Generic;
using UnityEngine;

public class ConvexStructure
{
    // Содержит в себе множество выпуклых объемов, а также граф связей между ними
    public List<ConvexVolume> volumes;
    public Bounds BBox;

}
public class ConvexVolume
{
    // Этот класс содержит в себе ровно один выпуклый объем, этот объем ограничен выпуклыми полигонами
    // Выпуклые полигоны могут иметь сложную структуру, например, в них могут быть дыры. 
    // Сам объем можно определить используя плоскости связанных полигонов
    // Поддерживает связи между полигонами
    public List<ConvexPoly3D> polygons;
    public bool isWall;

}
public class ConvexPolystruct3D
{
    // Содержит структуру из выпуклых полигонов, а также поддерживает между ними связи
    public Plane plane;
    public List<ConvexPoly3D> vertices;
    public Bounds BBox;
}

public class ConvexPoly3D
{
    // Выпуклый полигон. Обязательно должен быть выпуклым, иначе хрень будет
    // Может быть дыркой для связи между объектами
    public Plane plane;
    public List<Vector3> vertices;
    public bool isHole;

}