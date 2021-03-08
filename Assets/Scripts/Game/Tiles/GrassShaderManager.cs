using Core;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;
using ReadOnly = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace Game.Tiles
{
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(GrassShaderManager), menuName = SONames.GameDir + nameof(GrassShaderManager))]
	internal class GrassShaderManager : ScriptableObject
	{
		[TitleGroup("Wind")]
		[SerializeField]
		private ShaderProperty<float> windNoiseScale;

		[SerializeField]
		private ShaderProperty<Vector2> windNoiseVelocity;

		[TitleGroup("Bend")]
		[SerializeField]
		private ShaderProperty<float> bendNoiseScale;

		[TitleGroup("Mower Wind")]
		[SerializeField]
		private ShaderProperty<Vector3> mowerPosition;

		[SerializeField]
		private ShaderProperty<float> mowerWindSize;

		[SerializeField]
		private ShaderProperty<float> mowerWindZoneWidth;

		[SerializeField]
		private ShaderProperty<float> mowerWindFlutterSpeed;

		[TitleGroup("Event Channels")]
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

		[Button]
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