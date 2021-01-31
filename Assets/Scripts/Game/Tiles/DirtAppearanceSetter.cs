using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BalsamicBits.Extensions;
using Core;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	internal partial class DirtAppearanceSetter : MonoBehaviour
	{
		[TitleGroup("Setup"), SerializeField]
		private SerialisedDictionary<ChannelType, GameObject> channelToGameObjectDict =
			new SerialisedDictionary<ChannelType, GameObject>(
				EnumExtensions.GetValues<ChannelType>().ToDictionary(ct => ct, ct => null as GameObject));

		[SerializeField] private DirtAppearanceSetterData data;

		private GridVector _lastInDirection, _lastOutDirection;

		public void Set(GridVector inDirection)
		{
			// simulates a deadend
			Set(inDirection, -inDirection);
		}

		[Button(Expanded = true)]
		public void Set(GridVector inDirection, GridVector outDirection)
		{
			// if (_lastInDirection == inDirection && _lastOutDirection == outDirection)
			// {
			// 	return;
			// }

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
			Straight = 1,
			RightAngle,
			DeadEnd
		}
	}
}