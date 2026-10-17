using UnityEngine;

namespace Game.Tiles
{
	/// <summary>A rabbit hole: colours its carrot (the model's PairMarker part) by the hole's pair.</summary>
	internal class RabbitHoleTileObject : BaseTileObject
	{
		[SerializeField] private Renderer pairMarker;

		[Tooltip("One per pair, in RabbitHolePairs order: the palette material of that pair's carrot")]
		[SerializeField] private Material[] pairMaterials;

		private IReadonlyRabbitHoleTile _data;

		public override void Bind(IReadonlyTile data)
		{
			_data = (IReadonlyRabbitHoleTile)data;

			if (pairMarker != null && pairMaterials is { Length: > 0 })
			{
				pairMarker.sharedMaterial = pairMaterials[RabbitHolePairs.Index(_data.Pair) % pairMaterials.Length];
			}
		}

		public override void Dispose()
		{
		}
	}
}
