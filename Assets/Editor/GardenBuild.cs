// using System;
// using UnityEditor;
// using UnityEditor.Build.Reporting;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
//
// namespace GardenSnake.Editor
// {
//     /// <summary>
//     /// The one build path. It writes the project's shipping configuration into Project Settings
//     /// first, so a build from here, from the toolbar, or from the harness all produce the same
//     /// player. It owns nothing: the settings live in ProjectSettings and are committed with the
//     /// repository.
//     /// </summary>
//     public static class GardenBuild
//     {
//         public const string ScenePath = "Assets/Scenes/GardenSnake.unity";
//         private const string OutputPath = "Builds/WebGL";
//
//         [MenuItem("Garden Snake/Build WebGL")]
//         public static void BuildWebGL()
//         {
//             if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
//                 throw new InvalidOperationException("Install Web Build Support for Unity 6000.6.0f1, then restart the Editor.");
//             ApplyProjectSettings();
//             var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
//             {
//                 scenes = new[] { ScenePath },
//                 locationPathName = OutputPath,
//                 target = BuildTarget.WebGL,
//                 options = BuildOptions.None
//             });
//             if (report.summary.result != BuildResult.Succeeded)
//                 throw new InvalidOperationException("Web build failed: " + report.summary.result);
//             Debug.Log("[GardenSnake] Web build succeeded: " + report.summary.totalSize + " bytes");
//         }
//
//         /// <summary>
//         /// The window the game is framed for lives here, in the player's resolution: the camera is
//         /// authored to fill it, so changing these changes what the player sees at the edges.
//         /// </summary>
//         [MenuItem("Garden Snake/Apply project settings")]
//         public static void ApplyProjectSettings()
//         {
//             PlayerSettings.companyName = "Pikoya Demo";
//             PlayerSettings.productName = "Garden Snake";
//             PlayerSettings.runInBackground = true;
//             PlayerSettings.defaultWebScreenWidth = 1600;
//             PlayerSettings.defaultWebScreenHeight = 900;
//
//             // Uncompressed output so the folder can be served by any static host without
//             // custom Content-Encoding headers.
//             PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
//             PlayerSettings.WebGL.template = "PROJECT:Garden";
//             PlayerSettings.WebGL.dataCaching = true;
//             PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
//             PlayerSettings.WebGL.showDiagnostics = false;
//
//             // A development player stamps a watermark over the HUD and slows the build down.
//             EditorUserBuildSettings.development = false;
//             EditorUserBuildSettings.allowDebugging = false;
//
//             SelectReleaseProfile();
//             ApplyPipelineSettings();
//             AssetDatabase.SaveAssets();
//             Debug.Log("[GardenSnake] Project settings applied.");
//         }
//
//         /// <summary>
//         /// Unity 6 builds through Build Profiles, and the development profile is what stamps the
//         /// watermark over the HUD. Build And Run should ship the release profile by default.
//         /// </summary>
//         private static void SelectReleaseProfile()
//         {
//             foreach (string guid in AssetDatabase.FindAssets("t:BuildProfile"))
//             {
//                 string path = AssetDatabase.GUIDToAssetPath(guid);
//                 if (!path.Contains("Web - Desktop - Release")) continue;
//                 var profile = AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(path);
//                 if (profile == null) continue;
//                 UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(profile);
//                 Debug.Log("[GardenSnake] Active build profile: " + profile.name);
//                 return;
//             }
//         }
//
//         /// <summary>
//         /// URP's automatic upscaling filter asks for the FSR shader, which is stripped from a Web
//         /// build; when it goes missing URP skips the whole post-processing stack, so the browser
//         /// loses bloom, vignette and grading. Pinning the filter keeps the stack running.
//         /// </summary>
//         private static void ApplyPipelineSettings()
//         {
//             foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
//             {
//                 string path = AssetDatabase.GUIDToAssetPath(guid);
//                 var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
//                 if (pipeline == null) continue;
//                 pipeline.upscalingFilter = UpscalingFilterSelection.Linear;
//                 pipeline.renderScale = 1f;
//                 EditorUtility.SetDirty(pipeline);
//             }
//
//             var current = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
//             if (current != null) Debug.Log("[GardenSnake] Pipeline: " + current.name +
//                                           ", upscaling " + current.upscalingFilter);
//         }
//     }
// }
