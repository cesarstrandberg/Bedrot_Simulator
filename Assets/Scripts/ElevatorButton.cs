using UnityEngine;

// Goes on a small collider placed over the button panel inside the elevator. Clicking it sends
// the elevator to the other stop (it starts up, so the first press takes it down).
// Clicked through PlayerInteraction's crosshair raycast (tag the collider "Interactable"), not
// OnMouseDown - the button is a child of the elevator's Rigidbody, and Unity delivers mouse
// events for a Rigidbody's colliders to the Rigidbody's GameObject instead of the button.
public class ElevatorButton : MonoBehaviour
{
    public Elevator elevator;

    [Header("Ljud")]
    public AudioSource pressSound;

    public void Press()
    {
        if (pressSound != null) pressSound.Play();

        // Busy (moving, or an NPC is calling/riding it) - the click still clicks, nothing happens.
        if (elevator == null || elevator.IsMoving || elevator.IsReserved) return;
        elevator.Toggle();
    }
}
