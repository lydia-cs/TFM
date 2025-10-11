using UnityEngine;

public class TwoHandedAnchorUpdater : MonoBehaviour
{
    public Transform leftHand;   // Reference to the character's left hand
    public Transform rightHand;  // Reference to the character's right hand

    void Update()
    {
        if (leftHand == null || rightHand == null) return; // Skip if either hand is missing

        // Place the anchor at the midpoint between both hands
        transform.position = (leftHand.position + rightHand.position) * 0.5f;

        // Rotate so Z-axis points from left hand to right hand
        Vector3 direction = (rightHand.position - leftHand.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}




