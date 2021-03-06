﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DeltaEngine.Content;
using DeltaEngine.Content.Online;
using DeltaEngine.Content.Xml;
using DeltaEngine.Core;
using DeltaEngine.Entities;
using DeltaEngine.Extensions;
using DeltaEngine.Graphics;
using DeltaEngine.Logging;
using DeltaEngine.Networking;
using DeltaEngine.Networking.Tcp;
using DeltaEngine.Rendering2D;
using DeltaEngine.ScreenSpaces;

namespace DeltaEngine.Platforms
{
	/// <summary>
	/// Starts an application on demand by registering, resolving and running it (via EntitiesRunner).
	/// Most of the registration is not used when running with MockResolver, replaces lots of classes.
	/// </summary>
	public abstract class AppRunner : ApproveFirstFrameScreenshot
	{
		//ncrunch: no coverage start (for performance reasons)
		protected void RegisterCommonEngineSingletons()
		{
			LoadFileSettingsAndCommands();
			CreateOnlineService();
			CreateDefaultLoggers();
			CreateConsoleCommandResolver();
			CreateContentLoader();
			CreateEntitySystem();
			RegisterMediaTypes();
			CreateNetworking();
			CreateScreenSpacesAndCameraResolvers();
		}

		private void LoadFileSettingsAndCommands()
		{
			instancesToDispose.Add(settings = new FileSettings());
			RegisterInstance(settings);
			ContentIsReady += () => ContentLoader.Load<InputCommands>("DefaultCommands");
			ContentIsReady += () => Window.ViewportPixelSize = settings.Resolution;
		}

		protected Settings settings;
		protected internal static event Action ContentIsReady;

		private void CreateOnlineService()
		{
			onlineService = new OnlineServiceConnection(settings, OnTimeout);
			onlineService.ServerErrorHappened = OnError;
			onlineService.ContentReady = OnReady;
			onlineService.ContentReceived = OnContentReceived;
			RegisterInstance(onlineService);
		}

		private OnlineServiceConnection onlineService;

		private void CreateDefaultLoggers()
		{
			instancesToDispose.Add(new TextFileLogger());
			instancesToDispose.Add(new NetworkLogger(onlineService));
			if (ExceptionExtensions.IsDebugMode)
				instancesToDispose.Add(new ConsoleLogger());
		}

		protected readonly List<IDisposable> instancesToDispose = new List<IDisposable>();

		/// <summary>
		/// Console commands are only available when not starting from NCrunch. Initialization is slow.
		/// </summary>
		private void CreateConsoleCommandResolver()
		{
			ConsoleCommands.resolver = new ConsoleCommandResolver(this);
		}

		private void CreateContentLoader()
		{
			if (ContentLoader.Type == null)
				ContentLoader.Use<DeveloperOnlineContentLoader>();
			ContentLoader.resolver = new AutofacContentLoaderResolver(this);
		}

		internal enum ExitCode
		{
			InitializationError = -1,
			UpdateAndDrawTickFailed = -2,
			ContentMissingAndApiKeyNotSet = -3
		}

		private void OnConnectionError(string errorMessage)
		{
			Logger.Warning(errorMessage);
			connectionError = errorMessage;
		}

		private string connectionError;

		private void OnTimeout()
		{
			OnConnectionError("Content Service Connection " + settings.OnlineServiceIp + ":" +
				settings.OnlineServicePort + " timed out.");
		}

		private void OnError(string serverMessage)
		{
			OnConnectionError("Server Error: " + serverMessage);
		}

		private void OnReady()
		{
			onlineServiceReadyReceived = true;
		}

		private bool onlineServiceReadyReceived;

		public void OnContentReceived()
		{
			if (timeout < NextMessageTimeoutMs)
				timeout = NextMessageTimeoutMs;
		}

		private int timeout;
		private const int NextMessageTimeoutMs = 4000;

		private void CreateEntitySystem()
		{
			instancesToDispose.Add(
				entities = new EntitiesRunner(new AutofacHandlerResolver(this), settings));
			RegisterInstance(entities);
		}

		protected EntitiesRunner entities;

		protected virtual void RegisterMediaTypes()
		{
			Register<Material>();
			Register<ImageAnimation>();
			Register<SpriteSheetAnimation>();
		}

		internal override void MakeSureContentManagerIsReady()
		{
			if (alreadyCheckedContentManagerReady)
				return;
			alreadyCheckedContentManagerReady = true;
			if (ContentLoader.Type == typeof(DeveloperOnlineContentLoader) && !IsEditorContentLoader())
				WaitUntilContentFromOnlineServiceIsReady();
			if (!ContentLoader.HasValidContentForStartup())
			{
				if (StackTraceExtensions.IsStartedFromNunitConsole())
					throw new Exception("No local content available - Unable to continue: " + connectionError);
				Window.ShowMessageBox("No local content available", "Unable to continue: " +
					(connectionError ?? "No content found, please put content in the Content folder"),
					new[] { "OK" });
				Environment.Exit((int)ExitCode.ContentMissingAndApiKeyNotSet);
			}
			ContentIsReady();
		}

		private bool alreadyCheckedContentManagerReady;

		private static bool IsEditorContentLoader()
		{
			return ContentLoader.Type.FullName == "DeltaEngine.Editor.EditorContentLoader";
		}

		private void WaitUntilContentFromOnlineServiceIsReady()
		{
			if (!ContentLoader.HasValidContentForStartup())
				Logger.Info("No content available. Waiting until OnlineService sends it to us ...");
			timeout = InitialTimeoutMs;
			while (String.IsNullOrEmpty(connectionError) && !onlineServiceReadyReceived &&
				ContentLoader.Type == typeof(DeveloperOnlineContentLoader) && timeout > 0)
			{
				Thread.Sleep(10);
				timeout -= 10;
			}
			if (timeout <= 0)
				Logger.Warning(
					"Content download timeout reached, continuing app (content might be incomplete)");
		}

		private const int InitialTimeoutMs = 5000;

		private void CreateNetworking()
		{
			Register<TcpServer>();
			Register<TcpSocket>();
			instancesToDispose.Add(new Messaging(new AutofacNetworkResolver(this)));
		}

		private void CreateScreenSpacesAndCameraResolvers()
		{
			Register<QuadraticScreenSpace>();
			Register<RelativeScreenSpace>();
			Register<PixelScreenSpace>();
			Register<Camera2DScreenSpace>();
			ScreenSpace.resolver = new AutofacScreenSpaceResolver(this);
		}

		public virtual void Run()
		{
			do
				RunTick();
			while (!Window.IsClosing);
		}

		internal void RunTick()
		{
			Device.Clear();
			GlobalTime.Current.Update();
			UpdateAndDrawAllEntities();
			ExecuteTestCodeAndMakeScreenshotAfterFirstFrame();
			Device.Present();
			Window.Present();
			CheckIfAppShouldSleepBecauseOfLimitFramerate();
		}

		private void CheckIfAppShouldSleepBecauseOfLimitFramerate()
		{
			if (settings.LimitFramerate <= 0 || GlobalTime.Current.Milliseconds < 1000)
				return;
			if (sleepDoseEachFrame < 0 && GlobalTime.Current.Fps > settings.LimitFramerate)
				sleepDoseEachFrame = 1.0f / settings.LimitFramerate - 1.0f / GlobalTime.Current.Fps;
			if (sleepDoseEachFrame < 0)
				return;
			if (Time.CheckEvery(2.0f) &&
				Math.Abs(GlobalTime.Current.Fps - settings.LimitFramerate) > settings.LimitFramerate / 20)
				sleepDoseEachFrame += (1.0f / settings.LimitFramerate - 1.0f / GlobalTime.Current.Fps) / 2;
			accumulatedSleepMs += sleepDoseEachFrame * 1000.0f;
			if ((int)accumulatedSleepMs <= 0)
				return;
			Thread.Sleep((int)accumulatedSleepMs);
			accumulatedSleepMs -= (int)accumulatedSleepMs;
		}

		private float sleepDoseEachFrame = -1.0f;
		private float accumulatedSleepMs;

		private Device Device
		{
			get { return cachedDevice ?? (cachedDevice = Resolve<Device>()); }
		}
		private Device cachedDevice;

		private Window Window
		{
			get { return cachedWindow ?? (cachedWindow = Resolve<Window>()); }
		}
		private Window cachedWindow;

		/// <summary>
		/// When debugging or testing crash where the actual exception happens, not here.
		/// </summary>
		private void UpdateAndDrawAllEntities()
		{
			Drawing.NumberOfDynamicVerticesDrawnThisFrame = 0;
			Drawing.NumberOfDynamicDrawCallsThisFrame = 0;
			if (Debugger.IsAttached || StackTraceExtensions.StartedFromNCrunch)
				entities.UpdateAndDrawAllEntities(DrawEverythingInCurrentLayer);
			else
				TryUpdateAndDrawAllEntities();
		}

		private void DrawEverythingInCurrentLayer()
		{
			BatchRenderer.DrawAndResetBatches();
			Drawing.DrawEverythingInCurrentLayer();
		}

		private BatchRenderer BatchRenderer
		{
			get { return cachedBatchRenderer ?? (cachedBatchRenderer = Resolve<BatchRenderer>()); }
		}
		private BatchRenderer cachedBatchRenderer;

		private Drawing Drawing
		{
			get { return cachedDrawing ?? (cachedDrawing = Resolve<Drawing>()); }
		}
		private Drawing cachedDrawing;

		private void TryUpdateAndDrawAllEntities()
		{
			try
			{
				entities.UpdateAndDrawAllEntities(DrawEverythingInCurrentLayer);
			}
			catch (Exception exception)
			{
				Logger.Error(exception);
				if (exception.IsWeak())
					return;
				if (StackTraceExtensions.IsStartedFromNunitConsole())
					throw;
				DisplayMessageBoxAndCloseApp("Fatal Runtime Error", exception);
			}
		}

		internal void DisplayMessageBoxAndCloseApp(string title, Exception exception)
		{
			if (IsShowingMessageBoxClosedWithIgnore(title, exception))
				return;
			Dispose();
			if (!StackTraceExtensions.StartedFromNCrunch)
				Environment.Exit((int)ExitCode.UpdateAndDrawTickFailed);
		}

		private bool IsShowingMessageBoxClosedWithIgnore(string title, Exception ex)
		{
			Window.CopyTextToClipboard(ex.ToString());
			string message = "Unable to continue: " + ex + ErrorWasCopiedToClipboardMessage +
				ClickIgnoreToContinue;
			return Window.ShowMessageBox(title, message, new[] { "Abort", "Ignore" }) == "Ignore";
		}

		public override void Dispose()
		{
			base.Dispose();
			foreach (var instance in instancesToDispose)
				instance.Dispose();
			instancesToDispose.Clear();
			ContentLoader.DisposeIfInitialized();
			if (ScreenSpace.IsInitialized)
				ScreenSpace.Current.Dispose();
		}
	}
}