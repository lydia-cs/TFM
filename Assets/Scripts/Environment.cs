using Animancer;
using System;
using UnityEngine;

[Serializable]
public class Environment : ILoadableModel
{
    public string Id;             // Unique identifier for the environment
    public string Description;    // Description of the environment
    public string Model;          // Path under Resources/Models
    public string Unit;           // Unit or scale reference for measurements
    public float[] Scale;         // Optional scaling factors [x, y, z]
    public float[] Rotation;      // Optional rotation [x, y, z] in degrees
    public float Translation;     // Optional translation along a specific axis
    public float[] Origin;        // Origin point for the environment
    public GameObject ModelObject; // The instantiated GameObject in the scene

    // Interface implementation for ILoadableModel
    string ILoadableModel.Model => Model;
    string ILoadableModel.Id => Id;

    // Transform Methods
    public void UpdateTransform(Vector3 newPosition, Quaternion newRotation, Vector3 newScale) // Updates the GameObject’s transform in world space
    {
        if (ModelObject != null)
        {
            ModelObject.transform.position = newPosition;
            ModelObject.transform.rotation = newRotation;
            ModelObject.transform.localScale = newScale;
        }
    }

    // Physics Methods
    public void InitiatePhysics() // Adds MeshColliders to all child meshes and a static Rigidbody to the root
    {
        if (ModelObject == null) return;

        void AddCollidersRecursively(Transform parent) // Helper to add MeshColliders to children recursively
        {
            foreach (Transform child in parent)
            {
                if ((child.GetComponent<MeshRenderer>() || child.GetComponent<SkinnedMeshRenderer>()) &&
                    child.GetComponent<Collider>() == null)
                {
                    child.gameObject.AddComponent<MeshCollider>();
                }
                AddCollidersRecursively(child);
            }
        }

        AddCollidersRecursively(ModelObject.transform);

        Rigidbody rb = ModelObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ModelObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Debug.Log($"Physics initialized for environment '{ModelObject.name}'");
    }
}


