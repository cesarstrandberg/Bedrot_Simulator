// Anything you talk to / use with E (buyers, the bouncer...). PlayerInteraction finds it on whatever
// the crosshair hits (on the collider or any parent), shows the E prompt with PromptLabel, and calls
// Interact() on E or left-click. No "Interactable" tag needed.
public interface IInteractable
{
    string PromptLabel { get; }
    bool PromptAvailable { get; }   // false = prompt shown greyed out, Interact still called (for a reply)
    void Interact();
    void OnLookedAt(UnityEngine.Transform player);  // called every frame while targeted
}
