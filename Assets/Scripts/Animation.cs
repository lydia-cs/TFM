using System;
using UnityEditor.Animations;
using UnityEngine;

/// Represents an animation playable by a character in the ritual
[Serializable]
public class Animation
{
    public string Id;               // Unique identifier
    public string CharacterID;      // ID of the character this animation belongs to
    public string Actor;            // Optional: name of the actor performing the animation
}


