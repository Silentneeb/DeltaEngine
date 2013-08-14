﻿using System;
using System.Collections.Generic;
using System.IO;
using DeltaEngine.Datatypes;

namespace DeltaEngine.Content
{
	/// <summary>
	/// Holds frames inside a single image as opposed to ImageAnimation for rendering animated sprites
	/// </summary>
	public class SpriteSheetAnimation : ContentData
	{
		public SpriteSheetAnimation(string contentName)
			: base(contentName) {}

		public SpriteSheetAnimation(Image imageName, float duration, Size subImageSize)
			: base("<GeneratedSpriteSheetAnimation>")
		{
			Image = imageName;
			DefaultDuration = duration;
			SubImageSize = subImageSize;
			CreateUVs();
		}

		public Image Image { get; private set; }
		public float DefaultDuration { get; private set; }
		public Size SubImageSize { get; private set; }

		private void CreateUVs()
		{
			UVs = new List<Rectangle>();
			for (int y = 0; y < Image.PixelSize.Height / SubImageSize.Height; y++)
				for (int x = 0; x < Image.PixelSize.Width / SubImageSize.Width; x++)
					UVs.Add(Rectangle.BuildUvRectangle(CalculatePixelRect(x, y), Image.PixelSize));
		}

		public List<Rectangle> UVs { get; private set; }

		private Rectangle CalculatePixelRect(int x, int y)
		{
			return new Rectangle(x * SubImageSize.Width, y * SubImageSize.Height, SubImageSize.Width,
				SubImageSize.Height);
		}

		protected override void LoadData(Stream fileData)
		{
			var imageName = MetaData.Get("ImageName", "");
			if (string.IsNullOrEmpty(imageName))
				throw new NeedValidImageName();
			Image = ContentLoader.Load<Image>(imageName);
			DefaultDuration = MetaData.Get("DefaultDuration", 0.0f);
			SubImageSize = MetaData.Get("SubImageSize", Size.Zero);
			if (SubImageSize == Size.Zero)
				throw new NeedValidSubImageSize();
			CreateUVs();
		}

		private class NeedValidImageName : Exception { }
		private class NeedValidSubImageSize : Exception { }

		protected override void DisposeData()
		{
			if (IsDisposed)
				Image.Dispose();
		}

		public Material CreateMaterial(Shader shader)
		{
			return new Material(shader, null) { SpriteSheet = this };
		}
	}
}