using UnityEngine;

// The button inside the elevator (Toggle: go to the other floor) and the call buttons out in the
// stairwell on each floor (CallUp/CallDown: bring the elevator to this floor).
// Clicked through PlayerInteraction's crosshair raycast (tag the collider "Interactable"), not
// OnMouseDown - the inside button is a child of the elevator's Rigidbody, and Unity delivers mouse
// events for a Rigidbody's colliders to the Rigidbody's GameObject instead of the button.
public class ElevatorButton : MonoBehaviour
{
    // CallHere: calls it to whichever floor this button is on right now - for the call panel painted
    // on the cab's own door frame, which rides along (so in practice the cab is already there).
    public enum Mode { Toggle, CallUp, CallDown, CallHere }

    public Elevator elevator;
    public Mode mode = Mode.Toggle;

    [Header("Ljud")]
    public AudioSource pressSound;

    public void Press()
    {
        if (pressSound != null) pressSound.Play();

        // Busy (moving, or an NPC is calling/riding it) - the click still clicks, nothing happens.
        if (elevator == null || elevator.IsMoving || elevator.IsReserved) return;

        switch (mode)
        {
            case Mode.Toggle: elevator.Toggle(); break;
            case Mode.CallUp: elevator.GoUp(); break;     // already up -> nothing to do
            case Mode.CallDown: elevator.GoDown(); break;
            case Mode.CallHere:
                if (elevator.IsButtonOnUpperFloor(transform.position)) elevator.GoUp(); else elevator.GoDown();
                break;
        }
    }
}
