#pragma warning disable UDR0001

using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class StartSceneLoader
{
    private const string StartScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PrefKey = "StartSceneLoader_Enabled";

    static StartSceneLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Start Scene Loader/Enable", true)]
    private static bool ValidateMenu()
    {
        Menu.SetChecked("Tools/Start Scene Loader/Enable", Enabled);
        return true;
    }

    [MenuItem("Tools/Start Scene Loader/Enable")]
    private static void Toggle()
    {
        Enabled = !Enabled;
        Menu.SetChecked("Tools/Start Scene Loader/Enable", Enabled);
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!Enabled) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}

