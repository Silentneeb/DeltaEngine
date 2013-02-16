﻿using System;
using DeltaEngine.Input.Windows;
using DeltaEngine.Platforms;
using DeltaEngine.Platforms.Tests;
using DeltaEngine.Rendering;
using NUnit.Framework;

namespace DeltaEngine.Input.Tests
{
	public class MouseHookTests
	{
		[Test]
		public void WindowsMouseHandleProcMessage()
		{
			IntPtr lParamHandle = GenerateMouseHookData(new[] { 0, 0, 0, 0, 0, 240 });
			WindowsMouse windowsMouse = GetMouse();
			windowsMouse.hook.Dispose();
			windowsMouse.hook.HandleProcMessage((IntPtr)MouseHook.WMMousewheel, lParamHandle, 0);
			windowsMouse.Run();

			Assert.AreEqual(2, windowsMouse.ScrollWheelValue);
		}

		[Test]
		public void WindowsMouseHandleProcMessageButton()
		{
			IntPtr lParamHandle = GenerateMouseHookData(new[] { 50, 3, 0, 0, 0, 0x0201 });
			WindowsMouse windowsMouse = GetMouse();
			windowsMouse.hook.Dispose();
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0201, lParamHandle, 0);
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0201, lParamHandle, 0);
			windowsMouse.Run();
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0202, lParamHandle, 0);
			windowsMouse.Run();

			Assert.AreEqual(State.Releasing, windowsMouse.LeftButton);
		}

		[Test]
		public void RunWithPressAndReleasesBetweenTicks()
		{
			IntPtr lParamHandle = GenerateMouseHookData(new[] { 50, 3, 0, 0, 0, 0x0201 });
			WindowsMouse windowsMouse = GetMouse();
			windowsMouse.hook.Dispose();
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0201, lParamHandle, 0);
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0201, lParamHandle, 0);
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0202, lParamHandle, 0);
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0201, lParamHandle, 0);
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0201, lParamHandle, 0);
			windowsMouse.hook.HandleProcMessage((IntPtr)0x0202, lParamHandle, 0);
			windowsMouse.Run();

			// No matter what happens in one frame, we need to go to Pressing first.
			Assert.AreEqual(State.Pressing, windowsMouse.LeftButton);
			windowsMouse.Run();
			Assert.AreEqual(State.Releasing, windowsMouse.LeftButton);
			windowsMouse.Run();
			Assert.AreEqual(State.Released, windowsMouse.LeftButton);
		}

		private IntPtr GenerateMouseHookData(int[] lParamData)
		{
			IntPtr lParamHandle;
			unsafe
			{
				fixed (int* ptr = &lParamData[0])
					lParamHandle = (IntPtr)ptr;
			}

			return lParamHandle;
		}

		private WindowsMouse GetMouse()
		{
			var resolver = new TestResolver();
			var window = resolver.Resolve<Window>();
			var screen = new ScreenSpace(window);
			var positionTranslater = new CursorPositionTranslater(window, screen);
			return new WindowsMouse(positionTranslater);
		}
	}
}