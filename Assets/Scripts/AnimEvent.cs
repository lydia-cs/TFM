using System;
using UnityEditor.Animations;
using UnityEngine;

/// Represents a single animation event for a character
/// Used to trigger functions (e.g., grabbing objects) at a specific frame
[Serializable]
public class AnimEvent
{
    // Character & Animation Info
    public string CharacterId;      // ID of the character performing the animation
    public string Animation;        // ID of the animation where the event occurs

    // Timing
    public int Frame;               // Frame number when the event should trigger

    // Optional Object References
    public string GraspingLeft;     // Object ID to grasp in the left hand (optional)
    public string GraspingRight;    // Object ID to grasp in the right hand (optional)

    // Build Unity AnimationEvent
    public AnimationEvent BuildAnimationEvent(int frame, float frameRate)
    {
        // Convert frame number to time in seconds
        float time = frame / frameRate;

        return new AnimationEvent
        {
            time = time,
            functionName = "GrabObject",
            stringParameter = $"{CharacterId}|{GraspingLeft ?? ""}|{GraspingRight ?? ""}"
            // Format: CharacterId|LeftObject|RightObject
        };
    }
}


