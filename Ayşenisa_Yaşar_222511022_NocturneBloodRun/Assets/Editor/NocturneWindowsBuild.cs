using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class NocturneWindowsBuild
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string DefaultRelativeOutput = "Builds/Windows/NocturneBloodRun.exe";
    private const string RequestFileRelativePath = "Builds/Windows/build_request.txt";
    private const string StatusFileRelativePath = "Builds/Windows/build_status.txt";

    [InitializeOnLoadMethod]
    private static void RegisterBuildRequestHandler()
    {
        EditorApplication.update -= ProcessBuildRequestIfNeeded;
        EditorApplication.update += ProcessBuildRequestIfNeeded;
    }

    [MenuItem("Tools/Nocturne Village/Build Windows Player")]
    public static void BuildWindowsPlayerMenu()
    {
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativeOutput);
        BuildWindowsPlayer(outputPath);
    }

    public static void BuildWindowsPlayerBatch()
    {
        string outputPath = ResolveOutputPathFromArgs();
        BuildWindowsPlayer(outputPath);
    }

    private static void ProcessBuildRequestIfNeeded()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying)
        {
            return;
        }

        string requestPath = Path.Combine(Directory.GetCurrentDirectory(), RequestFileRelativePath);
        if (!File.Exists(requestPath))
        {
            return;
        }

        string statusPath = Path.Combine(Directory.GetCurrentDirectory(), StatusFileRelativePath);
        string outputPath = File.ReadAllText(requestPath).Trim();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativeOutput);
        }

        File.Delete(requestPath);

        try
        {
            BuildWindowsPlayer(outputPath);
            File.WriteAllText(statusPath, $"SUCCESS|{Path.GetFullPath(outputPath)}");
        }
        catch (Exception exception)
        {
            File.WriteAllText(statusPath, $"FAIL|{exception}");
            Debug.LogException(exception);
        }
    }

    private static void BuildWindowsPlayer(string outputPath)
    {
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ScenePath)))
        {
            throw new BuildFailedException($"Scene not found: {ScenePath}");
        }

        string fullOutputPath = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new BuildFailedException("Output directory is invalid.");
        }

        Directory.CreateDirectory(outputDirectory);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = fullOutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Windows build failed. Result: {report.summary.result}, errors: {report.summary.totalErrors}, warnings: {report.summary.totalWarnings}");
        }

        Debug.Log($"[NocturneBuild] Windows build created at: {fullOutputPath}");
    }

    private static string ResolveOutputPathFromArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], "-customBuildPath", StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativeOutput);
    }
}
