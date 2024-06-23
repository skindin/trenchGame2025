using System.Diagnostics;
using UnityEngine;
using UnityEditor;

public class BuildScript
{
#if UNITY_EDITOR
    //[MenuItem("Build/Build Dedicated Server")]
    public static void BuildDedicatedServer(string path)
    {
        SetDefineSymbol("server");
        Build(path);
        ClearDefineSymbol("server");
    }

    //[MenuItem("Build/Build Client")]
    public static void BuildClient(string path)
    {
        SetDefineSymbol("client");
        Build(path);
        ClearDefineSymbol("client");
    }

    private static void SetDefineSymbol(string symbol)
    {
        var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (!currentSymbols.Contains(symbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, $"{currentSymbols};{symbol}");
        }
    }

    private static void ClearDefineSymbol(string symbol)
    {
        var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (currentSymbols.Contains(symbol))
        {
            var newSymbols = currentSymbols.Replace(symbol, "").Replace(";;", ";").Trim(';');
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newSymbols);
        }
    }

    private static void Build(string buildPath)
    {
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" },  // Add your scene paths here
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
#endif
}
