using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
	public class RenderTextureCreator : MonoBehaviour
	{
		[Header("Targets")] [SerializeField]
		private Camera targetCamera;

		[SerializeField] private RawImage targetImage;

		[Header("Parameters")] [SerializeField]
		private bool useScreenSize = true;

		[SerializeField]
		private Vector2Int resolution;

		private RenderTexture _renderTexture;

		#region Unity

		private void Start()
		{
			CreateRenderTexture();

			SetupCamera();
			SetupImage();
		}

		private void OnDestroy()
		{
			Destroy(_renderTexture);
		}

		#endregion

		#region Methods

		private void SetupImage()
		{
			targetImage.texture = _renderTexture;
		}

		private void SetupCamera()
		{
			targetCamera.targetTexture = _renderTexture;
		}

		private void CreateRenderTexture()
		{
			if (useScreenSize)
			{
				_renderTexture = new RenderTexture(Screen.width, Screen.height, 1);
			}
			else
			{
				_renderTexture = new RenderTexture(resolution.x, resolution
					.y, 1);
			}
		}

		#endregion
	}
}