using System;
using System.Collections.Generic;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using UnityEngine;
using UnityEngine.UI;

namespace LevelSelect
{
	/// <summary>
	/// The Level Select screen (Figma 12:204): back button, perfects pill, world tabs and the garden path of level
	/// nodes. Passive: it draws what the presenter gives it and reports taps.
	/// </summary>
	public class LevelSelectScreenView : MonoBehaviour, ILevelSelectView
	{
		[SerializeField] private ButtonView backButton;
		[SerializeField] private PillView perfectsPill;
		[SerializeField] private WorldTabsView worldTabs;
		[SerializeField] private ToastView toast;

		[Header("Garden path")]
		[SerializeField] private ScrollRect scroll;
		[SerializeField] private RectTransform nodeContainer;
		[SerializeField] private PathLineGraphic pathBand;
		[SerializeField] private PathLineGraphic pathDots;
		[SerializeField] private LevelNodeView nodePrefab;
		[Tooltip("Node centres, level 1 first, in content space (x from the centre line, y up from the content's bottom).")]
		[SerializeField] private Vector2[] nodePositions = Array.Empty<Vector2>();
		[Tooltip("How far up the pattern repeats for levels beyond nodePositions.")]
		[SerializeField] private float repeatHeight = 616f;
		[Tooltip("Room above the top node for the bunny and the NEXT! tag.")]
		[SerializeField] private float topPadding = 70f;
		[SerializeField] private float bottomPadding = 16f;

		private readonly List<LevelNodeView> _nodes = new List<LevelNodeView>();

		public event Action<int> LevelChosen;
		public event Action<int> WorldChosen;
		public event Action BackRequested;

		public IReadOnlyList<LevelNodeView> Nodes => _nodes;
		public PillView PerfectsPill => perfectsPill;
		public WorldTabsView WorldTabs => worldTabs;
		public ButtonView BackButton => backButton;
		public ToastView Toast => toast;

		private void Awake()
		{
			backButton.Clicked += OnBackClicked;
			worldTabs.TabChosen += OnTabChosen;
		}

		private void OnDestroy()
		{
			backButton.Clicked -= OnBackClicked;
			worldTabs.TabChosen -= OnTabChosen;
		}

		public void ShowWorlds(IReadOnlyList<WorldSummary> worlds, int selected) => worldTabs.Show(worlds, selected);

		public void ShowPerfects(int perfects, int total) => perfectsPill.SetText($"{perfects}/{total}");

		public void ShowLevels(IReadOnlyList<LevelSummary> levels)
		{
			// Nodes from the scene's last layout (or a previous call) are reused; extras go.
			while (_nodes.Count > levels.Count)
			{
				Destroy(_nodes[_nodes.Count - 1].gameObject);
				_nodes.RemoveAt(_nodes.Count - 1);
			}

			while (_nodes.Count < levels.Count)
			{
				LevelNodeView node = Instantiate(nodePrefab, nodeContainer);
				int index = _nodes.Count;
				node.Clicked += () => LevelChosen?.Invoke(index);
				_nodes.Add(node);
			}

			float height = ContentHeight(levels.Count);
			((RectTransform)scroll.content).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

			List<Vector2> centres = new List<Vector2>(levels.Count);
			int nextIndex = -1;

			for (int i = 0; i < levels.Count; i++)
			{
				Vector2 centre = NodePosition(i) + Vector2.up * bottomPadding;
				centres.Add(centre);

				LevelNodeView node = _nodes[i];
				node.name = $"Level{levels[i].Number:00}";
				RectTransform rect = (RectTransform)node.transform;
				rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
				rect.anchoredPosition = centre;
				node.Show(levels[i].NodeState, levels[i].Number);

				if (levels[i].IsNext)
				{
					nextIndex = i;
				}
			}

			// Higher levels draw over lower ones, so a bunny or tag never hides under the node above.
			for (int i = 0; i < _nodes.Count; i++)
			{
				_nodes[i].transform.SetSiblingIndex(i);
			}

			if (nextIndex >= 0)
			{
				_nodes[nextIndex].transform.SetAsLastSibling();
			}

			pathBand.SetPoints(centres);
			pathDots.SetPoints(centres);
			ScrollTo(nextIndex >= 0 ? centres[nextIndex].y : 0f, height);
		}

		public void ShowLevelLocked(int index, string message)
		{
			if (index >= 0 && index < _nodes.Count)
			{
				_nodes[index].Shake();
			}

			toast.Show(message, 1.5f);
		}

		public void ShowWorldComingSoon(string message) => toast.Show(message, 1.5f);

		private Vector2 NodePosition(int index)
		{
			if (nodePositions.Length == 0)
			{
				return new Vector2(0f, index * 70f);
			}

			int lap = index / nodePositions.Length;
			return nodePositions[index % nodePositions.Length] + Vector2.up * (repeatHeight * lap);
		}

		private float ContentHeight(int count)
		{
			float top = 0f;
			for (int i = 0; i < count; i++)
			{
				top = Mathf.Max(top, NodePosition(i).y);
			}

			float viewport = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
			return Mathf.Max(viewport, bottomPadding + top + topPadding);
		}

		/// <summary>Scrolls so the given content height is roughly centred (level 1 at the bottom by default).</summary>
		private void ScrollTo(float y, float contentHeight)
		{
			float viewport = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
			float range = contentHeight - viewport;
			scroll.verticalNormalizedPosition = range > 0f ? Mathf.Clamp01((y - viewport * 0.5f) / range) : 0f;
		}

		private void OnBackClicked() => BackRequested?.Invoke();

		private void OnTabChosen(int index) => WorldChosen?.Invoke(index);
	}
}
