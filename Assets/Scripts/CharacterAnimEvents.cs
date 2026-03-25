using UnityEngine;
using UnityEngine.TextCore.Text;

/// Handles animation events for characters, such as grabbing and releasing objects
public class CharacterAnimEvents : MonoBehaviour
{
    // Called by animation events to handle object interactions
    // eventInfo format: "CharacterID|LeftObjectID|RightObjectID"

    public Character character; // asigna esto al instanciar el character

    public void GrabObject(string eventInfo)
    {
        string[] info = eventInfo.Split('|');                                    // Parse event info
        var ritualData = RitualController.Instance.GetRitualData();              // Access ritual data

        Character character = ritualData.FindCharacter(info[0]);                 // Find character
        Object objectLeft = ParseObject(info[1], ritualData);                    // Left object
        Object objectRight = ParseObject(info[2], ritualData);                   // Right object

        bool isTwoHanded = objectLeft != null && objectRight != null && objectLeft == objectRight;
        Transform leftHand = character.GetLeftHand();                            // Left hand transform
        Transform rightHand = character.GetRightHand();                          // Right hand transform

        if (isTwoHanded) HandleTwoHanded(character, objectLeft);                 // Two-handed interaction
        else
        {
            HandleSingleHand(character, objectLeft, character.GetLeftHandMiddle(), true);            // Left hand
            HandleSingleHand(character, objectRight, character.GetRightHandMiddle(), false);         // Right hand
        }
    }

    // Helper: Parse object ID
    private Object ParseObject(string id, RitualData ritualData)
    {
        if (string.IsNullOrEmpty(id)) return null;                                // Empty
        if (id == "Release") return new Object { Id = "Release" };                // Release action
        return ritualData.FindObject(id);                                         // Find object
    }

    // Two-handed object handling
    private void HandleTwoHanded(Character character, Object obj)
    {
        if (obj.Id != "Release")
        {
            obj.SetPhysicsActive(false);                                         // Disable physics
            character.AttachTwoHandedObject(obj);                                 // Attach to hands
        }
        else
        {
            character.DropLeftHand();                                            // Release left
            character.DropRightHand();                                           // Release right
        }
    }

    // Single-handed object handling
    private void HandleSingleHand(Character character, Object obj, Transform hand, bool isLeft)
    {
        if (obj == null) return;

        string handName = isLeft ? "LEFT" : "RIGHT";

        if (obj.Id != "Release")
        {
            obj.SetPhysicsActive(false);                                         // Disable physics
            if (hand != null)
            {
                AttachObjectToHand(obj, hand);                                   // Attach to hand
                if (isLeft) character.ObjectInLeftHand = obj;                    // Track left
                else character.ObjectInRightHand = obj;                           // Track right
            }
        }
        else
        {
            if (isLeft) character.DropLeftHand();                                 // Release left
            else character.DropRightHand();                                        // Release right
        }
    }

    // Helper: Attach object to hand
    private void AttachObjectToHand(Object obj, Transform hand)
    {
        Vector3 worldScale = obj.ModelObject.transform.lossyScale;               // Preserve world scale
        obj.ModelObject.transform.SetParent(hand);                               // Parent to hand
        obj.ModelObject.transform.localPosition = Vector3.zero;                  // Reset local position
        obj.ModelObject.transform.localRotation = Quaternion.identity;           // Reset local rotation

        // Adjust local scale to maintain world size
        Vector3 parentScale = hand.lossyScale;
        obj.ModelObject.transform.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );

        // Automatically determine if the hand is left or right based on the hand's local forward direction
        bool isLeftHand = Vector3.Dot(hand.transform.forward, Vector3.right) < 0;

        // Determine the correct axis to align with based on hand type
        Vector3 handAlignmentAxis = isLeftHand ? -hand.transform.right : hand.transform.right;

        // Align the object's Y-axis with the hand's axis
        Quaternion rotationToAlign = Quaternion.FromToRotation(obj.ModelObject.transform.up, handAlignmentAxis);

        // Apply the calculated rotation to the object
        obj.ModelObject.transform.rotation = rotationToAlign * obj.ModelObject.transform.rotation;
    }

    // Habilita el LowerBodyIdle cuando termina una animación full-body
    public void EnableLowerBodyIdleEvent()
    {
        // Si la referència és null, la busquem al moment
        if (character == null)
        {
            character = RitualController.Instance.GetRitualData().Characters
                        .Find(c => c.ModelObject == this.gameObject);
        }

        if (character != null)
        {
            character.EnableLowerBodyIdle();
        }
    }

}





