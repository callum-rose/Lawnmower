using System;
using Game.Core;
using UnityEngine;

namespace Game.Tiles
{
	internal partial class DirtAppearanceSetterData
	{
		[Serializable]
		internal struct ChannelSetup
		{
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

			public DirtAppearanceSetter.ChannelType type;

			public float angle;

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