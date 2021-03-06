﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using DeltaEngine.Datatypes;
using DeltaEngine.Extensions;
using DeltaEngine.Rendering2D.Particles;

namespace DeltaEngine.Editor.Converters
{
	public class ValueRangeGraphStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || !(value is RangeGraph<ValueRange>))
				return "";
			return (value as RangeGraph<ValueRange>).ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			var stringValue = value is string ? value as string : null;
			if (string.IsNullOrEmpty(stringValue))
				return null;
			var stringPartitions =
				stringValue.SplitAndTrim(new[] { '(', '{', '[', ',', ' ', ']', '}', ')' });
			if (stringPartitions.Length % 2 != 0 || stringPartitions.Length < 4)
				return null;
			return FillRangeFromStringPartitions(stringPartitions);
		}

		private static RangeGraph<ValueRange> FillRangeFromStringPartitions(string[] partitions)
		{
			var valueRanges = new List<ValueRange>();
			for (int i = 0; i < partitions.Length - 1; i += 2)
			{
				if (!IsInputParsable(partitions[i]) || !IsInputParsable(partitions[i + 1]))
					return null;
				try
				{
					valueRanges.Add(new ValueRange(float.Parse(partitions[i], CultureInfo.InvariantCulture),
						float.Parse(partitions[i + 1], CultureInfo.InvariantCulture)));
				}
				catch
				{
					return null;
				}
			}
			return new RangeGraph<ValueRange>(valueRanges);
		}

		private static bool IsInputParsable(string input)
		{
			return !(input == "-0" || input.EndsWith(".") || input.EndsWith("0") && input.Contains("."));
		}
	}
}