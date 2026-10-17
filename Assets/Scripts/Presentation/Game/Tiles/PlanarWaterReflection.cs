using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Tiles
{
	// Renders the scene mirrored about a horizontal plane at this transform's height into a global texture that the
	// Water shader samples. Water renderers must be on a layer excluded from reflectedLayers so they don't reflect themselves.
	public class PlanarWaterReflection : MonoBehaviour
	{
		private static readonly int TextureId = Shader.PropertyToID("_PlanarReflectionTexture");
		private static readonly int EnabledId = Shader.PropertyToID("_PlanarReflectionEnabled");
		private static readonly int DepthTextureId = Shader.PropertyToID("_PlanarReflectionDepth");
		private static readonly int ProjectionZId = Shader.PropertyToID("_PlanarReflectionProjZ");
		private static readonly int ProjectionWId = Shader.PropertyToID("_PlanarReflectionProjW");

		[SerializeField] private Camera targetCamera;
		[SerializeField, Range(0.25f, 1)] private float resolutionScale = 0.5f;
		[SerializeField] private LayerMask reflectedLayers = ~(1 << 4);
		[SerializeField] private float clipPlaneOffset = 0.01f;

		private static Camera _depthCamera;
		private static RTHandle _depthHandle;

		private Camera _reflectionCamera;
		private RenderTexture _texture;

		// Where PlanarReflectionDepthFeature copies the reflection camera's depth to.
		public static bool TryGetDepthTarget(Camera camera, out RTHandle target)
		{
			target = _depthHandle;
			return camera != null && camera == _depthCamera && _depthHandle != null;
		}

		public Camera TargetCamera
		{
			get => targetCamera;
			set => targetCamera = value;
		}

		private void LateUpdate()
		{
			Render();
		}

		private void OnDisable()
		{
			Shader.SetGlobalFloat(EnabledId, 0);
			if (_texture != null)
			{
				_texture.Release();
				DestroyObject(_texture);
				_texture = null;
			}

			ReleaseDepth();

			if (_reflectionCamera != null)
			{
				DestroyObject(_reflectionCamera.gameObject);
				_reflectionCamera = null;
			}
		}

		public void Render()
		{
			Camera source = targetCamera != null ? targetCamera : Camera.main;
			if (source == null)
			{
				return;
			}

			EnsureResources(source);

			float height = transform.position.y;
			Matrix4x4 reflection = ReflectionMatrix(height);

			_reflectionCamera.CopyFrom(source);
			_reflectionCamera.cullingMask = reflectedLayers;
			_reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
			_reflectionCamera.backgroundColor = Color.clear;
			_reflectionCamera.targetTexture = _texture;

			Vector3 forward = Vector3.Reflect(source.transform.forward, Vector3.up);
			Vector3 up = Vector3.Reflect(source.transform.up, Vector3.up);
			_reflectionCamera.transform.SetPositionAndRotation(reflection.MultiplyPoint(source.transform.position),
				Quaternion.LookRotation(forward, up));
			_reflectionCamera.worldToCameraMatrix = source.worldToCameraMatrix * reflection;

			Matrix4x4 view = _reflectionCamera.worldToCameraMatrix;
			Vector3 planePoint = view.MultiplyPoint(new Vector3(0, height + clipPlaneOffset, 0));
			Vector3 planeNormal = view.MultiplyVector(Vector3.up).normalized;
			_reflectionCamera.projectionMatrix =
				source.CalculateObliqueMatrix(new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector3.Dot(planePoint, planeNormal)));

			var request = new UniversalRenderPipeline.SingleCameraRequest { destination = _texture };
			if (!RenderPipeline.SupportsRenderRequest(_reflectionCamera, request))
			{
				return;
			}

			_depthCamera = _reflectionCamera;
			GL.invertCulling = true;
			RenderPipeline.SubmitRenderRequest(_reflectionCamera, request);
			GL.invertCulling = false;

			// The Water shader turns the mirrored depth back into each reflected point's height above the surface, so the
			// rows of the (oblique) projection that produce clip-space z and w go with it.
			Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(_reflectionCamera.projectionMatrix, true);
			Shader.SetGlobalTexture(TextureId, _texture);
			Shader.SetGlobalTexture(DepthTextureId, _depthHandle);
			Shader.SetGlobalVector(ProjectionZId, gpuProjection.GetRow(2));
			Shader.SetGlobalVector(ProjectionWId, gpuProjection.GetRow(3));
			Shader.SetGlobalFloat(EnabledId, 1);
		}

		private void EnsureResources(Camera source)
		{
			int width = Mathf.Max(1, Mathf.RoundToInt(source.pixelWidth * resolutionScale));
			int height = Mathf.Max(1, Mathf.RoundToInt(source.pixelHeight * resolutionScale));

			if (_texture == null || _texture.width != width || _texture.height != height)
			{
				if (_texture != null)
				{
					_texture.Release();
					DestroyObject(_texture);
				}

				_texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf)
				{
					name = "PlanarWaterReflection",
					useMipMap = true,
					autoGenerateMips = true,
					hideFlags = HideFlags.DontSave,
				};

				ReleaseDepth();
				_depthHandle = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R32_SFloat,
					wrapMode: TextureWrapMode.Clamp, name: "PlanarWaterReflectionDepth");
			}

			if (_reflectionCamera == null)
			{
				var go = new GameObject("PlanarWaterReflectionCamera") { hideFlags = HideFlags.HideAndDontSave };
				_reflectionCamera = go.AddComponent<Camera>();
				_reflectionCamera.enabled = false;
				UniversalAdditionalCameraData data = _reflectionCamera.GetUniversalAdditionalCameraData();
				data.renderPostProcessing = false;
				data.renderShadows = false;
				data.requiresColorOption = CameraOverrideOption.Off;
				data.requiresDepthOption = CameraOverrideOption.Off;
			}
		}

		private void ReleaseDepth()
		{
			_depthHandle?.Release();
			_depthHandle = null;
			_depthCamera = null;
		}

		private static void DestroyObject(Object obj)
		{
			if (Application.isPlaying)
			{
				Destroy(obj);
			}
			else
			{
				DestroyImmediate(obj);
			}
		}

		private static Matrix4x4 ReflectionMatrix(float height)
		{
			Matrix4x4 m = Matrix4x4.identity;
			m.m11 = -1;
			m.m13 = 2 * height;
			return m;
		}
	}
}
