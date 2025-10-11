using System;
using System.Linq;
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

    // Animator Setup
    public void AddAnimator()
    {
        Anim = ModelObject.GetComponent<Animator>() ?? ModelObject.AddComponent<Animator>();        // Ensure Animator
        if (ModelObject.GetComponent<CharacterAnimEvents>() == null)                                 // Ensure events handler
            ModelObject.AddComponent<CharacterAnimEvents>();

        var controller = new AnimatorController();
        controller.AddLayer("Base Layer");

        var stateMachine = controller.layers[0].stateMachine;
        var emptyState = stateMachine.AddState("Empty");                                             // Default state
        emptyState.motion = null;
        stateMachine.defaultState = emptyState;

        Anim.avatar = CharacterAvatar;
        Anim.runtimeAnimatorController = controller;
        Anim.applyRootMotion = true;

        Debug.Log($"Animator added to character '{Id}'");
    }

    public void AddAnimation(AnimationClip animClip)
    {
        var controller = Anim.runtimeAnimatorController as AnimatorController;
        var stateMachine = controller.layers[0].stateMachine;

        var state = stateMachine.AddState(animClip.name);
        state.motion = animClip;

        Debug.Log($"Added animation '{animClip.name}' to character '{Id}'");
    }

    public void PlayAnimation(string animation) => Anim.Play(animation);

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
    public Transform GetLeftHand()
    {
        if (Anim != null && Anim.isHuman && Anim.avatar.isValid)
            return Anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        return null;
    }

    public Transform GetRightHand()
    {
        if (Anim != null && Anim.isHuman && Anim.avatar.isValid)
            return Anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
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


