using System;
using Game.Core;
using IUnified;

namespace Game.Mowers
{
	internal interface INeedPositioner
	{
		void Set(Positioner positioner);
	}

	[Serializable]
	internal class INeedPositionerContainer : IUnifiedContainer<INeedPositioner>
	{
		
	}
}