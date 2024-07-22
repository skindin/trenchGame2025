#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class CustomBuildWindow : EditorWindow
{
    private const string ServerBuildPathKey = "CustomBuildWindow_ServerBuildPath";
    private const string ClientBuildPathKey = "CustomBuildWindow_ClientBuildPath";
    private const string ServerBuildNameKey = "CustomBuildWindow_ServerBuildName";
    private const string ClientBuildNameKey = "CustomBuildWindow_ClientBuildName";
    private const string BuildServerKey = "CustomBuildWindow_BuildServer";
    private const string BuildClientKey = "CustomBuildWindow_BuildClient";

    private string serverBuildPath;
    private string clientBuildPath;
    private string serverBuildName = "ServerBuild";
    private string clientBuildName = "ClientBuild";
    private bool buildServer;
    private bool buildClient;

    public static string serverSymbol = "DEDICATED_SERVER";

    [MenuItem("Build/Open Custom Build Window")]
    public static void OpenWindow()
    {
        CustomBuildWindow window = GetWindow<CustomBuildWindow>("Custom Build Window");
        window.LoadPrefs();
        window.Show();
    }

    private void OnEnable()
    {
        LoadPrefs();
    }

    private void OnDisable()
    {
        SavePrefs();
    }

    private void OnGUI()
    {
        GUILayout.Label("Specify Custom Build Paths", EditorStyles.boldLabel);

        // Display and update server build path
        string newServerBuildPath = EditorGUILayout.TextField("Server Build Path:", serverBuildPath);
        if (GUILayout.Button("Browse Server Build Path"))
        {
            string selectedPath = OpenFolderPanel("Select Server Build Path", serverBuildPath);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                serverBuildPath = selectedPath;
                SavePrefs();
            }
        }

        // Display and update server build name
        serverBuildName = EditorGUILayout.TextField("Server Build Name:", serverBuildName);

        // Display and update client build path
        string newClientBuildPath = EditorGUILayout.TextField("Client Build Path:", clientBuildPath);
        if (GUILayout.Button("Browse Client Build Path"))
        {
            string selectedPath = OpenFolderPanel("Select Client Build Path", clientBuildPath);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                clientBuildPath = selectedPath;
                SavePrefs();
            }
        }

        // Display and update client build name
        clientBuildName = EditorGUILayout.TextField("Client Build Name:", clientBuildName);

        buildServer = GUILayout.Toggle(buildServer, "Server Build");
        buildClient = GUILayout.Toggle(buildClient, "Client Build");

        if (GUILayout.Button("Build"))
        {
            if (buildServer)
            {
                BuildDedicatedServer();
            }

            if (buildClient)
            {
                BuildClient();
            }
        }

        //GUILayout.end
    }

    private void BuildClient()
    {
        var buildOptions = GetCurrentBuildPlayerOptions();
        buildOptions.locationPathName = Path.Combine(clientBuildPath, clientBuildName + ".exe");
        buildOptions.target = BuildTarget.StandaloneWindows;

        BuildPipeline.BuildPlayer(buildOptions);
    }

    private void BuildDedicatedServer()
    {
        var buildOptions = GetCurrentBuildPlayerOptions();
        buildOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        buildOptions.target = BuildTarget.StandaloneWindows;
        buildOptions.locationPathName = Path.Combine(serverBuildPath, serverBuildName + ".exe");

        AddDefineSymbol(serverSymbol);

        BuildPipeline.BuildPlayer(buildOptions);

        RemoveDefineSymbol(serverSymbol);
    }

    private string OpenFolderPanel(string title, string defaultPath)
    {
        return EditorUtility.OpenFolderPanel(title, defaultPath, "");
    }

    private void LoadPrefs()
    {
        serverBuildPath = EditorPrefs.GetString(ServerBuildPathKey, "C:/MyGame/DedicatedServer");
        clientBuildPath = EditorPrefs.GetString(ClientBuildPathKey, "C:/MyGame/Client");
        serverBuildName = EditorPrefs.GetString(ServerBuildNameKey, "ServerBuild");
        clientBuildName = EditorPrefs.GetString(ClientBuildNameKey, "ClientBuild");
        buildServer = EditorPrefs.GetBool(BuildServerKey, true);
        buildClient = EditorPrefs.GetBool(BuildClientKey, true);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(ServerBuildPathKey, serverBuildPath);
        EditorPrefs.SetString(ClientBuildPathKey, clientBuildPath);
        EditorPrefs.SetString(ServerBuildNameKey, serverBuildName);
        EditorPrefs.SetString(ClientBuildNameKey, clientBuildName);
        EditorPrefs.SetBool(BuildServerKey, buildServer);
        EditorPrefs.SetBool(BuildClientKey, buildClient);
    }

    private BuildPlayerOptions GetCurrentBuildPlayerOptions()
    {
        return new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = "",
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.None
        };
    }

    public static void AddDefineSymbol(string symbol)
    {
        // Get the current define symbols for the Standalone target group (for example)
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        // Add the new symbol if it doesn't already exist
        if (!currentSymbols.Contains(symbol))
        {
            string newSymbols = currentSymbols + ";" + symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newSymbols);
            Debug.Log($"Added define symbol: {symbol}");
        }
        else
        {
            Debug.Log($"Define symbol already exists: {symbol}");
        }
    }

    // Remove define symbols for a specific build target group
    public static void RemoveDefineSymbol(string symbol)
    {
        // Get the current define symbols
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        // Remove the symbol
        string[] symbols = currentSymbols.Split(';');
        string newSymbols = string.Join(";", symbols.Where(s => s != symbol));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newSymbols);
        Debug.Log($"Removed define symbol: {symbol}");
    }
}
#endif
