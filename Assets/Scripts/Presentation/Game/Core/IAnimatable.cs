using System;

namespace Game.Core
{
	internal interface IAnimatable<in T> where T : Enum
	{
		void Animate(T state);
	}
}