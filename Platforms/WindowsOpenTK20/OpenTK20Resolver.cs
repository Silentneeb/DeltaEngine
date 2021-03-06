﻿using System;
using System.IO;
using System.Linq;
using DeltaEngine.Graphics.OpenTK20;
using DeltaEngine.Input.Windows;
using DeltaEngine.Multimedia.OpenTK;
using DeltaEngine.Platforms.Windows;
using DeltaEngine.Content.Xml;
using DeltaEngine.Graphics;
using DeltaEngine.Rendering2D;
using DeltaEngine.Core;
using DeltaEngine.Extensions;

namespace DeltaEngine.Platforms
{
	internal class OpenTK20Resolver : AppRunner
	{
		private readonly string[] nativeDllsNeeded = { "openal32.dll", "wrap_oal.dll" };

		public OpenTK20Resolver()
		{
			try
			{
				InitializeOpenTK();
			}
			catch (Exception exception)
			{
				Logger.Error(exception);
				if (StackTraceExtensions.IsStartedFromNunitConsole())
					throw;
				DisplayMessageBoxAndCloseApp("Fatal OpenTK Initialization Error", exception);
			}
		}

		private void InitializeOpenTK()
		{
			MakeSureOpenALDllsAreAvailable();
			RegisterCommonEngineSingletons();
			RegisterSingleton<FormsWindow>();
			RegisterSingleton<WindowsSystemInformation>();
			RegisterSingleton<OpenTK20Device>();
			RegisterSingleton<Drawing>();
			RegisterSingleton<BatchRenderer>();
			RegisterSingleton<OpenTK20ScreenshotCapturer>();
			RegisterSingleton<OpenTKSoundDevice>();
			RegisterSingleton<WindowsMouse>();
			RegisterSingleton<WindowsKeyboard>();
			RegisterSingleton<WindowsTouch>();
			RegisterSingleton<WindowsGamePad>();
			RegisterSingleton<CursorPositionTranslater>();
			Register<InputCommands>();
			if (IsAlreadyInitialized)
				throw new UnableToRegisterMoreTypesAppAlreadyStarted();
		}

		protected override void RegisterMediaTypes()
		{
			base.RegisterMediaTypes();
			Register<OpenTK20Image>();
			Register<OpenTK20Shader>();
			Register<OpenTK20Geometry>();
			Register<OpenTKSound>();
			Register<OpenTKMusic>();
			Register<XmlContent>();
		}

		private void MakeSureOpenALDllsAreAvailable()
		{
			if (AreNativeDllsMissing())
				TryCopyNativeDlls();
		}

		private bool AreNativeDllsMissing()
		{
			return nativeDllsNeeded.Any(nativeDll => !File.Exists(nativeDll));
		}

		private void TryCopyNativeDlls()
		{
			if (TryCopyNativeDllsFromNuGetPackage())
				return;
			if (TryCopyNativeDllsFromDeltaEnginePath())
				return;
			throw new OpenTK20Resolver.FailedToCopyNativeOpenALDllFiles("OpenAL dlls not found inside the application " + "output directory nor inside the %DeltaEnginePath% environment variable. Make sure it's " + "set and containing the required files: " + string.Join(",", nativeDllsNeeded));
		}

		private bool TryCopyNativeDllsFromNuGetPackage()
		{
			var nuGetPackagesPath = FindNuGetPackagesPath();
			string nativeBinariesPath = Path.Combine(nuGetPackagesPath, "packages", "OpenTKWithOpenAL.1.1.1161.61462", "NativeBinaries", "x86");
			if (!Directory.Exists(nativeBinariesPath))
				return false;
			CopyNativeDllsFromPath(nativeBinariesPath);
			return true;
		}

		private static string FindNuGetPackagesPath()
		{
			int MaxPathLength = 18;
			var path = Path.Combine("..", "..");
			while (!IsPackagesDirectory(path))
			{
				path = Path.Combine(path, "..");
				if (path.Length > MaxPathLength)
					break;
			}
			return path;
		}

		private void CopyNativeDllsFromPath(string nativeBinariesPath)
		{
			foreach (var nativeDll in nativeDllsNeeded)
				File.Copy(Path.Combine(nativeBinariesPath, nativeDll), nativeDll, true);
		}

		private static bool IsPackagesDirectory(string path)
		{
			return Directory.Exists(Path.Combine(path, "packages"));
		}

		private bool TryCopyNativeDllsFromDeltaEnginePath()
		{
			string enginePath = Environment.GetEnvironmentVariable("DeltaEnginePath");
			if (enginePath == null || !Directory.Exists(enginePath))
				return false;
			CopyNativeDllsFromPath(Path.Combine(enginePath, "OpenTK"));
			return true;
		}

		private class FailedToCopyNativeOpenALDllFiles : Exception
		{
			public FailedToCopyNativeOpenALDllFiles(string message)
				: base(message)
			{
			}
		}
	}
}