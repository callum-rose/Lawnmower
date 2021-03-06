using System;
using System.Collections;
using BalsamicBits.Extensions;
using Game.Core;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Mowers.Models
{
	internal class FaceAnimationComponent : MonoBehaviour
	{
		[SerializeField] private MowerMovementManager mowerMovementManager;

		[SerializeField] private Material material;
		[SerializeField] private Texture idleFace;
		[SerializeField] private Texture mouthOpenFace;
		[SerializeField] private Texture blinkFace;
		[SerializeField] private Texture[] lookAroundFaces;

		[SerializeField] private float durationBeforeLookAround = 3f;
		[SerializeField, Range(0, 1)] private float blinkToLookAroundRatio = 0.33f;
		[SerializeField] private float blinkDuration = 0.2f;
		[SerializeField, MinMaxSlider(0, 5)] private Vector2 lookElsewhereAfterDurationRange = new Vector2(1, 3);

		private static readonly int decalTex = Shader.PropertyToID("_DecalTex");

		private Coroutine _currentRoutine;

		private void OnEnable()
		{
			mowerMovementManager.Moved += MowerMovementManagerOnMoved;

			_currentRoutine = StartCoroutine(LookAroundRoutine());
		}

		private void OnDisable()
		{
			mowerMovementManager.Moved -= MowerMovementManagerOnMoved;
		}

		private void MowerMovementManagerOnMoved(GridVector from, GridVector to, Xor inverted)
		{
			Animate();
		}

		private void Animate()
		{
			SetFace(mouthOpenFace);

			if (_currentRoutine != null)
			{
				StopCoroutine(_currentRoutine);
			}

			_currentRoutine = this.Timer(
				0.25f,
				() =>
				{
					SetFace(idleFace);
					_currentRoutine = StartCoroutine(LookAroundRoutine());
				});
		}

		private IEnumerator LookAroundRoutine()
		{
			yield return new WaitForSeconds(durationBeforeLookAround);

			while (true)
			{
				if (Random.value < blinkToLookAroundRatio)
				{
					SetFace(blinkFace);

					yield return new WaitForSeconds(blinkDuration);
				}

				Texture face = GetRandomLookFace();
				SetFace(face);

				float randomDuration =
					Random.Range(lookElsewhereAfterDurationRange.x, lookElsewhereAfterDurationRange.y);

				yield return new WaitForSeconds(randomDuration);
			}
		}

		private void SetFace(Texture texture)
		{
			material.SetTexture(decalTex, texture);
		}

		private Texture GetRandomLookFace()
		{
			int index = Random.Range(0, lookAroundFaces.Length - 1);
			return lookAroundFaces[index];
		}
	}
}