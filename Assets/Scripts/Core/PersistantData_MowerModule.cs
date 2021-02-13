using System;
using Sirenix.OdinInspector;

namespace Core
{
	internal static partial class PersistantData
	{
		public static class MowerModule
		{
			public static readonly IPersistentDataItem<Guid> CurrentId = new PlayerPrefsItem<Guid>("mowerId", Guid.Empty);
		}
	}
}