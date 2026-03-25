using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class AnimData
{
    public List<AnimSync> AnimSyncs;
    public List<AnimEvent> AnimEvents;
    public List<Animation> Animations;
}
