#if UNITY_IOS

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IOSPostProcess
{
    // 大きめの番号にして、他のPostProcessBuild処理より後に実行する
    [PostProcessBuild(999)]
    public static void OnPostprocessBuild(
        BuildTarget buildTarget,
        string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

        // Unityが生成したXcodeプロジェクトを取得
        string projectPath =
            PBXProject.GetPBXProjectPath(pathToBuiltProject);

        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // UnityFramework target を取得
        string unityFrameworkGuid =
            project.GetUnityFrameworkTargetGuid();

        // UnityFramework の Release configuration を取得
        string releaseConfigGuid =
            project.BuildConfigByName(
                unityFrameworkGuid,
                "Release"
            );

        if (string.IsNullOrEmpty(releaseConfigGuid))
        {
            Debug.LogError(
                "[iOS PostProcess] UnityFramework Release configuration was not found."
            );
            return;
        }

        // IPA版でUnity起動時にクラッシュする問題への対策
        // Optimization Level = None [-O0]
        project.SetBuildPropertyForConfig(
            releaseConfigGuid,
            "GCC_OPTIMIZATION_LEVEL",
            "0"
        );

        // Strip Linked Product = No
        project.SetBuildPropertyForConfig(
            releaseConfigGuid,
            "STRIP_INSTALLED_PRODUCT",
            "NO"
        );

        // pbxproj に保存
        project.WriteToFile(projectPath);

        Debug.Log(
            "[iOS PostProcess] UnityFramework Release settings updated: " +
            "GCC_OPTIMIZATION_LEVEL=0, " +
            "STRIP_INSTALLED_PRODUCT=NO"
        );
    }
}

#endif