using Core;
using Core.EventChannels;
using UnityEngine;

namespace Game.Tiles
{
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(GrassShaderManager), menuName = SoNames.GameDir + nameof(GrassShaderManager))]
	internal class GrassShaderManager : ScriptableObject
	{
		[Header("Wind")]
		[SerializeField]
		private ShaderProperty<float> windNoiseScale;

		[SerializeField]
		private ShaderProperty<Vector2> windNoiseVelocity;

		[Header("Bend")]
		[SerializeField]
		private ShaderProperty<float> bendNoiseScale;

		[Header("Mower Wind")]
		[SerializeField]
		private ShaderProperty<Vector3> mowerPosition;

		[SerializeField]
		private ShaderProperty<float> mowerWindSize;

		[SerializeField]
		private ShaderProperty<float> mowerWindZoneWidth;

		[SerializeField]
		private ShaderProperty<float> mowerWindFlutterSpeed;

		[Header("Event Channels")]
		[SerializeField] private Vector3EventChannel mowerObjectMovedEventChannel;
		
		private ShaderPropertyBase[] _allProperties;

		private void OnEnable()
		{
			mowerObjectMovedEventChannel.EventRaised += OnMowerObjectMoved;
			
			GroupProperties();

			SetIds();
			SetGlobals();
		}

		private void OnDisable()
		{
			mowerObjectMovedEventChannel.EventRaised -= OnMowerObjectMoved;
		}

		private void OnValidate()
		{
			GroupProperties();
			
			SetGlobals();
		}

		private void OnMowerObjectMoved(Vector3 position)
		{
			mowerPosition.SetAndApply(position);
		}

		private void SetGlobals()
		{
			foreach (ShaderPropertyBase property in _allProperties)
			{
				property.Apply();
			}
		}

		[ContextMenu("Set Ids")]
		private void SetIds()
		{
			foreach (ShaderPropertyBase property in _allProperties)
			{
				property.SetId();
			}
		}

		private void GroupProperties()
		{
			_allProperties = new ShaderPropertyBase[]
			{
				windNoiseScale, windNoiseVelocity,
				bendNoiseScale,
				mowerPosition, mowerWindSize, mowerWindZoneWidth, mowerWindFlutterSpeed
			};
		}
	}
}