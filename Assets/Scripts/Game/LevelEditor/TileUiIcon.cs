﻿using Game.Tiles;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.LevelEditor
{
    internal class TileUiIcon : MonoBehaviour
    {
        [SerializeField] private RawImage image;

        public event Action<TileData> Clicked;

        private TileData _data;

        public void Setup(TileData data, RenderTexture texture)
        {
            _data = data;
            image.texture = texture;
        }

        public void Test()
        {
            Clicked.Invoke(_data);
        }
    }
}