using System;
using UnityEngine;

// Represents a specific position or reference point in an environment
[Serializable]
public class Place
{
    public string Id;                 // Unique identifier for the place
    public string Title;              // Display name of the place
    public string Environment;        // ID of the environment this place belongs to
    public float[] Location;          // World position coordinates of the place
    public string RelativeGameObject; // Optional: ID of a related object used as reference
}


