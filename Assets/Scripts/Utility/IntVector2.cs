using UnityEngine;

[System.Serializable]
public struct IntVector2
{

	public int x, y;

	public IntVector2(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public static IntVector2 operator +(IntVector2 a, IntVector2 b)
	{
		a.x += b.x;
		a.y += b.y;
		return a;
	}

	public static IntVector2 operator -(IntVector2 a)
    {
		return new IntVector2(-a.x, -a.y);
    }

	public static implicit operator Vector2(IntVector2 vec)
    {
		return new Vector2(vec.x, vec.y);
    }

	public static implicit operator IntVector2(Vector2 vec)
	{
		return new IntVector2((int)vec.x, (int)vec.y);
	}

	public override bool Equals(object obj) => obj is IntVector2 i && this == i;
	public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
    public static bool operator ==(IntVector2 a, IntVector2 b) => a.x == b.x && a.y == b.y;
	public static bool operator !=(IntVector2 a, IntVector2 b) => a.x != b.x || a.y != b.y;

#if UNITY_EDITOR

    public override string ToString()
    {
        return $"({x},{y})";
    }

#endif
}