﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeltaEngine.Datatypes;

namespace DeltaEngine.Entities
{
	/// <summary>
	/// Drawable components like Entity2D or Entity3D will interpolate between update ticks.
	/// NextUpdateStarted marks the beginning of an update tick to copy interpolatable data, it can
	/// also be used to check if any data has changed since last time and if something needs updating.
	/// </summary>
	public class DrawableEntity : Entity
	{
		/// <summary>
		/// By default all drawable entities are visible, but you can easily turn off drawing with Hide
		/// </summary>
		public DrawableEntity()
		{
			// ReSharper disable DoNotCallOverridableMethodsInConstructor
			Visibility = Visibility.Show;	
		}

		public virtual Visibility Visibility { get; set; }

		/// <summary>
		/// Each Entity2D uses a RenderLayer, which will determine the sorting for rendering.
		/// </summary>
		public int RenderLayer
		{
			get { return base.Contains<int>() ? base.Get<int>() : DefaultRenderLayer; }
			set
			{
				if (value == RenderLayer)
					return;
				Set(value);
				EntitiesRunner.Current.ChangeRenderLayer(this, drawBehaviors);
			}
		}

		public const int DefaultRenderLayer = 0;

		internal void InternalNextUpdateStarted()
		{
			NextUpdateStarted();
		}

		protected virtual void NextUpdateStarted()
		{
			for (int index = 0; index < lastTickLerpComponents.Count; index++)
				foreach (var current in components)
					if (current.GetType() == lastTickLerpComponents[index].GetType())
					{
						if (current is IList)
						{
							var genericListTypes = current.GetType().GetGenericArguments();
							if (genericListTypes.Length > 0)
							{
								var elementType = genericListTypes[0];
								lastTickLerpComponents[index] =
									Activator.CreateInstance((typeof(List<>).MakeGenericType(elementType)), current);
							}
							else if (current is Array)
							{
								var currentArray = current as Array;
								var elementType = current.GetType().GetElementType();
								var copiedArray = lastTickLerpComponents[index] as Array;
								if (copiedArray == null || copiedArray == currentArray ||
									copiedArray.Length != currentArray.Length)
									copiedArray = Array.CreateInstance(elementType, currentArray.Length);
								if (currentArray.Length > 0)
									Array.Copy(currentArray, copiedArray, currentArray.Length);
								lastTickLerpComponents[index] = copiedArray;
							}
						}
						else
							lastTickLerpComponents[index] = current;
						break;
					}
		}

		/// <summary>
		/// Each element can either be a Lerp, a List of Lerps or an array of Lerp objects.
		/// </summary>
		protected readonly List<object> lastTickLerpComponents = new List<object>();

		public override T Get<T>()
		{
			if (EntitiesRunner.Current.State == UpdateDrawState.Draw &&
				typeof(Lerp).IsAssignableFrom(typeof(T)))
				if (typeof(Lerp<T>).IsAssignableFrom(typeof(T)))
					foreach (T previous in lastTickLerpComponents.OfType<T>())
						return ((Lerp<T>)base.Get<T>()).Lerp(previous, EntitiesRunner.CurrentDrawInterpolation);
			return base.Get<T>();
		}

		public IEnumerable<T> GetInterpolatedList<T>() where T : Lerp
		{
			EntitiesRunner.Current.CheckIfInDrawState();
			foreach (var list in lastTickLerpComponents.OfType<IEnumerable<T>>())
				if (list.GetType().IsGenericType && list.GetType().GetGenericArguments()[0] == typeof(T))
				{
					foreach (var current in components.OfType<IEnumerable<T>>())
					{
						var ret = new List<T>(list);
						int index = 0;
						foreach (var currentItem in current)
						{
							ret[index] = ((Lerp<T>)ret[index]).Lerp(currentItem,
								EntitiesRunner.CurrentDrawInterpolation);
							index++;
						}
						return ret;
					}
				}
			throw new ListWithLerpElementsForInterpolationWasNotFound(typeof(T));
		}

		private class ListWithLerpElementsForInterpolationWasNotFound : Exception
		{
			public ListWithLerpElementsForInterpolationWasNotFound(Type type)
				: base(type.ToString()) {}
		}

		public T[] GetInterpolatedArray<T>(int arrayCopyLimit = -1) where T : Lerp
		{
			EntitiesRunner.Current.CheckIfInDrawState();
			foreach (var array in lastTickLerpComponents.OfType<T[]>())
				if (array.GetType().GetElementType() == typeof(T))
				{
					foreach (var current in components.OfType<T[]>())
					{
						var length = Math.Min(array.Length, current.Length);
						if (arrayCopyLimit > 0 && length > arrayCopyLimit)
							length = arrayCopyLimit;
						var ret = new T[length];
						for (int index = 0; index < length; index++)
							ret[index] = ((Lerp<T>)array[index]).Lerp(current[index],
								EntitiesRunner.CurrentDrawInterpolation);
						return ret;
					}
				}
			throw new ArrayWithLerpElementsForInterpolationWasNotFound(typeof(T));
		}

		private class ArrayWithLerpElementsForInterpolationWasNotFound : Exception
		{
			public ArrayWithLerpElementsForInterpolationWasNotFound(Type type)
				: base(type.ToString()) { }
		}

		public override Entity Add<T>(T component)
		{
			base.Add(component);
			if (IsLerpableType(component))
				lastTickLerpComponents.Add(component);
			return this;
		}

		private static bool IsLerpableType<T>(T component)
		{
			var list = component as IList;
			return component is Lerp || component is float ||
				list != null && list.Count > 0 && IsLerpableType(list[0]);
		}

		public override void Set<T>(T component)
		{
			base.Set(component);
			if (!IsLerpableType(component))
				return;
			if (!lastTickLerpComponents.OfType<T>().Any())
				lastTickLerpComponents.Add(component);
		}

		public void OnDraw<T>() where T : class, DrawBehavior
		{
			var behavior = EntitiesRunner.Current.GetDrawBehavior<T>();
			if (drawBehaviors.Contains(behavior))
				return;
			drawBehaviors.Add(behavior);
			EntitiesRunner.Current.AddToDrawBehaviorList(this, behavior);
		}

		internal readonly List<DrawBehavior> drawBehaviors = new List<DrawBehavior>();

		public override bool IsActive
		{
			get { return base.IsActive; }
			set
			{
				if (base.IsActive == value)
					return;
				base.IsActive = value;
				if (value)
					foreach (DrawBehavior behavior in drawBehaviors)
						EntitiesRunner.Current.AddToDrawBehaviorList(this, behavior);
			}
		}
	}
}