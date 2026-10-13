namespace Lawnmower.Presenters
{
	/// <summary>Seconds since some fixed point. Unity's implementation uses Time.time.</summary>
	public interface IClock
	{
		double Now { get; }
	}
}
