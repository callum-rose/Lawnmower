using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Game.Core;
using Game.UndoSystem;

namespace Game.Tiles
{
    [RequireComponent(typeof(IAppearanceSetter))]
    internal partial class GrassTile : Tile
    {
        [SerializeField] private DirtAppearanceSetter dirtAppearanceSetter;
        
        public const int MaxGrassHeight = 3;
        public const int PerfectGrassHeight = 1;

        public event UndoableAction Ruined;

        public override bool IsComplete => GrassHeight == PerfectGrassHeight;

        private int ___grassHeight;

        [ShowInInspector, PropertyRange(0, MaxGrassHeight)]
        public int GrassHeight
        {
            get => ___grassHeight;
            set
            {
                if (value < 0 || value > MaxGrassHeight)
                {
                    return;
                }
                
                ___grassHeight = value;
                SetAppearance(___grassHeight);
            }
        }

        private static MaterialPropertyBlock _propertyBlock;

        private IAppearanceSetter _appearanceSetter;


        private GridVector _lastTraverseOntoDirection;

        #region Unity

        private void Awake()
        {
            _appearanceSetter = GetComponent<IAppearanceSetter>();
        }

        #endregion

        #region API

        public override void Setup(BaseTileSetupData data)
        {
            if (!(data is GrassTileSetupData grassData))
            {
                throw new ArgumentException($"Argument must be of type {nameof(GrassTileSetupData)}");
            }

            GrassHeight = grassData.grassHeight;
        }

        public override bool IsTraversable(bool editMode)
        {
            if (!editMode)
            {
                return true;
            }
            else
            {
                return GrassHeight < MaxGrassHeight;
            }
        }

        public override void TraverseOnto(GridVector fromDirection, Xor inverted)
        {
            if (!inverted)
            {
                GrassHeight--;
                
                if (GrassHeight < 1)
                {
                    Ruined.Invoke(inverted);
                }
            }
            else
            {
                GrassHeight++;
            }

            _lastTraverseOntoDirection = fromDirection;
        }

        public override void TraverseAway(GridVector toDirection, Xor inverted)
        {
            if (GrassHeight > 0)
            {
                return;
            }
            
            dirtAppearanceSetter.Set(_lastTraverseOntoDirection, toDirection);
        }

        #endregion

        #region Methods

        //[Button("Set Grass Height", Expanded = true), BoxGroup("Debug")]
        private void SetAppearance([PropertyRange(1, MaxGrassHeight)] int grassHeight)
        {
            if (_appearanceSetter == null)
            {
                _appearanceSetter = GetComponent<IAppearanceSetter>();
            }

            ___grassHeight = grassHeight;
            _appearanceSetter.SetAppearance(this);
        }

        #endregion
    }
}
