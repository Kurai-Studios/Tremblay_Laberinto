using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    private PlayerInputReader input;
    private IInteractable currentInteractable;

    public void Init(PlayerInputReader inputReader)
    {
        input = inputReader;
        input.OnInteractPressed += HandleInteractPressed;
    }

    private void HandleInteractPressed()
    {
        if (currentInteractable == null) return;
        if (!currentInteractable.CanInteract(gameObject)) return;

        currentInteractable.Interact(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
            currentInteractable = interactable;
    }

    void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && ReferenceEquals(interactable, currentInteractable))
            currentInteractable = null;
    }

    void OnDestroy()
    {
        if (input != null)
            input.OnInteractPressed -= HandleInteractPressed;
    }
}
