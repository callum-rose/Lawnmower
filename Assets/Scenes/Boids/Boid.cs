using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class Boid
{
	internal struct Data
	{
		public Vector3 Position;
		public Vector3 Velocity;
		
		public float DistanceFrom(Data boid)
		{
			return Vector3.Distance(Position, boid.Position);
		}

		public Vector3 DisplacementFrom(Data boid)
		{
			return Position - boid.Position;
		}
	}

	public Data Info;

	public Vector3 Position => Info.Position;
	public Vector3 Velocity => Info.Velocity;

	public Boid(Vector3 position, Vector3 velocity)
	{
		Info = new Data
		{
			Position = position,
			Velocity = velocity
		};
	}
	
	public float DistanceFrom(Boid boid)
	{
		return Info.DistanceFrom(boid.Info);
	}

	public Vector3 DisplacementFrom(Boid boid)
	{
		return Info.DisplacementFrom(boid.Info);
	}
}