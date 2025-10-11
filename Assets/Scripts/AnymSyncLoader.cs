using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Handles loading, processing, and modifying synchronized animations with events
public class AnymSyncLoader : MonoBehaviour
{
    // Public Data
    public AnimData animData;                        // Loaded animation data from JSON
    public List<AnimSync> AnimationsToSync;          // Sync instructions
    public List<AnimEvent> AnimEvents;               // Animation events list

    // Internal Data
    private Dictionary<string, List<AnimEvent>> eventsByAnimName; // Lookup dictionary
    private float frameRate = 30f;                                  // Default frame rate
    private List<Segment> segments = new List<Segment>();           // Temporary segments list

    // Nested Class
    [Serializable]
    public class Segment
    {
        public int startFrame = 0;
        public int endFrame = -1;
        public float speedMultiplier = 1f;
    }

    // Data Loading
    public void LoadAnimData()
    {
        var jsonFile = Resources.Load<TextAsset>("animData");
        if (jsonFile != null)
        {
            animData = JsonUtility.FromJson<AnimData>(jsonFile.text);
            Debug.Log($"Loaded {animData.AnimSyncs.Count} syncs and {animData.AnimEvents.Count} events.");
        }
        else
        {
            Debug.LogError("animData.json not found in Resources!");
        }

        AnimationsToSync = animData?.AnimSyncs;
        AnimEvents = animData?.AnimEvents;
    }

    // Event Management
    public void BuildEventsDict()
    {
        eventsByAnimName = AnimEvents
            .GroupBy(e => GetKey(e.CharacterId, e.Animation))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private string GetKey(string charId, string animName) => $"{charId}_{animName}".ToLower();

    public List<AnimEvent> GetEventsFor(string characterId, string animationName)
    {
        if (eventsByAnimName == null) BuildEventsDict();

        string key = GetKey(characterId, animationName);
        if (eventsByAnimName.TryGetValue(key, out var list))
        {
            eventsByAnimName.Remove(key); // Track added events
            return list;
        }

        return new List<AnimEvent>();
    }

    // Animation Modification
    public void ModifyAnimation2()
    {
        if (AnimationsToSync == null)
        {
            Debug.LogError("AnimationsToSync not loaded. Call LoadAnimData first.");
            return;
        }

        foreach (var ats in AnimationsToSync)
        {
            string anim1Name = $"{ats.Character1.ToLower()}_{ats.Animation1}";
            string anim2Name = $"{ats.Character2.ToLower()}_{ats.Animation2}";

            AnimationClip anim1Clip = Resources.Load<AnimationClip>($"Animations/{anim1Name}");
            AnimationClip anim2Clip = Resources.Load<AnimationClip>($"Animations/{anim2Name}");

            if (anim1Clip == null || anim2Clip == null)
            {
                Debug.LogError($"Missing clip(s): {anim1Name} or {anim2Name}");
                continue;
            }

            anim1Clip.name = ats.Animation1;
            anim2Clip.name = ats.Animation2;
            frameRate = anim2Clip.frameRate;

            int delayByFrames = Mathf.RoundToInt(ats.Delay * frameRate);
            int[] alignedFrames1 = ats.Frames1.Select(f => f - delayByFrames).ToArray();

            var segments = GenerateSegmentsFromFrameData(anim1Clip, anim2Clip, alignedFrames1, ats.Frames2);

            CreateModifiedClip(anim2Clip, segments, ats.Character2);
        }
    }

    private List<Segment> GenerateSegmentsFromFrameData(AnimationClip anim1, AnimationClip anim2, int[] frames1, int[] frames2)
    {
        var segments = new List<Segment>();
        int lastFrame1 = Mathf.RoundToInt(anim1.length * frameRate);
        int lastFrame2 = Mathf.RoundToInt(anim2.length * frameRate);

        // Split Animation2 frames into segments
        var anim2Segments = new List<(int start, int end)>();
        int prev = 0;
        foreach (var f in frames2)
        {
            anim2Segments.Add((prev, f));
            prev = f + 1;
        }
        if (prev <= lastFrame2) anim2Segments.Add((prev, lastFrame2));

        // Compute durations from Animation1
        var durations1 = new List<int>();
        prev = 0;
        foreach (var f in frames1)
        {
            durations1.Add(f - prev);
            prev = f;
        }
        durations1.Add(lastFrame1 - prev);

        // Build segments with speed multipliers
        for (int i = 0; i < anim2Segments.Count; i++)
        {
            var seg = anim2Segments[i];
            int dur2 = seg.end - seg.start;
            int dur1 = i < durations1.Count ? durations1[i] : 1;
            float speed = dur1 > 0 ? (float)dur2 / dur1 : 1f;

            segments.Add(new Segment { startFrame = seg.start, endFrame = seg.end, speedMultiplier = speed });
        }

        return segments;
    }

    public void CreateModifiedClip(AnimationClip originalClip, List<Segment> segments, string character)
    {
        var newClip = new AnimationClip { frameRate = originalClip.frameRate, wrapMode = originalClip.wrapMode };

        // Remap curves
        foreach (var binding in AnimationUtility.GetCurveBindings(originalClip))
        {
            var originalCurve = AnimationUtility.GetEditorCurve(originalClip, binding);
            var newKeys = new List<Keyframe>();

            foreach (var key in originalCurve.keys)
            {
                float keyFrame = key.time * frameRate;
                var seg = segments.Find(s => keyFrame >= s.startFrame && keyFrame <= s.endFrame);
                if (seg == null) { newKeys.Add(key); continue; }

                float offset = 0f;
                foreach (var s2 in segments)
                {
                    if (s2 == seg) break;
                    offset += (s2.endFrame - s2.startFrame) / frameRate / s2.speedMultiplier;
                }

                float relative = (keyFrame - seg.startFrame) / frameRate;
                float scaledTime = offset + (relative / seg.speedMultiplier);

                newKeys.Add(new Keyframe(scaledTime, key.value, key.inTangent, key.outTangent));
            }

            var newCurve = new AnimationCurve(newKeys.ToArray())
            {
                preWrapMode = originalCurve.preWrapMode,
                postWrapMode = originalCurve.postWrapMode
            };

            AnimationUtility.SetEditorCurve(newClip, binding, newCurve);
        }

        // Remap events
        if (AnimEvents != null)
        {
            var animEvs = GetEventsFor(character, originalClip.name);
            var unityEvents = new List<AnimationEvent>();

            foreach (var animEv in animEvs)
            {
                float remappedTime = RemapFrameToTime(animEv.Frame, segments, frameRate);
                int remappedFrame = Mathf.RoundToInt(remappedTime * frameRate);
                unityEvents.Add(animEv.BuildAnimationEvent(remappedFrame, frameRate));
            }

            AnimationUtility.SetAnimationEvents(newClip, unityEvents.ToArray());
        }

        // Save
        string saveFolder = "Assets/Resources/Animations/Modified/";
        if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);

        string fileName = $"{character.ToLower()}_{originalClip.name}.anim";
        string savePath = Path.Combine(saveFolder, fileName);

        if (File.Exists(savePath)) AssetDatabase.DeleteAsset(savePath);
        AssetDatabase.CreateAsset(newClip, savePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Modified AnimationClip saved at: {savePath}");
    }

    private float RemapFrameToTime(int frame, List<Segment> segments, float frameRate)
    {
        float time = 0f;
        foreach (var seg in segments)
        {
            if (frame >= seg.startFrame && frame <= seg.endFrame)
            {
                float relative = (frame - seg.startFrame) / frameRate;
                time += relative / seg.speedMultiplier;
                return time;
            }
            else
            {
                float segmentDuration = (seg.endFrame - seg.startFrame) / frameRate;
                time += segmentDuration / seg.speedMultiplier;
            }
        }
        return 0f;
    }

    // Event-Only Clip Generation
    public void AddEventOnlyClips()
    {
        foreach (var kvp in eventsByAnimName)
        {
            string key = kvp.Key;
            var events = kvp.Value;

            int underscoreIndex = key.IndexOf('_');
            if (underscoreIndex <= 0 || underscoreIndex == key.Length - 1) continue;

            string charId = key.Substring(0, underscoreIndex);
            string animName = key.Substring(underscoreIndex + 1);

            string animPath = $"Animations/{key}";
            AnimationClip originalClip = Resources.Load<AnimationClip>(animPath);
            if (originalClip == null) { Debug.LogWarning($"Animation not found for events: {key}"); continue; }

            var newClip = new AnimationClip
            {
                frameRate = originalClip.frameRate,
                wrapMode = originalClip.wrapMode,
                name = originalClip.name
            };

            foreach (var binding in AnimationUtility.GetCurveBindings(originalClip))
                AnimationUtility.SetEditorCurve(newClip, binding, AnimationUtility.GetEditorCurve(originalClip, binding));

            var unityEvents = new List<AnimationEvent>();
            foreach (var evt in events)
            {
                float time = evt.Frame / newClip.frameRate;
                unityEvents.Add(new AnimationEvent
                {
                    time = time,
                    functionName = "GrabObject",
                    stringParameter = $"{evt.CharacterId}|{evt.GraspingLeft}|{evt.GraspingRight}"
                });
            }

            AnimationUtility.SetAnimationEvents(newClip, unityEvents.ToArray());

            string saveFolder = "Assets/Resources/Animations/Modified/";
            if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);

            string savePath = Path.Combine(saveFolder, key + ".anim");
            if (File.Exists(savePath)) AssetDatabase.DeleteAsset(savePath);

            AssetDatabase.CreateAsset(newClip, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AddEventOnlyClips] Saved: {savePath}");
        }

        // Clear dictionary after use
        eventsByAnimName.Clear();
    }
}

