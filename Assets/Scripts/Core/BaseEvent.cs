using System;

namespace Core
{
	public abstract class BaseEvent : BaseEventChannel
	{
		protected override Delegate EventDelegate => (Action) Raise;

		protected abstract void Raise();
	}
}