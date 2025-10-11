using UnityEditor;
using UnityEngine;

/// Editor utility to generate modified animations using AnymSyncLoader
public class AnymSyncLoaderEditor
{
    /// Adds a menu item in Unity Editor under Tools -> Generate Modified Animations
    [MenuItem("Tools/Generate Modified Animations")]
    public static void GenerateModifiedAnimations()
    {
        // Setup Temporary Loader
        GameObject tempGO = new GameObject("Temp_AnymSyncRunner");
        var loader = tempGO.AddComponent<AnymSyncLoader>();

        // Load & Modify Animations
        loader.LoadAnimData();        // Load animation data from Resources/animData.json
        loader.ModifyAnimation2();    // Modify Animation2 clips based on sync data
        loader.AddEventOnlyClips();   // Add clips containing only animation events

        // Cleanup
        UnityEngine.Object.DestroyImmediate(tempGO);

        Debug.Log("All modified animations generated.");
        EditorUtility.DisplayDialog("Finished", "Modified animations generated successfully.", "OK");
    }
}



