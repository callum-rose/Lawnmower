using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lawnmower.Editor.Build
{
	/// <summary>
	/// iOS player settings and the Xcode-project build used by tools/build_and_install.sh.
	/// </summary>
	public static class IOSBuild
	{
		public const string ProductName = "Bunny Heaven";
		public const string CompanyName = "Callum Rose";
		public const string BundleId = "com.callumrose.bunnyheaven";
		public const string TeamId = "7R68U6FFLK";
		public const string MinimumIOSVersion = "15.0";
		public const string DefaultOutputDir = "Builds/iOS";

		private const string OutputArg = "-buildOutput";
		private const string DevelopmentArg = "-developmentBuild";

		[MenuItem("Bunny Heaven/Build/Apply iOS Player Settings")]
		public static void ApplyPlayerSettings()
		{
			PlayerSettings.productName = ProductName;
			PlayerSettings.companyName = CompanyName;
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);

			PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
			PlayerSettings.iOS.appleEnableAutomaticSigning = true;
			PlayerSettings.iOS.targetOSVersionString = MinimumIOSVersion;
			PlayerSettings.enableFrameTimingStats = true;

			PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
			PlayerSettings.allowedAutorotateToPortrait = false;
			PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
			PlayerSettings.allowedAutorotateToLandscapeLeft = false;
			PlayerSettings.allowedAutorotateToLandscapeRight = false;

			AssetDatabase.SaveAssets();
		}

		[MenuItem("Bunny Heaven/Build/Build iOS Xcode Project")]
		public static void BuildIOSMenu() => BuildIOS();

		/// <summary>
		/// Builds the enabled scenes into an Xcode project. Throws (and exits 1 in batch mode) on any failure.
		/// </summary>
		public static string BuildIOS()
		{
			try
			{
				return BuildIOSInternal();
			}
			catch (Exception e)
			{
				Debug.LogError($"[IOSBuild] FAILED: {e.Message}");
				if (Application.isBatchMode)
				{
					EditorApplication.Exit(1);
				}

				throw;
			}
		}

		/// <summary>
		/// For a live editor, whose remote calls time out long before a build finishes: schedules
		/// <see cref="BuildIOS"/> for the next editor update and writes "running", then "Succeeded ..."
		/// or "FAILED ..." to <paramref name="statusFile"/> (relative to the project root).
		/// </summary>
		public static string BuildIOSDeferred(string statusFile)
		{
			string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), statusFile));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, "running");

			// delayCall is not pumped for calls arriving over the CLI, so use a one-shot update instead.
			EditorApplication.CallbackFunction runOnce = null;
			runOnce = () =>
			{
				EditorApplication.update -= runOnce;
				string status;
				try
				{
					status = BuildIOS();
				}
				catch (Exception e)
				{
					status = $"[IOSBuild] FAILED: {e.Message}";
				}

				File.WriteAllText(path, status);
			};
			EditorApplication.update += runOnce;

			return $"[IOSBuild] Scheduled; status in {path}";
		}

		private static string BuildIOSInternal()
		{
			if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
			{
				throw new BuildFailedException("iOS Build Support module is not installed for this editor.");
			}

			ApplyPlayerSettings();

			string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
			if (scenes.Length == 0)
			{
				throw new BuildFailedException("No enabled scenes in Build Settings.");
			}

			string projectRoot = Path.GetDirectoryName(Application.dataPath);
			string outputDir = Path.GetFullPath(Path.Combine(projectRoot, GetOutputArg() ?? DefaultOutputDir));

			BuildPlayerOptions options = new BuildPlayerOptions
			{
				scenes = scenes,
				locationPathName = outputDir,
				target = BuildTarget.iOS,
				targetGroup = BuildTargetGroup.iOS,
				options = HasArg(DevelopmentArg) ? BuildOptions.Development : BuildOptions.None
			};

			BuildReport report = BuildPipeline.BuildPlayer(options);
			BuildSummary summary = report.summary;

			if (summary.result != BuildResult.Succeeded)
			{
				throw new BuildFailedException(
					$"iOS build {summary.result} with {summary.totalErrors} error(s). See the Editor log.");
			}

			string xcodeProject = Path.Combine(outputDir, "Unity-iPhone.xcodeproj");
			if (!Directory.Exists(xcodeProject))
			{
				throw new BuildFailedException($"Build reported success but {xcodeProject} is missing.");
			}

			string message = $"[IOSBuild] Succeeded in {summary.totalTime:mm\\:ss}: {xcodeProject} ({string.Join(", ", scenes)})";
			Debug.Log(message);
			return message;
		}

		private static string GetOutputArg()
		{
			string[] args = Environment.GetCommandLineArgs();
			int i = Array.IndexOf(args, OutputArg);
			return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
		}

		private static bool HasArg(string name) => Environment.GetCommandLineArgs().Contains(name);
	}
}
