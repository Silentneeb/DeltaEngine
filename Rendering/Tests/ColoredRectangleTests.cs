﻿using System;
using DeltaEngine.Datatypes;
using DeltaEngine.Platforms.Tests;
using NUnit.Framework;

namespace DeltaEngine.Rendering.Tests
{
	public class ColoredRectangleTests : TestStarter
	{
		[VisualTest]
		public void CreateFromRectangle(Type resolver)
		{
			var halfScreenRect = new Rectangle(Point.Zero, Size.Half);
			ColoredRectangle box = null;
			Start(resolver, (Renderer r) => r.Add(box = new ColoredRectangle(halfScreenRect, Color.Red)),
				() => Assert.AreEqual(Color.Red, box.Color));
		}

		[VisualTest]
		public void CreateFromPointAndSize(Type resolver)
		{
			ColoredRectangle box = null;
			Start(resolver,
				(Renderer r) => r.Add(box = new ColoredRectangle(Point.Half, Size.Half, Color.Red)),
				() => Assert.AreEqual(Color.Red, box.Color));
		}

		[VisualTest]
		public void AdddingBoxTwiceWillOnlyDisplayItOnce(Type resolver)
		{
			ColoredRectangle box = null;
			Start(resolver, (Renderer r) =>
			{
				box = new ColoredRectangle(Point.Half, Size.Half, Color.Yellow);
				r.Add(box);
				box.DrawArea.Center = new Point(0.6f, 0.6f);
				box.Color = Color.Teal;
				r.Add(box);
			}, () => Assert.AreEqual(Color.Teal, box.Color));
		}

		[VisualTest]
		public void DrawWithRunnerClass(Type resolver)
		{
			Start<BlinkingBox>(resolver);
		}

		[VisualTest]
		public void DrawTwoBoxes(Type resolver)
		{
			Start(resolver, (Renderer r) =>
			{
				r.Add(new ColoredRectangle(new Point(0.4f, 0.4f), Size.Half, Color.Yellow));
				r.Add(new ColoredRectangle(new Point(0.5f, 0.6f), Size.Half, Color.Blue));
			});
		}

		[VisualTest]
		public void RedBoxOverLaysBlueBox(Type resolver)
		{
			Start(resolver, (Renderer r) =>
			{
				var redBox = new ColoredRectangle(new Point(0.4f, 0.4f), Size.Half, Color.Red)
				{
					RenderLayer = 1
				};

				var blueBox = new ColoredRectangle(new Point(0.5f, 0.6f), Size.Half, Color.Blue)
				{
					RenderLayer = 2
				};

				r.Add(redBox);
				r.Add(blueBox);
				redBox.RenderLayer = 3;
			});
		}
	}
}