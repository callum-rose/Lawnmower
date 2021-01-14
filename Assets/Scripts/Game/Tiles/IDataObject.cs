using System;

namespace Game.Tiles
{
	internal interface IDataObject<in T> : IDisposable where T : class
	{
		void Bind(T data);
	}
}