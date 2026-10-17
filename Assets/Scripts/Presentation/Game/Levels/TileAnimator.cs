using Core;
using Core.EventChannels;
using DG.Tweening;
using Game.Core;
using Game.Tiles;
using UnityEngine;
using Lawnmower.Model;

namespace Game.Levels
{
	/// <summary>
	/// The level intro for the tiles: the terrain is already there, and the grass grows up out of it in a wave from the
	/// level's centre, over raked dirt. The shaders do the growing from the globals set here (see GrassGrowth.hlsl), so
	/// the tiles stay instanced; the other tiles pop up in step with the wave.
	/// </summary>
	[CreateAssetMenu(fileName = nameof(TileAnimator), menuName = SoNames.GameDir + nameof(TileAnimator))]
	[UnreferencedScriptableObject]
	internal class TileAnimator : ScriptableObject
	{
		private static readonly int RemainingId = Shader.PropertyToID("GrassGrowRemaining");
		private static readonly int CentreId = Shader.PropertyToID("GrassGrowCentre");
		private static readonly int RadiusId = Shader.PropertyToID("GrassGrowRadius");
		private static readonly int SpreadId = Shader.PropertyToID("GrassGrowSpread");

		// Mirrors GRASS_GROW_DISTANCE_WEIGHT in GrassGrowth.hlsl: how much of a blade's delay is its distance from the centre
		private const float WaveDistanceWeight = 0.75f;

		[Tooltip("From the first blade starting to grow to the last one finishing")]
		[SerializeField, Min(0.01f)] private float growDuration = 1f;
		[Tooltip("How much of the grow duration the wave takes to cross the level; each blade grows in the rest")]
		[SerializeField, Range(0, 0.95f)] private float waveSpread = 0.55f;

		[Header("Other Tiles")]
		[SerializeField, Min(0)] private float popDuration = 0.35f;
		[SerializeField] private Ease popEase = Ease.OutBack;

		[Header("Event Channels")]
		[SerializeField] private ILevelDataEventChannelListenerContainer levelStartedEventChannelContainer;

		[SerializeField] private TileObjectEventChannel tileObjectCreatedEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer tileAnimationsFinishedEventChannel;

		private ILevelDataEventChannelListener LevelStartedEventChannel => levelStartedEventChannelContainer.Result;

		private IVoidEventChannelTransmitter TileAnimationsFinishedEventChannel =>
			tileAnimationsFinishedEventChannel.Result;

		private Level _level;
		private Vector3 _centre;
		private float _radius;
		private Tween _growTween;

		private void OnEnable()
		{
			LevelStartedEventChannel.EventRaised += OnLevelStarted;
			tileObjectCreatedEventChannel.EventRaised += OnTileObjectObjectCreated;
			SetRemaining(0);
		}

		private void OnDisable()
		{
			LevelStartedEventChannel.EventRaised -= OnLevelStarted;
			tileObjectCreatedEventChannel.EventRaised -= OnTileObjectObjectCreated;
			_growTween?.Kill();
			SetRemaining(0);
		}

		private void OnLevelStarted(Level level)
		{
			_growTween?.Kill();
			_growTween = null;
			_level = level;

			// Hidden until the tiles are built, so the grass never shows full grown first
			SetRemaining(1);
		}

		private void OnTileObjectObjectCreated(GameObject tile, GridVector position)
		{
			if (_growTween == null)
			{
				SetWave(tile.transform.position, position);
				_growTween = StartGrowing();
			}

			if (!tile.TryGetComponent(out GrassTileObject _))
			{
				Pop(tile.transform);
			}
		}

		// Tiles sit at their cell's centre, so the first one gives where the whole level is
		private void SetWave(Vector3 tilePosition, GridVector position)
		{
			Vector3 levelOrigin = tilePosition - new Vector3(position.x + 0.5f, 0, position.y + 0.5f) * LevelDimensions.TileSize;
			Vector3 size = new Vector3(_level.Width, 0, _level.Depth) * LevelDimensions.TileSize;
			_centre = levelOrigin + size * 0.5f;
			_radius = Mathf.Max(LevelDimensions.TileSize, size.magnitude * 0.5f);

			Shader.SetGlobalVector(CentreId, _centre);
			Shader.SetGlobalFloat(RadiusId, _radius);
			Shader.SetGlobalFloat(SpreadId, waveSpread);
		}

		private Tween StartGrowing()
		{
			return DOVirtual.Float(1, 0, growDuration, SetRemaining)
				.SetEase(Ease.Linear)
				.OnComplete(() =>
				{
					_growTween = null;
					TileAnimationsFinishedEventChannel.Raise();
				});
		}

		// Matches the grass's wave, minus the per-blade jitter
		private void Pop(Transform tile)
		{
			if (popDuration <= 0)
			{
				return;
			}

			Vector3 fromCentre = tile.position - _centre;
			float distance = new Vector2(fromCentre.x, fromCentre.z).magnitude;
			float delay = Mathf.Clamp01(distance / _radius * WaveDistanceWeight) * waveSpread * growDuration;

			Vector3 scale = tile.localScale;
			tile.localScale = scale * 0.01f;
			tile.DOScale(scale, popDuration).SetDelay(delay).SetEase(popEase).SetLink(tile.gameObject);
		}

		private static void SetRemaining(float remaining)
		{
			Shader.SetGlobalFloat(RemainingId, remaining);
		}
	}
}
