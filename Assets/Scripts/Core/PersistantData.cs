using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace Core
{
    internal static partial class PersistantData
    {
        [ShowInInspector]
        public static MowerModule Mower { get; } = new MowerModule();

        [ShowInInspector]
        public static LevelModule Level { get; } = new LevelModule(false);
    }
}
