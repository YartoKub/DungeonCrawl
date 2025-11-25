using UnityEditor;
using UnityEngine;
using System;

public struct CH2D_Edge
{
    public CH2D_P_Index A; public CH2D_P_Index B;
    public CH2D_Edge(CH2D_P_Index A, CH2D_P_Index B) { this.A = A; this.B = B; }
}
// Тут кода дофига, но на самом деле тут ничего умного нет. 
// Это просто обертка для ushort, с поддержкой операторов вроде +-, а также сравнений
// Тоесть CH2D_Index можно использовать как любое другое число в большинстве случаев
[Serializable]
public struct CH2D_P_Index
{
    [SerializeField] public UInt16 i;
    public CH2D_P_Index(UInt16 i) { this.i = i; }
    public CH2D_P_Index(int i) { if (i < UInt16.MinValue && i >= UInt16.MaxValue) throw new ArgumentOutOfRangeException(); else this.i = (UInt16)i; }
    public override string ToString() => i.ToString();
    // Преобразования
    public static implicit operator int(CH2D_P_Index id) => id.i;

    // Сравнения
    public bool Equals(CH2D_P_Index other) { return i == other.i; }
    public override bool Equals(object obj) { return obj is CH2D_P_Index other && Equals(other); }
    public override int GetHashCode() { return i.GetHashCode(); }
    public int CompareTo(CH2D_P_Index other) { return i.CompareTo(other.i); }
    // ОРператоры
    public static bool operator ==(CH2D_P_Index left, CH2D_P_Index right) => left.i.Equals(right.i);
    public static bool operator !=(CH2D_P_Index left, CH2D_P_Index right) => !left.i.Equals(right.i);
    public static bool operator <(CH2D_P_Index left, CH2D_P_Index right) => left.i.CompareTo(right.i) < 0;
    public static bool operator <=(CH2D_P_Index left, CH2D_P_Index right) => left.i.CompareTo(right.i) <= 0;
    public static bool operator >(CH2D_P_Index left, CH2D_P_Index right) => left.i.CompareTo(right.i) > 0;
    public static bool operator >=(CH2D_P_Index left, CH2D_P_Index right) => left.i.CompareTo(right.i) >= 0;
    // Математические операторы
    public static CH2D_P_Index operator +(CH2D_P_Index left, CH2D_P_Index right) { return new CH2D_P_Index(checked((ushort)(left.i + right.i))); }
    public static CH2D_P_Index operator -(CH2D_P_Index left, CH2D_P_Index right) { return new CH2D_P_Index(checked((ushort)(left.i - right.i))); }
    public static CH2D_P_Index operator *(CH2D_P_Index left, CH2D_P_Index right) { return new CH2D_P_Index(checked((ushort)(left.i * right.i))); }
    public static CH2D_P_Index operator /(CH2D_P_Index left, CH2D_P_Index right)
    {
        if (right.i == 0) throw new DivideByZeroException();
        return new CH2D_P_Index((ushort)(left.i / right.i));
    }
    public static CH2D_P_Index operator %(CH2D_P_Index left, CH2D_P_Index right)
    {
        if (right.i == 0) throw new DivideByZeroException();
        return new CH2D_P_Index((ushort)(left.i % right.i));
    }
    public static CH2D_P_Index operator +(CH2D_P_Index left, int right) { return new CH2D_P_Index(checked((ushort)(left.i + right))); }
    public static CH2D_P_Index operator -(CH2D_P_Index left, int right) { return new CH2D_P_Index(checked((ushort)(left.i - right))); }
}