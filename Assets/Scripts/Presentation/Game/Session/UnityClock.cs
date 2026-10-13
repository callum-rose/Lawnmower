using Lawnmower.Presenters;
using UnityEngine;

namespace Game.Core
{
	internal sealed class UnityClock : IClock
	{
		public double Now => Time.time;
	}
}
