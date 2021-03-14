using Core;
using UnityEngine;

namespace Game.Core
{
	[CreateAssetMenu(fileName = nameof(GamePlayData), menuName = SoNames.GameDir + nameof(GamePlayData))]
	internal class GamePlayData : ScriptableObject
	{
		[SerializeField] private float levelIntroDuration = 2;

		public float LevelIntroDuration => levelIntroDuration;
	}
}