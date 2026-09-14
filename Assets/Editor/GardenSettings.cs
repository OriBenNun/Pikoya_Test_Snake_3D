using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GardenSnake.Editor
{
    /// <summary>
    /// Writes the project's shipping configuration into Project Settings, so every build path
    /// produces the same player - including Build And Run straight from the Unity toolbar. The
    /// build menu item calls this too, but it deliberately owns nothing: the settings live in
    /// ProjectSettings and are committed with the repository.
    /// </summary>
    public static class GardenSettings
    {
        [MenuItem("Garden Snake/Apply project settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Pikoya Demo";
            PlayerSettings.productName = "Garden Snake";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1600;
            PlayerSettings.defaultWebScreenHeight = 900;

            // Uncompressed output so the folder can be served by any static host without
            // custom Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "PROJECT:Garden";
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.WebGL.showDiagnostics = false;

            // A development player stamps a watermark over the HUD and slows the build down.
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            SelectReleaseProfile();
            ApplyPipelineSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[GardenSnake] Project settings applied.");
        }

        /// <summary>
        /// Unity 6 builds through Build Profiles, and the development profile is what stamps the
        /// watermark over the HUD. Build And Run should ship the release profile by default.
        /// </summary>
        private static void SelectReleaseProfile()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:BuildProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("Web - Desktop - Release")) continue;
                var profile = AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(path);
                if (profile == null) continue;
                UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(profile);
                Debug.Log("[GardenSnake] Active build profile: " + profile.name);
                return;
            }
        }

        /// <summary>
        /// URP's automatic upscaling filter asks for the FSR shader, which is stripped from a Web
        /// build; when it goes missing URP skips the whole post-processing stack, so the browser
        /// loses bloom, vignette and grading. Pinning the filter keeps the stack running.
        /// </summary>
        private static void ApplyPipelineSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (pipeline == null) continue;
                pipeline.upscalingFilter = UpscalingFilterSelection.Linear;
                pipeline.renderScale = 1f;
                EditorUtility.SetDirty(pipeline);
            }

            var current = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (current != null) Debug.Log("[GardenSnake] Pipeline: " + current.name +
                                          ", upscaling " + current.upscalingFilter);
        }
    }
}
