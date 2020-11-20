using Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Tiles
{
    [CreateAssetMenu(fileName = nameof(GrassMaterialDataHolder), menuName = SONames.GameDir + nameof(GrassMaterialDataHolder))]
    internal class GrassMaterialDataHolder : SerializedScriptableObject
    {
        [OdinSerialize] private Dictionary<int, GrassData> grassColours;
        [SerializeField] private float colourChannelMaxVariation = 0.1f;

        public GrassData GetDataForHeight(int height)
        {
            return grassColours[height];
        }

        public Color VaryColour(Color input)
        {
            float GetRandomChannelOffset() => UnityEngine.Random.Range(-colourChannelMaxVariation, colourChannelMaxVariation);
            return input + new Color(GetRandomChannelOffset(), GetRandomChannelOffset(), GetRandomChannelOffset());
        }

        [Serializable]
        public struct GrassData
        {
            [SerializeField, FormerlySerializedAs("@base"), FormerlySerializedAs("base")] private Color baseColour;
            [SerializeField, FormerlySerializedAs("tip")] private Color tipColour;

            [SerializeField] private Vector2 colourFadeYRange;

            public Color BaseColour => baseColour;
            public Color TipColour => tipColour;

            public Vector2 ColourFadeYRange => colourFadeYRange;
        }
    }
}