using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class FlashlightDirectInput : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Light flashlightLight; // Drag your Light component here

    private void Update()
    {
        // 1. Ensure a keyboard is physically connected to the device
        if (Keyboard.current == null) return;

        // 2. Check if the F key was pressed down exactly during this frame
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleFlashlight();
        }
    }

    private void ToggleFlashlight()
    {
        if (flashlightLight != null)
        {
            // Invert the current enabled state of the light
            flashlightLight.enabled = !flashlightLight.enabled;
        }
    }
}
