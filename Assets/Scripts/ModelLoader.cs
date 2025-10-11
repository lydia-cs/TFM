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
        ApplyMaterials(instance, asset.Model);               // Apply textures and materials
        Debug.Log($"Spawned Model: {asset.Id}");
        return instance;
    }

    // Applies textures to all renderers in the model
    // Looks in Resources/Textures/{modelFolder}/ for textures matching material names
    private static void ApplyMaterials(GameObject model, string modelFolder)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(); // All child renderers

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                string materialName = mat.name.Replace(" (Instance)", "") + "DiffuseMap.tga"; // Texture name
                string textureName = materialName.Replace(" ", "_");
                string texturePath = $"Textures/{modelFolder}/{textureName}";

                Texture2D texture = Resources.Load<Texture2D>(texturePath); // Load texture
                if (texture != null)
                {
                    Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); // Create material
                    newMat.SetTexture("_BaseMap", texture);
                    renderer.material = newMat;   // Assign material
                    Debug.Log($"Applied new material: {newMat.name} with texture: {textureName}");
                }
                else
                {
                    Debug.LogWarning($"Texture not found for material: {mat.name}");
                }
            }
        }
    }
}



