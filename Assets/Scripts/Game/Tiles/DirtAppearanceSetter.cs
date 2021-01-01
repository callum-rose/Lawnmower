using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BalsamicBits.Extensions;
using Core;
using Game.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.Tiles
{
	internal partial class DirtAppearanceSetter : SerializedMonoBehaviour
	{
		[TitleGroup("Setup"), OdinSerialize] private Dictionary<ChannelType, GameObject> channelToGameObjectDict =
			EnumExtensions.GetValues<ChannelType>().ToDictionary(ct => ct, ct => null as GameObject);

		[SerializeField] private DirtAppearanceSetterData data;
		[SerializeField] private Vector3EventChannel particlesEventChannel;

		private GridVector _lastInDirection, _lastOutDirection;
		
		public void Set(GridVector inDirection, GridVector outDirection)
		{
			if (_lastInDirection == inDirection && _lastOutDirection == outDirection)
			{
				return;
			}
			
			DirtAppearanceSetterData.ChannelSetup channel = GetChannel(inDirection, outDirection);
			foreach (KeyValuePair<ChannelType, GameObject> kv in channelToGameObjectDict)
			{
				bool isRelevantChannel = kv.Key == channel.type;
				GameObject relevantGameObject = kv.Value;

				relevantGameObject.SetActive(isRelevantChannel);

				if (isRelevantChannel)
				{
					relevantGameObject.transform.localRotation = Quaternion.Euler(0, channel.angle, 0);
				}
			}
			
			particlesEventChannel.Raise(transform.position);

			_lastInDirection = inDirection;
			_lastOutDirection = outDirection;
		}

		private DirtAppearanceSetterData.ChannelSetup GetChannel(GridVector inDirection, GridVector outDirection)
		{
			return data.GetChannel(inDirection, outDirection);
		}

		[SuppressMessage("ReSharper", "UnusedMember.Local")]
		internal enum ChannelType
		{
			None,
			Straight,
			RightAngle,
			DeadEnd
		}
	}
}