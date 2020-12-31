using System;
using Sirenix.OdinInspector;

namespace Core
{
	internal static partial class PersistantData
	{
		[Serializable]
		public class MowerModule
		{
			[ShowInInspector]
			public IPersistentDataItem<Guid> CurrentId { get; } = new PlayerPrefsItem<Guid>("mowerId", Guid.Empty);
		}
	}
}