using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Singleton de input centralizado. Reemplaza los InputActions individuales
/// de cada ObjectDisplayController — UN solo raycast por tap en lugar de 24.
/// Colocar en un GameObject persistente en la escena (ej. GameManager).
/// </summary>
public class InputRouter : MonoBehaviour
{
    public static InputRouter Instance { get; private set; }

    private InputAction touchPressAction;
    private InputAction touchPositionAction;
    private InputAction mousePressAction;
    private InputAction mousePositionAction;

    private Camera mainCamera;
    private InventarioToggleController inventarioToggle;
    private MissionListUI missionListUI;
    private SettingsPanelManager settingsManager;

    // Reutilizado en el chequeo de UI para no generar basura en cada toque
    private static readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Awake()
    {
        // Last-wins: si Instance es una referencia vieja que sigue "viva" (p.ej.
        // porque comparte GameObject con un componente DontDestroyOnLoad como
        // StoryManager, lo que la mantiene fuera del ciclo normal de destrucción
        // de escena), destruirla en vez de autodestruir esta instancia nueva.
        // Con first-wins, esta instancia nueva se autodestruía y se llevaba
        // consigo TODO el GameController de la escena (incluido Vuforia/AR).
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        mainCamera = Camera.main;

        touchPressAction    = new InputAction("TouchPress",    InputActionType.Button, "<Touchscreen>/primaryTouch/press");
        touchPositionAction = new InputAction("TouchPosition", InputActionType.Value,  "<Touchscreen>/primaryTouch/position");
        mousePressAction    = new InputAction("MousePress",    InputActionType.Button, "<Mouse>/leftButton");
        mousePositionAction = new InputAction("MousePosition", InputActionType.Value,  "<Mouse>/position");

        touchPressAction.Enable();
        touchPositionAction.Enable();
        mousePressAction.Enable();
        mousePositionAction.Enable();

        touchPressAction.performed += OnTouchPress;
        mousePressAction.performed += OnMousePress;
    }

    void Start()
    {
        inventarioToggle = InventarioToggleController.Instance;
        missionListUI = FindObjectOfType<MissionListUI>();
        settingsManager = FindObjectOfType<SettingsPanelManager>();

        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();
    }

    private bool EstaInventarioAbierto()
    {
        if (inventarioToggle == null)
            inventarioToggle = InventarioToggleController.Instance;
        return inventarioToggle != null && inventarioToggle.EstaPanelVisible();
    }

    private bool EstaMisionesAbierto()
    {
        if (missionListUI == null)
            missionListUI = FindObjectOfType<MissionListUI>();
        return missionListUI != null && missionListUI.EstaPanelVisible();
    }

    private bool EstaSettingsAbierto()
    {
        if (settingsManager == null)
            settingsManager = FindObjectOfType<SettingsPanelManager>();
        return settingsManager != null && settingsManager.EstaPanelVisible();
    }

    /// <summary>
    /// ¿Hay algún elemento de UI (con Raycast Target) bajo este punto de pantalla?
    /// Fiable con el nuevo Input System (a diferencia de IsPointerOverGameObject(touchId),
    /// cuyo touchId no coincide con el pointerId interno del EventSystem y solía fallar).
    /// </summary>
    private bool EstaSobreUI(Vector2 posicionPantalla)
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = posicionPantalla };
        _uiHits.Clear();
        EventSystem.current.RaycastAll(ped, _uiHits);
        return _uiHits.Count > 0;
    }

    private void OnTouchPress(InputAction.CallbackContext ctx)
    {
        if (Touchscreen.current == null) return;
        DetectarYRutar(touchPositionAction.ReadValue<Vector2>());
    }

    private void OnMousePress(InputAction.CallbackContext ctx)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return;

        DetectarYRutar(mousePositionAction.ReadValue<Vector2>());
    }

    private void DetectarYRutar(Vector2 posicionPantalla)
    {
        // No rutear al AR si un panel (casi pantalla completa) está abierto…
        if (EstaInventarioAbierto()) return;
        if (EstaMisionesAbierto()) return;
        if (EstaSettingsAbierto()) return;

        // …ni si el dedo/cursor está sobre cualquier elemento de UI.
        if (EstaSobreUI(posicionPantalla)) return;

        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(posicionPantalla);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            hit.collider.GetComponent<ObjectDisplayController>()?.OnHit();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Guard: si este era el duplicado destruido en Awake, los campos son null
        if (touchPressAction == null) return;

        touchPressAction.performed -= OnTouchPress;
        mousePressAction.performed -= OnMousePress;

        touchPressAction.Disable();
        touchPositionAction.Disable();
        mousePressAction.Disable();
        mousePositionAction.Disable();

        touchPressAction.Dispose();
        touchPositionAction.Dispose();
        mousePressAction.Dispose();
        mousePositionAction.Dispose();
    }
}
