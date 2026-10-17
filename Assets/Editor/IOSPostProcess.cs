#if UNITY_IOS

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IOSPostProcess
{
    private const string MinimumIOSVersion = "15.0";

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

        // Unity-iPhone / UnityFramework target を取得
        string unityMainTargetGuid =
            project.GetUnityMainTargetGuid();

        string unityFrameworkGuid =
            project.GetUnityFrameworkTargetGuid();

        // ----------------------------------------------------------------
        // 最新Xcode対応
        // iOS Deployment Target を15.0に設定
        // ----------------------------------------------------------------
        project.SetBuildProperty(
            unityMainTargetGuid,
            "IPHONEOS_DEPLOYMENT_TARGET",
            MinimumIOSVersion
        );

        project.SetBuildProperty(
            unityFrameworkGuid,
            "IPHONEOS_DEPLOYMENT_TARGET",
            MinimumIOSVersion
        );

        // ----------------------------------------------------------------
        // IPA版でUnity起動時にクラッシュする問題への対策
        // UnityFramework の Release configuration のみ変更
        // ----------------------------------------------------------------
        string releaseConfigGuid =
            project.BuildConfigByName(
                unityFrameworkGuid,
                "Release"
            );

        if (!string.IsNullOrEmpty(releaseConfigGuid))
        {
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
        }
        else
        {
            Debug.LogError(
                "[iOS PostProcess] UnityFramework Release configuration was not found."
            );
        }

        // まずPBXProject APIで保存
        project.WriteToFile(projectPath);

        // ----------------------------------------------------------------
        // Unity/Xcodeのバージョンによっては、
        // Projectレベルや追加Targetにも古いDeployment Targetが残る場合がある。
        // project.pbxproj 内の既存のIPHONEOS_DEPLOYMENT_TARGETを
        // すべて15.0に統一する。
        // ----------------------------------------------------------------
        string pbxText = File.ReadAllText(projectPath);

        pbxText = Regex.Replace(
            pbxText,
            @"IPHONEOS_DEPLOYMENT_TARGET = [0-9.]+;",
            "IPHONEOS_DEPLOYMENT_TARGET = " + MinimumIOSVersion + ";"
        );

        File.WriteAllText(projectPath, pbxText);

        Debug.Log(
            "[iOS PostProcess] Xcode project settings updated: " +
            "IPHONEOS_DEPLOYMENT_TARGET=" + MinimumIOSVersion + ", " +
            "UnityFramework Release GCC_OPTIMIZATION_LEVEL=0, " +
            "STRIP_INSTALLED_PRODUCT=NO"
        );
    }
}

#endif
