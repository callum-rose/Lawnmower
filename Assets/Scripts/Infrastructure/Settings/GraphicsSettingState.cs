using System;

namespace Settings
{
	/// <summary>
	/// The graphics setting in force. The saved settings set it at startup and when the player changes it; shader
	/// keywords and camera depth follow it through <see cref="Changed"/>.
	/// </summary>
	public static class GraphicsSettingState
	{
		public const GraphicsSetting Default = GraphicsSetting.High;

		private static GraphicsSetting _current = Default;

		public static event Action<GraphicsSetting> Changed;

		public static GraphicsSetting Current
		{
			get => _current;
			set
			{
				if (_current == value)
				{
					return;
				}

				_current = value;
				Changed?.Invoke(value);
			}
		}
	}
}
