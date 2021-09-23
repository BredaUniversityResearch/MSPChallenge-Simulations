using System;

namespace REL
{
	/* Bare-bones 2D vector implementation for use with SEL & REL*/
	public struct Vector2D : IEquatable<Vector2D>
	{
		public double x;
		public double y;

		public Vector2D(double a_x, double a_y)
		{
			x = a_x;
			y = a_y;
		}

		public static Vector2D operator -(Vector2D lhs, Vector2D rhs)
		{
			return new Vector2D(lhs.x - rhs.x, lhs.y - rhs.y);
		}

		public static Vector2D operator +(Vector2D lhs, Vector2D rhs)
		{
			return new Vector2D(lhs.x + rhs.x, lhs.y + rhs.y);
		}

		public static Vector2D operator /(Vector2D lhs, double rhs)
		{
			return new Vector2D(lhs.x / rhs, lhs.y / rhs);
		}

		public static Vector2D operator *(Vector2D lhs, double rhs)
		{
			return new Vector2D(lhs.x * rhs, lhs.y * rhs);
		}

		public bool Equals(Vector2D other)
		{
			return x == other.x && y == other.y;
		}

		public double DotProduct(Vector2D other)
		{
			return (x * other.x) + (y * other.y);
		}

		public double CrossProduct(Vector2D otherLine)
		{
			return (x * otherLine.y) - (y * otherLine.x);
		}

		public override string ToString()
		{
			return string.Format("X: {0}, Y: {1} ", x, y);
		}

		public double MagnitudeSqr()
		{
			return (x * x) + (y * y);
		}

		public double Magnitude()
		{
			return Math.Sqrt(MagnitudeSqr());
		}

		public void Normalise()
		{
			double rcpLength = 1.0f / Magnitude();
			x = x * rcpLength;
			y = y * rcpLength;
		}

		public static Vector2D Lerp(Vector2D fromPoint, Vector2D toPoint, float timeSlice)
		{
			Vector2D deltaPosition = toPoint - fromPoint;
			return fromPoint + (deltaPosition * timeSlice);
		}

		public static Vector2D Min(Vector2D position1, Vector2D position2)
		{
			return new Vector2D(Math.Min(position1.x, position2.x), Math.Min(position1.y, position2.y));
		}

		public static Vector2D Max(Vector2D position1, Vector2D position2)
		{
			return new Vector2D(Math.Max(position1.x, position2.x), Math.Max(position1.y, position2.y));
		}
	}
}
