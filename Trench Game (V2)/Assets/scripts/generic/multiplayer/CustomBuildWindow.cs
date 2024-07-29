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
    private const string BuildTargetKey = "CustomBuildWindow_BuildTarget"; // Key for build target

    private string serverBuildPath;
    private string clientBuildPath;
    private string serverBuildName = "ServerBuild";
    private string clientBuildName = "ClientBuild";
    private bool buildServer;
    private bool buildClient;
    private BuildTarget buildTarget = BuildTarget.StandaloneWindows; // Default build target

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
        serverBuildPath = EditorGUILayout.TextField("Server Build Path:", serverBuildPath);
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
        clientBuildPath = EditorGUILayout.TextField("Client Build Path:", clientBuildPath);
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
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Client Build Target:", buildTarget);

        buildServer = GUILayout.Toggle(buildServer, "Server Build");
        buildClient = GUILayout.Toggle(buildClient, "Client Build");

        // Display and update build target

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
    }

    private void BuildClient()
    {
        var buildOptions = GetCurrentBuildPlayerOptions();
        buildOptions.locationPathName = Path.Combine(clientBuildPath, clientBuildName + ".exe");
        buildOptions.target = buildTarget; // Use the stored build target

        BuildPipeline.BuildPlayer(buildOptions);
    }

    private void BuildDedicatedServer()
    {
        var buildOptions = GetCurrentBuildPlayerOptions();
        buildOptions.locationPathName = Path.Combine(serverBuildPath, serverBuildName + ".exe");
        buildOptions.target = BuildTarget.StandaloneLinux64; // Always use StandaloneWindows for server
        buildOptions.options = BuildOptions.AllowDebugging | BuildOptions.Development;
        buildOptions.subtarget = (int)StandaloneBuildSubtarget.Server;

        BuildPipeline.BuildPlayer(buildOptions);
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

        // Load the build target
        int buildTargetInt = EditorPrefs.GetInt(BuildTargetKey, (int)BuildTarget.StandaloneWindows);
        buildTarget = (BuildTarget)buildTargetInt;
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(ServerBuildPathKey, serverBuildPath);
        EditorPrefs.SetString(ClientBuildPathKey, clientBuildPath);
        EditorPrefs.SetString(ServerBuildNameKey, serverBuildName);
        EditorPrefs.SetString(ClientBuildNameKey, clientBuildName);
        EditorPrefs.SetBool(BuildServerKey, buildServer);
        EditorPrefs.SetBool(BuildClientKey, buildClient);

        // Save the build target
        EditorPrefs.SetInt(BuildTargetKey, (int)buildTarget);
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
}
#endif
