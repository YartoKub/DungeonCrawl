using UnityEngine;

public struct NavBoxInt
{
    public BoundsInt bounds;
    public int myId;
    public int myGraphID; // принадлежность к первому вложенному графу

    //public int tilt;
    //public int NavBoxType;

    public Vector3Int A { get { return bounds.min; } set { this.bounds.min = value; } }
    public Vector3Int B { get { return bounds.max; } set { this.bounds.max = value; } }
    public Vector3Int size { get { return bounds.size; }}
    // float-bounds может понадобиться для рейкастов, ведь только он умеет их делать
    public Bounds boundsFloat { get { Bounds tmp_bounds = new Bounds(); tmp_bounds.SetMinMax(bounds.min, bounds.max); return tmp_bounds; }}
    
    public NavBoxInt(Vector3Int A, Vector3Int B)
    {
        bounds = new BoundsInt();
        bounds.SetMinMax(A, B);
        myId = -1;
        myGraphID = -1;
    }

    public NavBoxInt CreateFromSize(Vector3Int min, Vector3Int size)
    {
        myId = -1;
        myGraphID = -1;
        NavBoxInt toReturn = new NavBoxInt();
        toReturn.bounds.SetMinMax(min, min + size);
        return toReturn;
    }

    public NavBoxInt(Vector3Int A, Vector3Int B, int ID, int GID)
    {
        bounds = new BoundsInt();
        bounds.SetMinMax(A, B);
        myId = ID;
        myGraphID = GID;
    }


    // Все эти функции реализованы в BoundsMathHelper чтобы не повторяться, т.к. коробок я уже много наделал и не хочу копироват ькод
    public void DebugDrawBox() { BoundsMathHelper.DebugDrawBox(this.A, this.size); }
    public bool DoesContainPoint(Vector3 point) { return BoundsMathHelper.DoesContainPoint(this.bounds, point); }
    public BoundsInt ExpandToInclude(BoundsInt otherRoom) { return BoundsMathHelper.ExpandToInclude(this.bounds, otherRoom); }
    public BoundsInt Intersect(BoundsInt otherRoom) { return BoundsMathHelper.Intersect(this.bounds, otherRoom); }
    public bool DoesIntersect(BoundsInt otherRoom) { return BoundsMathHelper.Intersects(this.bounds, otherRoom);  }
    public Vector3Int[] Get_8_Corners() { return BoundsMathHelper.Get_8_Corners(this.bounds);}
    public int CalculateSurfaceArea() { return BoundsMathHelper.CalculateSurfaceArea(this.bounds); }
    public int CalculateVolume() { return BoundsMathHelper.CalculateVolume(this.bounds); }
    public int Voluminosity()  { return BoundsMathHelper.Voluminosity(this.bounds); }
    public bool IsDot() { return BoundsMathHelper.IsDot(this.bounds); }
    public bool IsLine() { return BoundsMathHelper.IsLine(this.bounds); }
    public bool IsSquare() { return BoundsMathHelper.IsSquare(this.bounds); }
    public bool IsBox(){ return BoundsMathHelper.IsBox(this.bounds); }

    public override string ToString()
    {
        return "GID: " + myGraphID + "ID: " + ((myId / 100 == 0) ? "" : "_") + ((myId / 10 == 0) ? "" : "_") + myId + " pos: " + this.bounds.min.ToString() + "size: " + this.bounds.size.ToString();
    }

}
