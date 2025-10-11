using System;
using UnityEngine;

/// Represents synchronization info between two characters’ animations
/// Ensures two animations play in sync, with optional frame offsets
[Serializable]
public class AnimSync
{
    // Character References
    public string Character1;       // ID of the first character
    public string Character2;       // ID of the second character

    // Animation Clips & Frames
    public string Animation1;       // ID of first character’s animation
    public string Animation2;       // ID of second character’s animation
    public int[] Frames1;           // Frame numbers of first animation to sync
    public int[] Frames2;           // Frame numbers of second animation to sync

    // Sync Settings
    public int Delay;               // Optional: delay in seconds before starting Animation2
}


