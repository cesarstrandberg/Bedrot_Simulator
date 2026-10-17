using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click release builds, zipped and ready to upload to itch.io.
// Windows output: Builds/Windows/ (the game) and Builds/BedrotSimulator_Windows.zip (upload this).
// WebGL output:   Builds/WebGL/ (the game) and Builds/BedrotSimulator_WebGL.zip (upload this,
//                 tick "This file will be played in the browser" on itch.io).
public static class ItchBuild
{
    const string WebGLBuildDir = "Builds/WebGL";
    const string WebGLZipPath = "Builds/BedrotSimulator_WebGL.zip";

    const string BuildDir = "Builds/Windows";
    const string ZipPath = "Builds/BedrotSimulator_Windows.zip";
    const string ExeName = "Bedrot Simulator.exe";

    [MenuItem("Build/Build Windows Zip for itch.io")]
    public static void BuildWindowsZip()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (Directory.Exists(BuildDir))
            Directory.Delete(BuildDir, true);
        Directory.CreateDirectory(BuildDir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(BuildDir, ExeName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build failed",
                "The build failed. Check the Console for errors.", "OK");
            return;
        }

        // Debug-symbol folders Unity drops next to the game; not meant to be shipped.
        foreach (string dir in Directory.GetDirectories(BuildDir, "*DoNotShip*"))
            Directory.Delete(dir, true);

        if (File.Exists(ZipPath))
            File.Delete(ZipPath);
        ZipFile.CreateFromDirectory(BuildDir, ZipPath, System.IO.Compression.CompressionLevel.Optimal, false);

        long sizeMb = new FileInfo(ZipPath).Length / (1024 * 1024);
        Debug.Log($"[ItchBuild] Done: {Path.GetFullPath(ZipPath)} ({sizeMb} MB)");
        EditorUtility.RevealInFinder(ZipPath);
    }

    [MenuItem("Build/Build WebGL Zip for itch.io")]
    public static void BuildWebGLZip()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // Gzip + decompression fallback: works on any static host (itch.io included) without
        // needing the server to send Content-Encoding headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        CapWebGLTextureSizes();

        if (Directory.Exists(WebGLBuildDir))
            Directory.Delete(WebGLBuildDir, true);
        Directory.CreateDirectory(WebGLBuildDir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = WebGLBuildDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            else
                EditorUtility.DisplayDialog("Build failed",
                    "The WebGL build failed. Check the Console for errors.", "OK");
            return;
        }

        if (File.Exists(WebGLZipPath))
            File.Delete(WebGLZipPath);
        ZipFile.CreateFromDirectory(WebGLBuildDir, WebGLZipPath, System.IO.Compression.CompressionLevel.Optimal, false);

        long sizeMb = new FileInfo(WebGLZipPath).Length / (1024 * 1024);
        Debug.Log($"[ItchBuild] Done: {Path.GetFullPath(WebGLZipPath)} ({sizeMb} MB)");
        if (!Application.isBatchMode)
            EditorUtility.RevealInFinder(WebGLZipPath);
    }

    // itch.io rejects any single file over 200 MB inside an HTML5 zip, and WebGL.data blows past
    // that with full-res textures. Caps every texture's WebGL-only override at this size; the
    // Windows build keeps the originals. Already-smaller overrides are left alone.
    const int WebGLMaxTextureSize = 1024;

    [MenuItem("Build/Cap WebGL Texture Sizes")]
    public static void CapWebGLTextureSizes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int changed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (importer == null)
                    continue;

                TextureImporterPlatformSettings webgl = importer.GetPlatformTextureSettings("WebGL");
                if (webgl.overridden && webgl.maxTextureSize <= WebGLMaxTextureSize)
                    continue;

                webgl.overridden = true;
                webgl.maxTextureSize = WebGLMaxTextureSize;
                importer.SetPlatformTextureSettings(webgl);
                importer.SaveAndReimport();
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[ItchBuild] Capped {changed} textures to {WebGLMaxTextureSize}px for WebGL.");
    }
}
