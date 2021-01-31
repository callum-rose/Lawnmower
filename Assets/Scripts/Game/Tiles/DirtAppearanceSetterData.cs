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

		private static readonly GridVector[] cardinalDirections =
		{
			GridVector.Up,
			GridVector.Right,
			GridVector.Down,
			GridVector.Left
		};

		private static readonly int cardinalDirectionCount = cardinalDirections.Length;

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
			_setupSearchTree = new Dictionary<GridVector, Dictionary<GridVector, ChannelSetup>>(cardinalDirectionCount);
			for (int i = 0; i < cardinalDirectionCount; i++)
			{
				GridVector cardinalDirectionIn = cardinalDirections[i];
				Dictionary<GridVector, ChannelSetup> subTree =
					new Dictionary<GridVector, ChannelSetup>(cardinalDirectionCount);
				for (int j = 0; j < cardinalDirectionCount; j++)
				{
					GridVector cardinalDirectionOut = cardinalDirections[j];
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
				for (int i = 0; i < cardinalDirectionCount; i++)
				{
					for (int j = 0; j < cardinalDirectionCount; j++)
					{
						channels[i * cardinalDirectionCount + j] =
							new ChannelSetup(cardinalDirections[i], cardinalDirections[j]);
					}
				}
			}
		}
	}
}