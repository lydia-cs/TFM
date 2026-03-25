using GLTFast.Schema;
using System;
using System.Buffers.Text;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// Represents a ritual character — includes model, animations, and interactions
[Serializable]
public class Character : ILoadableModel
{
    public string Id;                     // Unique ID
    public string Name;                   // Display name
    public string Description;            // Short description
    public string ExtendedDescription;    // Extended description

    // Model & Avatar
    public string Model;                  // ID in Resources/Models
    public string Avatar;                 

    // Transform data
    public float[] Scale;
    public float[] Rotation;
    public float Translation;

    // Runtime references
    public Avatar CharacterAvatar;        // Avatar assigned to Animator
    public Animator Anim;                 // Runtime Animator
    public GameObject ModelObject;        // Instantiated model in scene

    // Objects held
    public Object ObjectInLeftHand;
    public Object ObjectInRightHand;

    // Interface
    string ILoadableModel.Model => Model;
    string ILoadableModel.Id => Id;

    // Animation settings
    public float speedMultiplier = 2f;

    // Two-hand anchor
    private GameObject twoHandAnchor;

    // LowerBody defaul idle
    private const int BASE_LAYER = 0;
    private const int LOWER_BODY_LAYER = 1;
    private static AvatarMask lowerBodyMask;

    private static AvatarMask GetOrCreateLowerBodyMask()
    {
        if (lowerBodyMask != null)
            return lowerBodyMask;

        lowerBodyMask = new AvatarMask();

        // Desactivar todo
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            lowerBodyMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

        // Només cames
        lowerBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, true);
        lowerBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, true);
        lowerBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, true);

        return lowerBodyMask;
    }

    public void AddAnimator()
    {
        Anim = ModelObject.GetComponent<Animator>() ?? ModelObject.AddComponent<Animator>();

        var animEvents = ModelObject.GetComponent<CharacterAnimEvents>();

        if (animEvents == null)
            animEvents = ModelObject.AddComponent<CharacterAnimEvents>();

        animEvents.character = this; // asignar la referencia

        var controller = new AnimatorController();

        // =========================
        // BASE LAYER (FULL BODY)
        // =========================
        controller.AddLayer("Base Layer");
        var baseSM = controller.layers[BASE_LAYER].stateMachine;

        var empty = baseSM.AddState("Empty");
        empty.motion = null;
        empty.tag = "Idle";
        baseSM.defaultState = empty;

        // =========================
        // LOWER BODY IDLE LAYER
        // =========================
        var lowerLayer = new AnimatorControllerLayer
        {
            name = "LowerBodyIdle",
            blendingMode = AnimatorLayerBlendingMode.Override,
            defaultWeight = 0f, // off initially
            avatarMask = GetOrCreateLowerBodyMask(),
            stateMachine = new AnimatorStateMachine()
        };

        controller.AddLayer(lowerLayer);

        var idleClip = Resources.Load<AnimationClip>($"Animations/Accelerated/{Id.ToLower()}_idle");
        if (idleClip != null)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(idleClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idleClip, settings);
        }
        var idleState = lowerLayer.stateMachine.AddState("IdleLower");
        idleState.motion = idleClip;
        lowerLayer.stateMachine.defaultState = idleState;

        Anim.runtimeAnimatorController = controller;
        //Anim.avatar = CharacterAvatar;

        Anim.applyRootMotion = true;
        Anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Ensure layer weight starts at 1
        Anim.SetLayerWeight(LOWER_BODY_LAYER, 0f);
    }

    public void AddAnimation(AnimationClip animClip)
    {
        if (animClip == null || Anim == null) return;

        // Crear evento que llame a EnableLowerBodyIdleEvent de CharacterAnimEvents
        AnimationEvent evt = new AnimationEvent
        {
            functionName = "EnableLowerBodyIdleEvent",
            time = animClip.length
        };

        // Limpiar eventos previos
        var existingEvents = animClip.events;
        animClip.events = System.Array.FindAll(existingEvents, e => e.functionName != "EnableLowerBodyIdleEvent");

        // Añadir el nuevo evento
        animClip.AddEvent(evt);

        // Añadir al AnimatorController
        var controller = Anim.runtimeAnimatorController as AnimatorController;
        var stateMachine = controller.layers[BASE_LAYER].stateMachine;

        var state = stateMachine.AddState(animClip.name);
        state.motion = animClip;

        Debug.Log($"Added animation '{animClip.name}' with LowerBodyIdleEvent to '{Id}'");
    }

    public void PlayAnimation(string animation)
    {
        if (Anim == null) return;

        // Desactivar LowerBodyIdle mientras se reproduce otra animación
        DisableLowerBodyIdle();

        // Reproducir animación full-body en BaseLayer
        Anim.Play(animation, BASE_LAYER, 0f);
    }


    public void EnableLowerBodyIdle()
    {
        if (Anim == null) return;
        Anim.SetLayerWeight(LOWER_BODY_LAYER, 1f);
    }

    public void DisableLowerBodyIdle()
    {
        if (Anim == null) return;
        Anim.SetLayerWeight(LOWER_BODY_LAYER, 0f);
    }


    public bool HasAnimation(string animationName)
    {
        var controller = Anim.runtimeAnimatorController as AnimatorController;
        if (controller == null) return false;

        var stateMachine = controller.layers[0].stateMachine;
        return stateMachine.states.Any(s => s.state.name == animationName);
    }

    // Transform Handling
    public void UpdateTransform(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
    {
        if (ModelObject == null) return;
        ModelObject.transform.position = newPosition;
        ModelObject.transform.rotation = newRotation;
        ModelObject.transform.localScale = newScale;
    }

    // Hand / Object Interaction
    public Transform GetLeftHandMiddle()
    {
        if (Anim != null && Anim.isHuman && Anim.avatar.isValid)
            return Anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        return null;
    }

    public Transform GetRightHandMiddle()
    {
        if (Anim != null && Anim.isHuman && Anim.avatar.isValid)
            return Anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        return null;
    }

public Transform GetRightHand()
    {
        if (Anim != null && Anim.isHuman && Anim.avatar.isValid)
            // HumanBodyBones.RightHand és el canell, pare de TOTS els dits
            return Anim.GetBoneTransform(HumanBodyBones.RightHand);
        return null;
    }

    public Transform GetLeftHand()
    {
        if (Anim != null && Anim.isHuman && Anim.avatar.isValid)
            return Anim.GetBoneTransform(HumanBodyBones.LeftHand);
        return null;
    }

    public void DropLeftHand()
    {
        if (ObjectInLeftHand == null) return;
        ObjectInLeftHand.ModelObject.transform.SetParent(null);
        ObjectInLeftHand.SetPhysicsActive(true);
        DestroyTwoHandAnchor();
        ObjectInLeftHand = null;
    }

    public void DropRightHand()
    {
        if (ObjectInRightHand == null) return;
        ObjectInRightHand.ModelObject.transform.SetParent(null);
        ObjectInRightHand.SetPhysicsActive(true);
        DestroyTwoHandAnchor();
        ObjectInRightHand = null;
    }

    public void AttachTwoHandedObject(Object obj)
    {
        if (twoHandAnchor == null)
        {
            twoHandAnchor = new GameObject($"TwoHandAnchor_{Id}");
            var updater = twoHandAnchor.AddComponent<TwoHandedAnchorUpdater>();
            updater.leftHand = GetLeftHand();
            updater.rightHand = GetRightHand();
        }

        var scale = new Vector3(obj.Scale[0], obj.Scale[1], obj.Scale[2]);
        obj.ModelObject.transform.SetParent(twoHandAnchor.transform);
        obj.ModelObject.transform.localPosition = Vector3.zero;
        obj.ModelObject.transform.localRotation = Quaternion.identity;
        obj.ModelObject.transform.localScale = scale != Vector3.zero ? scale : Vector3.one;

        ObjectInLeftHand = obj;
        ObjectInRightHand = obj;
    }

    public void DestroyTwoHandAnchor()
    {
        if (twoHandAnchor == null) return;
        GameObject.Destroy(twoHandAnchor);
        twoHandAnchor = null;
        ObjectInLeftHand = null;
        ObjectInRightHand = null;
    }
}


