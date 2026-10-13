using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Tiles
{
	/// <summary>
	/// Draws the tiles' meshes with GPU instancing: one instanced draw per mesh/material pair rather than one draw per
	/// renderer. Registered MeshRenderers are switched off and their meshes are drawn from their current transforms
	/// every frame, so tile animations, grass height changes (containers toggled active) and dirt rotations still show.
	/// Renderers that can't be instanced (no instancing on the material, a property block, ...) are left drawing as normal.
	/// </summary>
	internal class InstancedTileRenderer
	{
		// Instanced draws are capped by the size of the per-instance constant buffer
		private const int MaxInstancesPerDraw = 1023;

		private readonly Dictionary<BatchKey, Batch> _batches = new Dictionary<BatchKey, Batch>();
		private readonly List<Batch> _batchList = new List<Batch>();
		private readonly Dictionary<GameObject, List<Instance>> _instancesByTile = new Dictionary<GameObject, List<Instance>>();
		private readonly List<MeshRenderer> _rendererBuffer = new List<MeshRenderer>();

		#region API

		public void Register(GameObject tileObject)
		{
			if (!SystemInfo.supportsInstancing || _instancesByTile.ContainsKey(tileObject))
			{
				return;
			}

			List<Instance> tileInstances = new List<Instance>();

			// Include inactive children: the grass heights not currently shown live in inactive containers
			tileObject.GetComponentsInChildren(true, _rendererBuffer);
			foreach (MeshRenderer meshRenderer in _rendererBuffer)
			{
				if (!CanInstance(meshRenderer, out Mesh mesh))
				{
					continue;
				}

				Material[] materials = meshRenderer.sharedMaterials;
				for (int subMesh = 0; subMesh < materials.Length; subMesh++)
				{
					Batch batch = GetOrCreateBatch(meshRenderer, mesh, subMesh, materials[subMesh]);
					Instance instance = new Instance(meshRenderer, batch);
					batch.Add(instance);
					tileInstances.Add(instance);
				}

				meshRenderer.enabled = false;
			}

			_rendererBuffer.Clear();
			_instancesByTile.Add(tileObject, tileInstances);
		}

		public void Unregister(GameObject tileObject)
		{
			if (!_instancesByTile.Remove(tileObject, out List<Instance> tileInstances))
			{
				return;
			}

			foreach (Instance instance in tileInstances)
			{
				instance.Batch.Remove(instance);

				if (instance.Renderer)
				{
					instance.Renderer.enabled = true;
				}
			}
		}

		public void Draw()
		{
			Camera sortCamera = Camera.main;
			Vector3 cameraPosition = sortCamera ? sortCamera.transform.position : Vector3.zero;

			foreach (Batch batch in _batchList)
			{
				batch.Draw(sortCamera != null, cameraPosition);
			}
		}

		#endregion

		#region Methods

		private static bool CanInstance(MeshRenderer meshRenderer, out Mesh mesh)
		{
			mesh = null;

			if (!meshRenderer.enabled || meshRenderer.HasPropertyBlock() || meshRenderer.isPartOfStaticBatch)
			{
				return false;
			}

			if (!meshRenderer.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
			{
				return false;
			}

			mesh = meshFilter.sharedMesh;
			Material[] materials = meshRenderer.sharedMaterials;

			// Extra materials redraw the last sub-mesh as another pass; not worth supporting here
			if (materials.Length == 0 || materials.Length > mesh.subMeshCount)
			{
				return false;
			}

			foreach (Material material in materials)
			{
				if (material == null || !material.enableInstancing)
				{
					return false;
				}
			}

			return true;
		}

		private Batch GetOrCreateBatch(MeshRenderer meshRenderer, Mesh mesh, int subMesh, Material material)
		{
			BatchKey key = new BatchKey(mesh, subMesh, material, meshRenderer.gameObject.layer,
				meshRenderer.shadowCastingMode, meshRenderer.receiveShadows, meshRenderer.renderingLayerMask);

			if (_batches.TryGetValue(key, out Batch batch))
			{
				return batch;
			}

			RenderParams renderParams = new RenderParams(material)
			{
				layer = key.Layer,
				shadowCastingMode = key.ShadowCastingMode,
				receiveShadows = key.ReceiveShadows,
				renderingLayerMask = key.RenderingLayerMask,
				// The scenes have no light probes, so this matches the renderers: shaders get the ambient probe
				lightProbeUsage = LightProbeUsage.Off,
			};

			batch = new Batch(mesh, subMesh, renderParams, material.renderQueue > (int)RenderQueue.GeometryLast);
			_batches.Add(key, batch);
			_batchList.Add(batch);

			return batch;
		}

		#endregion

		private readonly struct BatchKey : IEquatable<BatchKey>
		{
			public readonly Mesh Mesh;
			public readonly int SubMesh;
			public readonly Material Material;
			public readonly int Layer;
			public readonly ShadowCastingMode ShadowCastingMode;
			public readonly bool ReceiveShadows;
			public readonly uint RenderingLayerMask;

			public BatchKey(Mesh mesh, int subMesh, Material material, int layer, ShadowCastingMode shadowCastingMode,
				bool receiveShadows, uint renderingLayerMask)
			{
				Mesh = mesh;
				SubMesh = subMesh;
				Material = material;
				Layer = layer;
				ShadowCastingMode = shadowCastingMode;
				ReceiveShadows = receiveShadows;
				RenderingLayerMask = renderingLayerMask;
			}

			public bool Equals(BatchKey other)
			{
				return Mesh == other.Mesh && SubMesh == other.SubMesh && Material == other.Material &&
				       Layer == other.Layer && ShadowCastingMode == other.ShadowCastingMode &&
				       ReceiveShadows == other.ReceiveShadows && RenderingLayerMask == other.RenderingLayerMask;
			}

			public override bool Equals(object obj)
			{
				return obj is BatchKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Mesh, SubMesh, Material, Layer, (int)ShadowCastingMode, ReceiveShadows,
					RenderingLayerMask);
			}
		}

		private class Instance
		{
			public readonly MeshRenderer Renderer;
			public readonly GameObject GameObject;
			public readonly Transform Transform;
			public readonly Batch Batch;

			public Instance(MeshRenderer renderer, Batch batch)
			{
				Renderer = renderer;
				GameObject = renderer.gameObject;
				Transform = renderer.transform;
				Batch = batch;
			}
		}

		private class Batch
		{
			private readonly Mesh _mesh;
			private readonly int _subMesh;
			private readonly Bounds _meshBounds;
			private readonly bool _sortBackToFront;
			private readonly List<Instance> _instances = new List<Instance>();

			private RenderParams _renderParams;
			private Matrix4x4[] _matrices = Array.Empty<Matrix4x4>();
			private float[] _sortKeys = Array.Empty<float>();

			public Batch(Mesh mesh, int subMesh, RenderParams renderParams, bool sortBackToFront)
			{
				_mesh = mesh;
				_subMesh = subMesh;
				_meshBounds = mesh.GetSubMesh(subMesh).bounds;
				_renderParams = renderParams;
				_sortBackToFront = sortBackToFront;
			}

			public void Add(Instance instance)
			{
				_instances.Add(instance);

				if (_matrices.Length < _instances.Count)
				{
					int capacity = Mathf.Max(_instances.Count, _matrices.Length * 2);
					Array.Resize(ref _matrices, capacity);
					Array.Resize(ref _sortKeys, capacity);
				}
			}

			public void Remove(Instance instance)
			{
				_instances.Remove(instance);
			}

			public void Draw(bool canSort, Vector3 cameraPosition)
			{
				int count = 0;
				Bounds worldBounds = default;

				foreach (Instance instance in _instances)
				{
					// The tile may have been destroyed without being unregistered (e.g. on scene unload)
					if (instance.GameObject == null || !instance.GameObject.activeInHierarchy)
					{
						continue;
					}

					Matrix4x4 matrix = instance.Transform.localToWorldMatrix;
					Bounds instanceBounds = TransformBounds(_meshBounds, matrix);

					if (count == 0)
					{
						worldBounds = instanceBounds;
					}
					else
					{
						worldBounds.Encapsulate(instanceBounds);
					}

					_matrices[count] = matrix;
					// Negated so an ascending sort puts the furthest first
					_sortKeys[count] = -(instanceBounds.center - cameraPosition).sqrMagnitude;
					count++;
				}

				if (count == 0)
				{
					return;
				}

				// Instances are drawn in array order, so transparent ones (the grass) are drawn back to front as
				// separate renderers would be
				if (_sortBackToFront && canSort)
				{
					Array.Sort(_sortKeys, _matrices, 0, count);
				}

				_renderParams.worldBounds = worldBounds;

				for (int start = 0; start < count; start += MaxInstancesPerDraw)
				{
					Graphics.RenderMeshInstanced(_renderParams, _mesh, _subMesh, _matrices,
						Mathf.Min(MaxInstancesPerDraw, count - start), start);
				}
			}

			private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
			{
				Vector3 extents = bounds.extents;
				Vector3 worldExtents = new Vector3(
					Mathf.Abs(matrix.m00) * extents.x + Mathf.Abs(matrix.m01) * extents.y + Mathf.Abs(matrix.m02) * extents.z,
					Mathf.Abs(matrix.m10) * extents.x + Mathf.Abs(matrix.m11) * extents.y + Mathf.Abs(matrix.m12) * extents.z,
					Mathf.Abs(matrix.m20) * extents.x + Mathf.Abs(matrix.m21) * extents.y + Mathf.Abs(matrix.m22) * extents.z);

				return new Bounds(matrix.MultiplyPoint3x4(bounds.center), worldExtents * 2);
			}
		}
	}
}
