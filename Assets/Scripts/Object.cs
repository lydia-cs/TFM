using System;
using UnityEngine;

// Represents a 3D object in the ritual environment
[Serializable]
public class Object : ILoadableModel
{
    public string Id;                // Unique identifier for the object
    public bool IsStatic;            // If true, object does not move and physics is kinematic
    public string Description;       // Short description of the object
    public string ExtendedDescription; // Extended description
    public string Provenance;        // Source or origin info
    public string LicenseModel;      // License type
    public string LicenseSources;    // License source references
    public string Model;             // Path under Resources/Models
    public string[] PhotoURL;        // Optional images of the object
    public string Size;              // Text-based size info (optional)

    // Transform Data
    public float[] Location;         // World position of the object
    public float[] Scale;            // Local scale of the object
    public float[] Rotation;         // Euler rotation angles
    public float Translation;        // Optional translation (unused in current context)

    // Runtime Reference
    public GameObject ModelObject;   // Instantiated GameObject reference

    // Interface (ILoadableModel)
    string ILoadableModel.Model => Model;
    string ILoadableModel.Id => Id;

    // Transform Management
    public void UpdateTransform(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
    {
        if (ModelObject != null)
        {
            ModelObject.transform.position = newPosition;   // Update position
            ModelObject.transform.rotation = newRotation;   // Update rotation
            ModelObject.transform.localScale = newScale;    // Update scale
        }
    }

    // Physics Setup & Initialization
    // Adds a BoxCollider and Rigidbody to the root object based on the first mesh child
    public void InitiatePhysics()
    {
        if (ModelObject == null) return;

        // Find the first mesh-bearing child
        MeshFilter meshChildFilter = ModelObject.GetComponentInChildren<MeshFilter>();
        Renderer meshChildRenderer = ModelObject.GetComponentInChildren<Renderer>();

        if (meshChildFilter == null || meshChildRenderer == null)
        {
            Debug.LogWarning($"No mesh found under {ModelObject.name}");
            return;
        }

        // Compute bounds of the child in world space
        Bounds bounds = meshChildRenderer.bounds;

        // Add or get a BoxCollider on the root
        BoxCollider boxCol = ModelObject.GetComponent<BoxCollider>();
        if (boxCol == null)
            boxCol = ModelObject.AddComponent<BoxCollider>();

        // Convert world bounds to local bounds relative to root
        Vector3 localCenter = ModelObject.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = Vector3.Scale(bounds.size, new Vector3(
            1f / ModelObject.transform.lossyScale.x,
            1f / ModelObject.transform.lossyScale.y,
            1f / ModelObject.transform.lossyScale.z
        ));

        boxCol.center = localCenter;
        boxCol.size = localSize;

        // Add or get a Rigidbody on the root
        Rigidbody rb = ModelObject.GetComponent<Rigidbody>();
        if (rb == null)
            rb = ModelObject.AddComponent<Rigidbody>();

        // Configure Rigidbody based on whether the object is static
        if (IsStatic)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 1;
        }

        Debug.Log($"Physics initialized for root '{ModelObject.name}' using child mesh '{meshChildFilter.gameObject.name}'");
    }


    // Physics State Control
    public void SetPhysicsActive(bool isActive)
    {
        if (ModelObject == null) return;

        Rigidbody rb = ModelObject.GetComponent<Rigidbody>();
        Collider col = ModelObject.GetComponent<Collider>();
        if (rb == null || col == null) return;

        rb.isKinematic = !isActive; // If active, physics simulated
        rb.useGravity = isActive;
        col.enabled = isActive;     // Enable/disable collider
    }
}


