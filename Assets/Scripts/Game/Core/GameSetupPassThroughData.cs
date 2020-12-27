using Core;
using Game.Levels;
using Game.Mowers;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    internal class GameSetupPassThroughData : PassThroughData
    {
        public MowerData Mower { get; set; }
        public LevelData Level { get; set; }
    }
}
