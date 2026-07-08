using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CursorController : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController cameraInput;
    [SerializeField] private bool autoFindCameraInput = true;

    private void Start()
    {
        if (cameraInput == null && autoFindCameraInput)
            cameraInput = FindFirstObjectByType<CinemachineInputAxisController>();

        Lock();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.altKey.wasPressedThisFrame)
            Unlock();
        else if (Keyboard.current.altKey.wasReleasedThisFrame)
            Lock();
    }

    private void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraInput != null) cameraInput.enabled = true;
    }

    private void Unlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cameraInput != null) cameraInput.enabled = false;
    }
}
