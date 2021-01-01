using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(DirtAppearanceSetterData),
		menuName = SONames.GameDir + nameof(DirtAppearanceSetterData))]
	internal partial class DirtAppearanceSetterData : SerializedScriptableObject
	{
		// TODO split all this data out into a scriptable object
		[SerializeField, //ValidateInput(nameof(AreThereNoDuplicateChannelSetups)),
		 ListDrawerSettings(Expanded = true, NumberOfItemsPerPage = 4)]
		private ChannelSetup[] channels;

		private static readonly GridVector[] CardinalDirections =
			new[] {GridVector.Up, GridVector.Right, GridVector.Down, GridVector.Left};

		private static readonly int CardinalDirectionCount = CardinalDirections.Length;

		private Dictionary<GridVector, Dictionary<GridVector, ChannelSetup>> _setupSearchTree;

		private void Awake()
		{
#if UNITY_EDITOR
			InitChannelArray();
#endif

			SetupChannelSearchTree();
		}

		private void OnValidate()
		{
			SetupChannelSearchTree();
		}

		public ChannelSetup GetChannel(GridVector inDirection, GridVector outDirection)
		{
#if UNITY_EDITOR
			SetupChannelSearchTree();
#endif

			return _setupSearchTree[inDirection][outDirection];
		}

		private void SetupChannelSearchTree()
		{
			_setupSearchTree = new Dictionary<GridVector, Dictionary<GridVector, ChannelSetup>>(CardinalDirectionCount);
			for (int i = 0; i < CardinalDirectionCount; i++)
			{
				GridVector cardinalDirectionIn = CardinalDirections[i];
				Dictionary<GridVector, ChannelSetup> subTree =
					new Dictionary<GridVector, ChannelSetup>(CardinalDirectionCount);
				for (int j = 0; j < CardinalDirectionCount; j++)
				{
					GridVector cardinalDirectionOut = CardinalDirections[j];
					ChannelSetup relevantChannel = channels.First(c =>
						c.inDirection == cardinalDirectionIn && c.outDirection == cardinalDirectionOut);
					subTree.Add(cardinalDirectionOut, relevantChannel);
				}

				_setupSearchTree.Add(cardinalDirectionIn, subTree);
			}
		}

		private void InitChannelArray()
		{
			if (channels == null || channels.Length == 0)
			{
				channels = new ChannelSetup[16];
				for (int i = 0; i < CardinalDirectionCount; i++)
				{
					for (int j = 0; j < CardinalDirectionCount; j++)
					{
						channels[i * CardinalDirectionCount + j] =
							new ChannelSetup(CardinalDirections[i], CardinalDirections[j]);
					}
				}
			}
		}
	}
}