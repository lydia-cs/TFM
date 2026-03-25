using UnityEngine;

public static class ModelLoader
{
    // Instantiates a model prefab from Resources and applies textures
    // Returns the instantiated GameObject or null if loading fails
    public static GameObject InstantiateModel(ILoadableModel asset)
    {
        if (asset == null) return null;                      // Null check
        if (string.IsNullOrEmpty(asset.Model)) return null;  // Invalid model path check

        // Load prefab from Resources/Models
        GameObject prefab = Resources.Load<GameObject>($"Models/{asset.Model}");
        if (prefab == null)
        {
            Debug.LogError($"Model not found: Models/{asset.Model}");
            return null;
        }

        // Instantiate the prefab
        GameObject instance = GameObject.Instantiate(prefab);
        instance.name = asset.Id;                            // Rename instance to match asset ID
        Debug.Log($"Spawned Model: {asset.Id}");
        return instance;
    }
}



