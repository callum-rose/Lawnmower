using System;

namespace Core
{
	public static partial class PersistantData
	{
		public static class MowerModule
		{
			public static readonly IPersistentDataItem<Guid> CurrentId = new PlayerPrefsItem<Guid>("mowerId", Guid.Empty);
		}
	}
}