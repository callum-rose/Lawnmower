using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LevelSelect
{
	/// <summary>
	/// A smooth line through <see cref="Points"/> (Catmull-Rom), drawn as a solid band or as dashes. Level Select draws
	/// the garden path twice: a wide dirt band, then white dashes on top.
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public class PathLineGraphic : MaskableGraphic
	{
		[SerializeField] private List<Vector2> points = new List<Vector2>();
		[SerializeField, Min(0.5f)] private float thickness = 26f;
		[Tooltip("0 draws a solid line.")]
		[SerializeField, Min(0)] private float dashLength;
		[SerializeField, Min(0)] private float gapLength = 10f;
		[SerializeField, Range(1, 32)] private int subdivisions = 12;

		private readonly List<Vector2> _curve = new List<Vector2>();

		public IReadOnlyList<Vector2> Points => points;

		public void SetPoints(IEnumerable<Vector2> value)
		{
			points.Clear();
			points.AddRange(value);
			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			BuildCurve();

			if (_curve.Count < 2)
			{
				return;
			}

			if (dashLength <= 0f)
			{
				AddBand(vh, 0f, float.MaxValue);
				return;
			}

			float period = dashLength + gapLength;
			float total = Length();
			for (float start = gapLength * 0.5f; start < total; start += period)
			{
				AddBand(vh, start, Mathf.Min(start + dashLength, total));
			}
		}

		private void BuildCurve()
		{
			_curve.Clear();

			if (points.Count < 2)
			{
				_curve.AddRange(points);
				return;
			}

			for (int i = 0; i < points.Count - 1; i++)
			{
				Vector2 p0 = points[Mathf.Max(i - 1, 0)];
				Vector2 p1 = points[i];
				Vector2 p2 = points[i + 1];
				Vector2 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

				for (int s = 0; s < subdivisions; s++)
				{
					_curve.Add(CatmullRom(p0, p1, p2, p3, s / (float)subdivisions));
				}
			}

			_curve.Add(points[points.Count - 1]);
		}

		private float Length()
		{
			float length = 0f;
			for (int i = 1; i < _curve.Count; i++)
			{
				length += Vector2.Distance(_curve[i - 1], _curve[i]);
			}

			return length;
		}

		/// <summary>One continuous strip covering arc length [from, to].</summary>
		private void AddBand(VertexHelper vh, float from, float to)
		{
			float half = thickness * 0.5f;
			float travelled = 0f;
			int previousIndex = -1;
			Color32 c = color;

			for (int i = 0; i < _curve.Count - 1; i++)
			{
				Vector2 a = _curve[i];
				Vector2 b = _curve[i + 1];
				float segment = Vector2.Distance(a, b);

				if (segment <= Mathf.Epsilon || travelled + segment < from)
				{
					travelled += segment;
					continue;
				}

				if (travelled > to)
				{
					break;
				}

				float t0 = Mathf.Clamp01((from - travelled) / segment);
				float t1 = Mathf.Clamp01((to - travelled) / segment);
				Vector2 start = Vector2.Lerp(a, b, t0);
				Vector2 end = Vector2.Lerp(a, b, t1);
				Vector2 normal = Normal(i) * half;
				Vector2 nextNormal = (t1 < 1f ? Normal(i) : Normal(i + 1)) * half;

				if (previousIndex < 0)
				{
					previousIndex = vh.currentVertCount;
					vh.AddVert(start - normal, c, Vector4.zero);
					vh.AddVert(start + normal, c, Vector4.zero);
				}

				int index = vh.currentVertCount;
				vh.AddVert(end - nextNormal, c, Vector4.zero);
				vh.AddVert(end + nextNormal, c, Vector4.zero);
				vh.AddTriangle(previousIndex, previousIndex + 1, index + 1);
				vh.AddTriangle(previousIndex, index + 1, index);
				previousIndex = index;

				travelled += segment;
			}
		}

		/// <summary>The averaged (mitred) normal at curve point <paramref name="i"/>.</summary>
		private Vector2 Normal(int i)
		{
			Vector2 before = i > 0 ? (_curve[i] - _curve[i - 1]).normalized : Vector2.zero;
			Vector2 after = i < _curve.Count - 1 ? (_curve[i + 1] - _curve[i]).normalized : Vector2.zero;
			Vector2 tangent = (before + after).normalized;

			if (tangent == Vector2.zero)
			{
				tangent = after != Vector2.zero ? after : before;
			}

			Vector2 normal = new Vector2(-tangent.y, tangent.x);
			float miter = Vector2.Dot(normal, new Vector2(-after.y, after.x));
			return miter > 0.5f ? normal / miter : normal;
		}

		private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			float t2 = t * t;
			float t3 = t2 * t;
			return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
		}
	}
}
