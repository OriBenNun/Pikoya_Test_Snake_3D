// Builds the Web player through the release build profile, which owns the shipping player
// settings (product name, compression, IL2CPP configuration and the project's own page template).

UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);

if (!UnityEditor.BuildPipeline.IsBuildTargetSupported(UnityEditor.BuildTargetGroup.WebGL, UnityEditor.BuildTarget.WebGL))
    return "FAILED: install Web Build Support for this Editor";

UnityEditor.Build.Profile.BuildProfile profile = null;
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:BuildProfile"))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (!path.Contains("Web - Desktop - Release")) continue;
    profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(path);
    break;
}
if (profile == null) return "FAILED: no 'Web - Desktop - Release' build profile";
UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(profile);

UnityEditor.EditorUserBuildSettings.development = false;
UnityEditor.EditorUserBuildSettings.allowDebugging = false;

var output = "Builds/WebGL/REL_" + UnityEditor.PlayerSettings.bundleVersion;
var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerWithProfileOptions
{
    buildProfile = profile,
    locationPathName = output,
    options = UnityEditor.BuildOptions.None
});

var summary = report.summary;
if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
    return "FAILED: " + summary.result + " with " + summary.totalErrors + " errors";
return "SUCCEEDED: " + output + ", " + (summary.totalSize / 1048576) + " MB";
