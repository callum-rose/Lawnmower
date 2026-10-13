using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Terrain
{
	/// <summary>
	/// Samples a <see cref="TerrainShape"/> over a voxel grid and meshes it with surface nets: one vertex per voxel the
	/// surface passes through, one quad per grid edge it crosses. Vertices are pulled onto the surface and their normals
	/// come from the field's gradient, so rounded corners shade smoothly without needing a fine grid.
	/// </summary>
	internal static class TerrainMesher
	{
		#region API

		/// <param name="min">The grid's minimum corner. The grid's bottom is left open (it sits below the water).</param>
		public static void Build(Mesh mesh, TerrainFootprint footprint, TerrainShape shape, float3 min, float3 max,
			float voxelSize)
		{
			int3 nodes = (int3)math.ceil((max - min) / voxelSize) + 1;
			int columnCount = nodes.x * nodes.z;

			NativeArray<float> columns = new NativeArray<float>(columnCount, Allocator.TempJob);
			NativeArray<float> field = new NativeArray<float>(columnCount * nodes.y, Allocator.TempJob);
			NativeArray<int> cellVertices = new NativeArray<int>((nodes.x - 1) * (nodes.y - 1) * (nodes.z - 1),
				Allocator.TempJob);
			NativeList<float3> vertices = new NativeList<float3>(Allocator.TempJob);
			NativeList<float3> normals = new NativeList<float3>(Allocator.TempJob);
			NativeList<int> indices = new NativeList<int>(Allocator.TempJob);

			try
			{
				JobHandle columnsJob = new FootprintColumnsJob
				{
					Footprint = footprint,
					Columns = columns,
					Nodes = nodes,
					Min = min,
					VoxelSize = voxelSize,
				}.Schedule(columnCount, 64);

				JobHandle fieldJob = new FieldJob
				{
					Shape = shape,
					Columns = columns,
					Field = field,
					Nodes = nodes,
					Min = min,
					VoxelSize = voxelSize,
				}.Schedule(field.Length, 256, columnsJob);

				new SurfaceNetsJob
				{
					Footprint = footprint,
					Shape = shape,
					Field = field,
					CellVertices = cellVertices,
					Nodes = nodes,
					Min = min,
					VoxelSize = voxelSize,
					Vertices = vertices,
					Normals = normals,
					Indices = indices,
				}.Schedule(fieldJob).Complete();

				mesh.Clear();
				mesh.indexFormat = vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
				mesh.SetVertices(vertices.AsArray());
				mesh.SetNormals(normals.AsArray());
				mesh.SetIndices(indices.AsArray(), MeshTopology.Triangles, 0);
				mesh.RecalculateBounds();
			}
			finally
			{
				columns.Dispose();
				field.Dispose();
				cellVertices.Dispose();
				vertices.Dispose();
				normals.Dispose();
				indices.Dispose();
			}
		}

		#endregion

		#region Jobs

		[BurstCompile]
		private struct FootprintColumnsJob : IJobParallelFor
		{
			[ReadOnly] public TerrainFootprint Footprint;
			[WriteOnly] public NativeArray<float> Columns;
			public int3 Nodes;
			public float3 Min;
			public float VoxelSize;

			public void Execute(int index)
			{
				int x = index % Nodes.x;
				int z = index / Nodes.x;
				Columns[index] = Footprint.Distance(Min.xz + new float2(x, z) * VoxelSize);
			}
		}

		[BurstCompile]
		private struct FieldJob : IJobParallelFor
		{
			public TerrainShape Shape;
			[ReadOnly] public NativeArray<float> Columns;
			[WriteOnly] public NativeArray<float> Field;
			public int3 Nodes;
			public float3 Min;
			public float VoxelSize;

			public void Execute(int index)
			{
				int column = index % (Nodes.x * Nodes.z);
				int y = index / (Nodes.x * Nodes.z);
				int x = column % Nodes.x;
				int z = column / Nodes.x;

				float3 p = Min + new float3(x, y, z) * VoxelSize;
				Field[index] = Shape.Evaluate(p, Columns[column]);
			}
		}

		[BurstCompile]
		private struct SurfaceNetsJob : IJob
		{
			[ReadOnly] public TerrainFootprint Footprint;
			public TerrainShape Shape;
			[ReadOnly] public NativeArray<float> Field;
			public NativeArray<int> CellVertices;
			public int3 Nodes;
			public float3 Min;
			public float VoxelSize;

			public NativeList<float3> Vertices;
			public NativeList<float3> Normals;
			public NativeList<int> Indices;

			public void Execute()
			{
				int3 cells = Nodes - 1;

				for (int z = 0; z < cells.z; z++)
				{
					for (int y = 0; y < cells.y; y++)
					{
						for (int x = 0; x < cells.x; x++)
						{
							CellVertices[CellIndex(x, y, z)] = PlaceVertex(new int3(x, y, z));
						}
					}
				}

				for (int z = 0; z < Nodes.z; z++)
				{
					for (int y = 0; y < Nodes.y; y++)
					{
						for (int x = 0; x < Nodes.x; x++)
						{
							int3 node = new int3(x, y, z);
							AddQuad(node, 0);
							AddQuad(node, 1);
							AddQuad(node, 2);
						}
					}
				}
			}

			private int PlaceVertex(int3 cell)
			{
				float3 sum = 0;
				int crossings = 0;

				for (int edge = 0; edge < 12; edge++)
				{
					int axis = edge / 4;
					int3 a = cell + CornerOffset(edge % 4, axis);
					int3 b = a + AxisStep(axis);

					float da = Sample(a);
					float db = Sample(b);

					if ((da < 0) == (db < 0))
					{
						continue;
					}

					float t = da / (da - db);
					sum += math.lerp((float3)a, b, t);
					crossings++;
				}

				if (crossings == 0)
				{
					return -1;
				}

				float3 position = Min + sum / crossings * VoxelSize;
				float3 gradient = Gradient(position);

				// One Newton step onto the surface sharpens the rounded edges the averaging softens
				float3 projected = position - gradient * Evaluate(position);
				if (math.all(math.abs(projected - position) < VoxelSize))
				{
					position = projected;
					gradient = Gradient(position);
				}

				Vertices.Add(position);
				Normals.Add(gradient);
				return Vertices.Length - 1;
			}

			/// <summary>The quad for the grid edge from <paramref name="node"/> along <paramref name="axis"/>.</summary>
			private void AddQuad(int3 node, int axis)
			{
				int3 next = node + AxisStep(axis);
				if (math.any(next >= Nodes))
				{
					return;
				}

				float d0 = Sample(node);
				float d1 = Sample(next);
				if ((d0 < 0) == (d1 < 0))
				{
					return;
				}

				int3 u = AxisStep((axis + 1) % 3);
				int3 v = AxisStep((axis + 2) % 3);
				int3 cell = node - u - v;

				if (math.any(cell < 0) || math.any(cell + u + v >= Nodes - 1))
				{
					return;
				}

				int c0 = CellVertices[CellIndex(cell)];
				int c1 = CellVertices[CellIndex(cell + u)];
				int c2 = CellVertices[CellIndex(cell + u + v)];
				int c3 = CellVertices[CellIndex(cell + v)];

				if (c0 < 0 || c1 < 0 || c2 < 0 || c3 < 0)
				{
					return;
				}

				// Face away from the inside end of the edge
				float3 outward = (float3)AxisStep(axis) * (d0 < 0 ? 1 : -1);
				float3 normal = math.cross(Vertices[c1] - Vertices[c0], Vertices[c2] - Vertices[c0]);
				if (math.dot(normal, outward) < 0)
				{
					(c1, c3) = (c3, c1);
				}

				Indices.Add(c0);
				Indices.Add(c1);
				Indices.Add(c2);
				Indices.Add(c0);
				Indices.Add(c2);
				Indices.Add(c3);
			}

			private float Evaluate(float3 p)
			{
				return Shape.Evaluate(p, Footprint.Distance(p.xz));
			}

			private float3 Gradient(float3 p)
			{
				float e = VoxelSize * 0.25f;
				float3 gradient = new float3(
					Evaluate(p + new float3(e, 0, 0)) - Evaluate(p - new float3(e, 0, 0)),
					Evaluate(p + new float3(0, e, 0)) - Evaluate(p - new float3(0, e, 0)),
					Evaluate(p + new float3(0, 0, e)) - Evaluate(p - new float3(0, 0, e)));

				return math.normalizesafe(gradient, new float3(0, 1, 0));
			}

			private float Sample(int3 node)
			{
				return Field[node.x + node.z * Nodes.x + node.y * Nodes.x * Nodes.z];
			}

			private int CellIndex(int3 cell)
			{
				return CellIndex(cell.x, cell.y, cell.z);
			}

			private int CellIndex(int x, int y, int z)
			{
				int3 cells = Nodes - 1;
				return x + y * cells.x + z * cells.x * cells.y;
			}

			private static int3 AxisStep(int axis)
			{
				return axis switch
				{
					0 => new int3(1, 0, 0),
					1 => new int3(0, 1, 0),
					_ => new int3(0, 0, 1),
				};
			}

			/// <summary>The start of one of the four cell edges along <paramref name="axis"/>.</summary>
			private static int3 CornerOffset(int corner, int axis)
			{
				int3 u = AxisStep((axis + 1) % 3);
				int3 v = AxisStep((axis + 2) % 3);
				return u * (corner & 1) + v * ((corner >> 1) & 1);
			}
		}

		#endregion
	}
}
