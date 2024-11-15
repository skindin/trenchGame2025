#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using static CustomBuildWindow;
using Unity.VisualScripting;

public class CustomBuildWindow : EditorWindow
{
    private const string ServerBuildPathKey = "CustomBuildWindow_ServerBuildPath";
    private const string ClientBuildPathKey = "CustomBuildWindow_ClientBuildPath";
    private const string ServerBuildNameKey = "CustomBuildWindow_ServerBuildName";
    private const string ClientBuildNameKey = "CustomBuildWindow_ClientBuildName";
    private const string BuildServerKey = "CustomBuildWindow_BuildServer";
    private const string BuildClientKey = "CustomBuildWindow_BuildClient";
    private const string BuildTargetKey = "CustomBuildWindow_BuildTarget"; // Key for build target

    const string DefaultPathKey = "CustomBuildWindow_DefaultPathKey", ScrollPosYKey = "CustomBuildWindow_ScrollPosY";

    Vector2 scrollPos = Vector2.zero;
    public string defaultPath = "";

    [System.Serializable]
    public class Build
    {
        static string nameKeyPrefix = "BuildName", pathKeyPrefix = "BuildPath", targetKeyPrefix = "BuildTarget", subTargetKeyPrefix = "BuildSubTarget", includeKeyPrefix = "IncludeKey";

        public string name, nameKey, path, pathKey, targetKey, subTargetKey, includeKey;
        public BuildTarget target = BuildTarget.StandaloneWindows;
        public StandaloneBuildSubtarget subTarget = StandaloneBuildSubtarget.Player;
        public bool include = true;
        public int index;

        public Build (string name, string path, BuildTarget target, StandaloneBuildSubtarget subTarget, bool include, int index)
        {
            this.name = name;
            this.path = path;
            this.target = target;
            this.subTarget = subTarget;
            this.include = include;
            SaveBuild (index);
        }

        public void BuildMe ()
        {
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(path,name),
                target = target,
                options = BuildOptions.None,
                subtarget = (int)subTarget
            }
            );
        }

        public void SaveBuild (int index = -1)
        {
            if (index < 0)
                index = this.index;

            EditorPrefs.SetString( nameKey = nameKeyPrefix + index, name);
            EditorPrefs.SetString(pathKey = pathKeyPrefix + index, path);
            EditorPrefs.SetInt(targetKey = targetKeyPrefix + index, (int)target);
            EditorPrefs.SetInt(subTargetKey = subTargetKeyPrefix + index, (int)subTarget);
            EditorPrefs.SetBool(includeKey = includeKeyPrefix + index, include);
            this.index = index;
        }

        public static void DeleteBuild (int index)
        {
            EditorPrefs.DeleteKey(nameKeyPrefix + index);
            EditorPrefs.DeleteKey(pathKeyPrefix + index);
            EditorPrefs.DeleteKey (targetKeyPrefix + index);
            EditorPrefs.DeleteKey(subTargetKeyPrefix + index);
            EditorPrefs.DeleteKey(includeKeyPrefix + index);
        }
    }

    public List<Build> builds = new();

    [MenuItem("Build/Open Custom Build Window")]
    public static void OpenWindow()
    {
        CustomBuildWindow window = GetWindow<CustomBuildWindow>("Custom Build Window");
        window.LoadPrefs();
        window.Show();
        //window.DeletePrefs();
    }

    private void OnEnable()
    {
        //LoadPrefs();
        LoadPrefs();
    }

    private void OnDisable()
    {
        SavePrefs();
    }

    private void OnGUI()
    {
        GUILayout.Label("Specify Custom Build Paths", EditorStyles.boldLabel);

        defaultPath = EditorGUILayout.TextField("Default Path:", defaultPath);
        if (GUILayout.Button("Browse Default Path"))
        {
            string selectedPath = OpenFolderPanel("Select Default Path", defaultPath);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                defaultPath = selectedPath;
                SavePrefs();
            }
        }

        if (GUILayout.Button("New Build"))
        {
            NewBuild();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (Build build in builds)
        {
            build.include = GUILayout.Toggle(build.include, $"{build.name}"
                + $" {build.target.ToString()} {build.subTarget}"
                //, EditorStyles.boldLabel
                );

            if (build.include)
            {
                EditorGUI.indentLevel++;

                build.path = EditorGUILayout.TextField("Path:", build.path);
                if (GUILayout.Button("Browse Build Path"))
                {
                    string selectedPath = OpenFolderPanel("Select Build Path", build.path);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        build.path = selectedPath;
                        build.SaveBuild();
                    }
                }

                build.name = EditorGUILayout.TextField("Name:", build.name);

                build.target = (BuildTarget)EditorGUILayout.EnumPopup("Target:", build.target);
                build.subTarget = (StandaloneBuildSubtarget)EditorGUILayout.EnumPopup("Subtarget: ", build.subTarget);

                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Build"))
        {
            foreach (var build in builds)
            {
                if (build.include)
                {
                    build.BuildMe();
                }
            }
        }
    }

    public void NewBuild ()
    {
        builds.Add(new("new build", defaultPath, BuildTarget.StandaloneWindows, StandaloneBuildSubtarget.Player, true, builds.Count));
    }

    private string OpenFolderPanel(string title, string defaultPath)
    {
        return EditorUtility.OpenFolderPanel(title, defaultPath, "");
    }

    private void LoadPrefs()
    {
        defaultPath = EditorPrefs.GetString(DefaultPathKey,defaultPath);
        scrollPos.y = EditorPrefs.GetFloat(ScrollPosYKey, scrollPos.y);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(DefaultPathKey, defaultPath);
        EditorPrefs.SetFloat(ScrollPosYKey, scrollPos.y);
    }


    void DeletePrefs ()
    {
        EditorPrefs.DeleteKey(ServerBuildPathKey);
        EditorPrefs.DeleteKey(ClientBuildPathKey);
        EditorPrefs.DeleteKey (ServerBuildNameKey);
        EditorPrefs.DeleteKey(ClientBuildNameKey);
        EditorPrefs.DeleteKey(BuildServerKey);
        EditorPrefs.DeleteKey(BuildClientKey);
        EditorPrefs.DeleteKey(BuildTargetKey);

        Debug.Log("deleted old prefs");
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
