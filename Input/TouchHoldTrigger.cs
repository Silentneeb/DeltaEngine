﻿using DeltaEngine.Commands;
using DeltaEngine.Datatypes;
using DeltaEngine.Entities;
using DeltaEngine.Extensions;

namespace DeltaEngine.Input
{
	/// <summary>
	/// Allows a touch hold to be detected.
	/// </summary>
	public class TouchHoldTrigger : PositionTrigger, TouchTrigger
	{
		public TouchHoldTrigger(Rectangle holdArea, float holdTime = DefaultHoldTime)
		{
			HoldArea = holdArea;
			HoldTime = holdTime;
		}

		public Rectangle HoldArea { get; private set; }
		public float HoldTime { get; private set; }
		private const float DefaultHoldTime = 1.0f;
		public float Elapsed { get; set; }

		public TouchHoldTrigger(string holdAreaAndTime)
		{
			var parameters = holdAreaAndTime.SplitAndTrim(new[] { ' ' });
			if (parameters.Length > 1)
				HoldArea = parameters[0].Convert<Rectangle>();
			if (parameters.Length > 2)
				HoldTime = parameters[1].Convert<float>();
		}

		protected override void StartInputDevice()
		{
			Start<Touch>();
		}

		public void HandleWithTouch(Touch touch)
		{
			if (touch.GetState(0) == State.Pressing)
				startPosition = touch.GetPosition(0);
			Position = touch.GetPosition(0);
			if (CheckHoverState(touch) && IsHovering())
				Invoke();
			else
				Elapsed = 0.0f;
		}

		private Vector2D startPosition;

		private bool CheckHoverState(Touch touch)
		{
			return HoldArea.Contains(startPosition) &&
				touch.GetState(0) == State.Pressed &&
				startPosition.DistanceTo(touch.GetPosition(0)) < PositionEpsilon;
		}

		public bool IsHovering()
		{
			if (Elapsed >= HoldTime || !HoldArea.Contains(Position))
				return false;
			Elapsed += Time.Delta;
			return Elapsed >= HoldTime;
		}
	}
}