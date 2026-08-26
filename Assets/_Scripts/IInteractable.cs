using UnityEngine;

public interface IInteractable
{
    /// <summary>Whether this interactable can currently be interacted with by the given interactor.</summary>
    bool CanInteract(GameObject interactor);

    /// <summary>Perform the interaction.</summary>
    void Interact(GameObject interactor);

    /// <summary>Prompt text to show in UI, e.g. "Press E to open".</summary>
    string InteractionPrompt { get; }
}
