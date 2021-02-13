using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class Boid
{
	internal struct Data
	{
		public Vector3 Position;
		public Vector3 Velocity;
	}

	public Data Info;

	public Vector3 Position => Info.Position;
	public Vector3 Velocity => Info.Velocity;
	
	private readonly IBoidRule[] _rules;
	private readonly IVelocityPostProcessor _velocityPostProcessor;

	private Boid[] _otherBoids;

	public Boid(Vector3 position, Vector3 velocity, IEnumerable<IBoidRule> rules,
		IVelocityPostProcessor velocityPostProcessor)
	{
		Info = new Data
		{
			Position = position,
			Velocity = velocity
		};

		_rules = rules.ToArray();
		_velocityPostProcessor = velocityPostProcessor;
	}

	public void SetOtherBoids(IEnumerable<Boid> otherBoids)
	{
		_otherBoids = otherBoids.ToArray();
	}

	public void Tick(float deltaTime)
	{
		Vector3 tempVelocity = Velocity;

		foreach (IBoidRule rule in _rules)
		{
			tempVelocity += rule.CalcAcceleration(this, _otherBoids);
		}

		Info.Velocity = _velocityPostProcessor.Process(tempVelocity);
		Info.Position += Velocity * deltaTime;
	}

	public float DistanceFrom(Boid boid)
	{
		return Vector3.Distance(Position, boid.Position);
	}

	public Vector3 DisplacementFrom(Boid boid)
	{
		return Position - boid.Position;
	}
}