using System;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	internal partial class DirtAppearanceSetterData
	{
		[Serializable]
		internal struct ChannelSetup
		{
			[ShowInInspector, HideLabel, DisplayAsString, PropertyOrder(1)]
			private string Desciption => "In: " + inDirection + " --> Out: " + outDirection;

			// [
			// 	//ValueDropdown(nameof(CardinalDirections), HideChildProperties = true), 
			// 	DisplayAsString,
			// 	HorizontalGroup(0.5f),
			// 	LabelWidth(40), LabelText("In"), SuffixLabel("-->")]
			[HideInInspector] public GridVector inDirection;

			// [
			// 	//ValueDropdown(nameof(CardinalDirections), HideChildProperties = true),
			// 	DisplayAsString,
			// 	HorizontalGroup, LabelWidth(40),
			// 	LabelText("Out")]
			[HideInInspector] public GridVector outDirection;

			[PropertyOrder(2)] public DirtAppearanceSetter.ChannelType type;

			[ValueDropdown(nameof(Angles), HideChildProperties = true), PropertyOrder(2)]
			public float angle;

			private static readonly float[] Angles = {0, 90, 180, 270};

			public ChannelSetup(GridVector inDirection, GridVector outDirection)
			{
				this.inDirection = inDirection;
				this.outDirection = outDirection;

				type = DirtAppearanceSetter.ChannelType.Straight;
				angle = 0;
			}
		}
	}
}