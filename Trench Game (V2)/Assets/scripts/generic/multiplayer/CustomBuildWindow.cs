#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class CustomBuildWindow : EditorWindow
{
    private string serverBuildPath = "C:/MyGame/DedicatedServer"; // Default server build path
    private string clientBuildPath = "C:/MyGame/Client"; // Default client build path

    [MenuItem("Build/Open Custom Build Window")]
    public static void OpenWindow()
    {
        CustomBuildWindow window = GetWindow<CustomBuildWindow>("Custom Build Window");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Specify Custom Build Paths", EditorStyles.boldLabel);

        serverBuildPath = EditorGUILayout.TextField("Server Build Path:", serverBuildPath);
        if (GUILayout.Button("Browse Server Build Path"))
        {
            serverBuildPath = OpenFolderPanel("Select Server Build Path", serverBuildPath);
        }

        clientBuildPath = EditorGUILayout.TextField("Client Build Path:", clientBuildPath);
        if (GUILayout.Button("Browse Client Build Path"))
        {
            clientBuildPath = OpenFolderPanel("Select Client Build Path", clientBuildPath);
        }

        if (GUILayout.Button("Build Dedicated Server"))
        {
            BuildDedicatedServer();
        }

        if (GUILayout.Button("Build Client"))
        {
            BuildClient();
        }
    }

    private void BuildDedicatedServer()
    {
        Debug.Log("Building dedicated server...");
        Build(serverBuildPath);
    }

    private void BuildClient()
    {
        Debug.Log("Building client...");
        Build(clientBuildPath);
    }

    private void Build(string outputPath)
    {
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" }, // Update with your scenes
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildOptions);

        Debug.Log($"Build completed at: {outputPath}");
    }

    private string OpenFolderPanel(string title, string defaultPath)
    {
        return EditorUtility.OpenFolderPanel(title, defaultPath, "");
    }
}
#endif
