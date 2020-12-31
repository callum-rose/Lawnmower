using System;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace MainMenu
{
	public class RenderTextureCreator : MonoBehaviour
	{
		[TitleGroup("Targets")] [SerializeField]
		private Camera targetCamera;

		[SerializeField] private RawImage targetImage;

		[TitleGroup("Parameters")] [SerializeField]
		private bool useScreenSize = true;

		[SerializeField, HideIf(nameof(useScreenSize))]
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