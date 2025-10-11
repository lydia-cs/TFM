using System;
using UnityEngine;

// Represents a single line or action in the ritual sequence
[Serializable]
public class Play
{
    public string ID;                 // Unique identifier for this line
    public string ActID;              // ID of the act this line belongs to
    public string SceneID;            // ID of the scene this line belongs to
    public string BranchID;           // Optional: ID of the branch this line belongs to
    public string EnvironmentID;      // ID of the environment to use for this line
    public string Description;        // Text description of the line (may include "ChooseBranch")
    public float Duration;            // Duration in seconds (-1 = automatic based on animation)
    public string CharacterID;        // ID of the character performing the action
    public string StartPos;           // Optional starting position reference
    public string Animation;          // Optional animation to play
    public string GraspingLeft;       // Optional object held in left hand
    public string GraspingRight;      // Optional object held in right hand
    public string Target;             // Optional target character/object for movement or animation
    public string Dialogue;           // Optional dialogue text
    public string Audio;              // Optional audio clip (path/ID for Resources/Audios)
    public float[] CameraPos;         // Optional camera position (world coordinates)
    public string CameraTarget;       // Optional camera target (character or object ID)
}


