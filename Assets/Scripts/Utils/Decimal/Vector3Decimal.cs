using System;
using UnityEngine;

public struct Vector3Decimal
{
    public decimal x;
    public decimal y;
    public decimal z;

    public Vector3Decimal(decimal x, decimal y, decimal z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    #region Constants

    public static Vector3Decimal zero
    {
        get { return new Vector3Decimal(0, 0, 0); }
    }

    public static Vector3Decimal one
    {
        get { return new Vector3Decimal(1, 1, 1); }
    }

    #endregion

    #region Operators

    public static Vector3Decimal operator-(Vector3Decimal d)
    {
        return d * -1;
    }

    public static Vector3Decimal operator+(Vector3Decimal d)
    {
        return d;
    }

    public static Vector3Decimal operator+(Vector3Decimal lhs, Vector3Decimal rhs)
    {
        return new Vector3Decimal(
            lhs.x + rhs.x,
            lhs.y + rhs.y,
            lhs.z + rhs.z
        );
    }

    public static Vector3Decimal operator-(Vector3Decimal lhs, Vector3Decimal rhs)
    {
        return new Vector3Decimal(
            lhs.x - rhs.x,
            lhs.y - rhs.y,
            lhs.z - rhs.z
        );
    }

    public static Vector3Decimal operator*(Vector3Decimal lhs, decimal rhs)
    {
        return new Vector3Decimal(
            lhs.x * rhs,
            lhs.y * rhs,
            lhs.z * rhs
        );
    }

    public static Vector3Decimal operator*(decimal lhs, Vector3Decimal rhs)
    {
        return rhs * lhs;
    }

    public static Vector3Decimal operator/(Vector3Decimal lhs, decimal rhs)
    {
        return new Vector3Decimal(
            lhs.x / rhs,
            lhs.y / rhs,
            lhs.z / rhs
        );
    }

    public static bool operator==(Vector3Decimal lhs, Vector3Decimal rhs)
    {
        return Vector3Decimal.Equals(lhs, rhs);
    }

    public static bool operator!=(Vector3Decimal lhs, Vector3Decimal rhs)
    {
        return !Vector3Decimal.Equals(lhs, rhs);
    }

    #endregion

    #region Conversions

    public static implicit operator Vector3(Vector3Decimal d)
    {
        return new Vector3((float)d.x, (float)d.y, (float)d.z);
    }

    public static explicit operator Vector3Decimal(Vector3 f)
    {
        return new Vector3Decimal((decimal)f.x, (decimal)f.y, (decimal)f.z);
    }

    public static explicit operator Vector3Int(Vector3Decimal d)
    {
        return new Vector3Int((int)d.x, (int)d.y, (int)d.z);
    }

    public static implicit operator Vector3Decimal(Vector3Int i)
    {
        return new Vector3Decimal(i.x, i.y, i.z);
    }

    #endregion

    #region Public functions

    public override bool Equals(object obj)
    {
        if (obj is Vector3Decimal)
        {
            return Equals((Vector3Decimal)obj);
        }
        return false;
    }

    public bool Equals(Vector3Decimal other)
    {
        return Vector3Decimal.Equals(this, other);
    }

    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + this.x.GetHashCode();
        hash = (hash * 7) + this.y.GetHashCode();
        hash = (hash * 7) + this.z.GetHashCode();
        return hash;
    }

    public override string ToString()
    {
        return "Vector3Decimal(" + this.x.ToString() + ", " + this.y.ToString() + ", " + this.z.ToString() + ")";
    }

    public string ToString(string format)
    {
        return "Vector3Decimal(" + this.x.ToString(format) + ", " + this.y.ToString(format) + ", " + this.z.ToString(format) + ")";
    }

    #endregion

    #region Static functions

    public static bool Equals(Vector3Decimal a, Vector3Decimal b)
    {
        return a.x.Equals(b.x) && a.y.Equals(b.y) && a.z.Equals(b.z);
    }

    public static Vector3Decimal Ceil(Vector3Decimal d)
    {
        return new Vector3Decimal(
            decimal.Ceiling(d.x),
            decimal.Ceiling(d.y),
            decimal.Ceiling(d.z)
        );
    }

    public static Vector3Decimal Floor(Vector3Decimal d)
    {
        return new Vector3Decimal(
            decimal.Floor(d.x),
            decimal.Floor(d.y),
            decimal.Floor(d.z)
        );
    }

    public static Vector3Decimal Round(Vector3Decimal d, Int32 decimals, MidpointRounding mode)
    {
        return new Vector3Decimal(
            decimal.Round(d.x, decimals, mode),
            decimal.Round(d.y, decimals, mode),
            decimal.Round(d.z, decimals, mode)
        );
    }

    public static Vector3Decimal Round(Vector3Decimal d, Int32 decimals)
    {
        return new Vector3Decimal(
            decimal.Round(d.x, decimals),
            decimal.Round(d.y, decimals),
            decimal.Round(d.z, decimals)
        );
    }

    public static Vector3Decimal Round(Vector3Decimal d)
    {
        return new Vector3Decimal(
            decimal.Round(d.x),
            decimal.Round(d.y),
            decimal.Round(d.z)
        );
    }

    public static Vector3Decimal Round(Vector3Decimal d, MidpointRounding mode)
    {
        return new Vector3Decimal(
            decimal.Round(d.x, mode),
            decimal.Round(d.y, mode),
            decimal.Round(d.z, mode)
        );
    }
    
    #endregion
}
