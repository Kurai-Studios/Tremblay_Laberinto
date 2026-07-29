using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InventarioToggleController : MonoBehaviour
{
    public event Action<bool> OnVisibilityChanged;
    [Header("Referencias")]
    [SerializeField] private Button botonToggle;
    [SerializeField] private GameObject panelInventario;
    [SerializeField] private TextMeshProUGUI textoBoton;
    
    [Header("Animación")]
    [SerializeField] private float duracionAnimacion = 0.4f;
    [SerializeField] private AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float alturaPanel = 400f;
    
    [Header("Configuración Visual")]
    [SerializeField] private string textoMostrar = "Mostrar Inventario";
    [SerializeField] private string textoOcultar = "Ocultar Inventario";
    
    [Header("Posición del Slide")]
    [SerializeField] private TipoSlide tipoSlide = TipoSlide.AbajoArriba;

    // 🔗 Control de canvas de objetos
    [Header("🔗 Integración con Canvas de Objetos")]
    [SerializeField] private bool ocultarCanvasObjetos = true;
    [Tooltip("Si está activo, oculta el canvas de ObjectInfoUIManager al abrir el inventario")]

    // 👆 Detección de Gestos Swipe
    [Header("👆 Gestos de Swipe")]
    [SerializeField] private bool habilitarSwipe = true;
    [Tooltip("Permite abrir/cerrar el panel deslizando el botón")]
    
    [SerializeField] private float distanciaMinSwipe = 50f;
    [Tooltip("Distancia mínima en píxeles para considerar un swipe")]
    
    [SerializeField] private float tiempoMaxSwipe = 0.5f;
    [Tooltip("Tiempo máximo en segundos para considerar un swipe")]
    
    [SerializeField] private bool invertirSwipe = false;
    [Tooltip("Invertir dirección: swipe arriba cierra, swipe abajo abre")]
    
    [SerializeField] private bool mostrarDebugSwipe = false;

    // 🔄 NUEVO: Rotación de Flecha
    [Header("🔄 Rotación de Flecha")]
    [SerializeField] private bool habilitarRotacionFlecha = true;
    [Tooltip("Anima la rotación de una imagen/icono en el botón")]
    
    [SerializeField] private RectTransform imagenFlecha;
    [Tooltip("La imagen que rotará (puede ser hijo del botón o el botón mismo)")]
    
    [SerializeField] private float rotacionCerrado = 0f;
    [Tooltip("Rotación cuando el panel está cerrado (ej: 0° = flecha arriba)")]
    
    [SerializeField] private float rotacionAbierto = 180f;
    [Tooltip("Rotación cuando el panel está abierto (ej: 180° = flecha abajo)")]
    
    [SerializeField] private float duracionRotacion = 0.3f;
    [Tooltip("Duración de la animación de rotación")]
    
    [SerializeField] private AnimationCurve curvaRotacion = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [SerializeField] private bool usarMismaDuracionQuePanel = true;
    [Tooltip("Usar la misma duración de animación que el panel")]

    private bool panelVisible = false;
    private RectTransform rectTransformPanel;
    private CanvasGroup canvasGroupPanel;
    private Coroutine animacionActual;
    private Coroutine rotacionActual;
    
    private Vector2 posicionVisible;
    private Vector2 posicionOculta;

    // Variables para detección de swipe
    private Vector2 posicionInicioSwipe;
    private float tiempoInicioSwipe;
    private bool swipeEnProgreso = false;

    public enum TipoSlide
    {
        ArribaAbajo,
        AbajoArriba,
        IzquierdaDerecha,
        DerechaIzquierda
    }

    void Start()
    {
        InicializarComponentes();
        ConfigurarBotonToggle();
        CalcularPosiciones();
        ConfigurarEstadoInicial();
        
        // Configurar Input System para swipe
        if (habilitarSwipe)
        {
            ConfigurarInputSwipe();
        }

        // Configurar flecha
        if (habilitarRotacionFlecha)
        {
            ConfigurarFlecha();
        }
    }

    private void ConfigurarInputSwipe()
    {
        // Detección vía Touchscreen.current en Update — no requiere InputAction
    }

    private void ConfigurarFlecha()
    {
        // Si no se asignó flecha, intentar buscarla automáticamente
        if (imagenFlecha == null && botonToggle != null)
        {
            // Buscar en hijos del botón una imagen llamada "Flecha", "Arrow", "Icono", etc.
            Transform[] hijos = botonToggle.GetComponentsInChildren<Transform>(true);
            foreach (Transform hijo in hijos)
            {
                if (hijo.name.ToLower().Contains("flecha") || 
                    hijo.name.ToLower().Contains("arrow") ||
                    hijo.name.ToLower().Contains("icono") ||
                    hijo.name.ToLower().Contains("icon"))
                {
                    imagenFlecha = hijo.GetComponent<RectTransform>();
                    if (imagenFlecha != null)
                    {
                        Debug.Log($"[InventarioToggle] Flecha encontrada automáticamente: {hijo.name}");
                        break;
                    }
                }
            }

            // Si no se encontró, usar el primer componente Image
            if (imagenFlecha == null)
            {
                Image imagen = botonToggle.GetComponentInChildren<Image>();
                if (imagen != null)
                {
                    imagenFlecha = imagen.GetComponent<RectTransform>();
                    Debug.Log($"[InventarioToggle] Usando primera imagen como flecha: {imagen.name}");
                }
            }
        }

        // Establecer rotación inicial
        if (imagenFlecha != null)
        {
            imagenFlecha.localRotation = Quaternion.Euler(0, 0, rotacionCerrado);
            Debug.Log($"[InventarioToggle] Flecha configurada - Rotación inicial: {rotacionCerrado}°");
        }
        else
        {
            Debug.LogWarning("[InventarioToggle] No se encontró imagen para rotar. Asigna 'Imagen Flecha' en el Inspector.");
        }
    }

    private void InicializarComponentes()
    {
        if (panelInventario == null)
        {
            panelInventario = GameObject.Find("Inventario");
            if (panelInventario == null)
            {
                Debug.LogError("[InventarioToggle] No se encontró el panel 'Inventario' en la escena");
                return;
            }
        }

        rectTransformPanel = panelInventario.GetComponent<RectTransform>();
        if (rectTransformPanel == null)
        {
            Debug.LogError("[InventarioToggle] El panel Inventario no tiene RectTransform");
            return;
        }

        canvasGroupPanel = panelInventario.GetComponent<CanvasGroup>();
        if (canvasGroupPanel == null)
        {
            canvasGroupPanel = panelInventario.AddComponent<CanvasGroup>();
        }

        if (textoBoton == null && botonToggle != null)
        {
            textoBoton = botonToggle.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void CalcularPosiciones()
    {
        if (rectTransformPanel == null) return;

        posicionVisible = rectTransformPanel.anchoredPosition;
        
        switch (tipoSlide)
        {
            case TipoSlide.ArribaAbajo:
                posicionOculta = new Vector2(posicionVisible.x, posicionVisible.y + alturaPanel);
                break;
            case TipoSlide.AbajoArriba:
                posicionOculta = new Vector2(posicionVisible.x, posicionVisible.y - alturaPanel);
                break;
            case TipoSlide.IzquierdaDerecha:
                posicionOculta = new Vector2(posicionVisible.x - rectTransformPanel.sizeDelta.x, posicionVisible.y);
                break;
            case TipoSlide.DerechaIzquierda:
                posicionOculta = new Vector2(posicionVisible.x + rectTransformPanel.sizeDelta.x, posicionVisible.y);
                break;
        }
        
        Debug.Log($"[InventarioToggle] Posición visible: {posicionVisible}, Posición oculta: {posicionOculta}");
    }

    private void ConfigurarBotonToggle()
    {
        if (botonToggle != null)
        {
            botonToggle.onClick.RemoveAllListeners();
            botonToggle.onClick.AddListener(TogglePanel);
        }
        else
        {
            Debug.LogWarning("[InventarioToggle] Botón toggle no asignado");
        }
    }

    private void ConfigurarEstadoInicial()
    {
        panelVisible = false;
        
        if (rectTransformPanel != null)
        {
            rectTransformPanel.anchoredPosition = posicionOculta;
        }
        
        if (canvasGroupPanel != null)
        {
            canvasGroupPanel.alpha = 1f;
            canvasGroupPanel.interactable = true;
            canvasGroupPanel.blocksRaycasts = true;
        }
        
        ActualizarTextoBoton();
    }

    void Update()
    {
        if (!habilitarSwipe) return;

        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        var touch = touchscreen.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            Vector2 pos = touch.position.ReadValue();
            if (EstaEnZonaSwipe(pos))
            {
                posicionInicioSwipe = pos;
                tiempoInicioSwipe = Time.time;
                swipeEnProgreso = true;

                if (mostrarDebugSwipe)
                    Debug.Log($"[InventarioSwipe] Swipe iniciado en: {pos}");
            }
        }
        else if (touch.press.wasReleasedThisFrame && swipeEnProgreso)
        {
            Vector2 pos = touch.position.ReadValue();
            float tiempoTranscurrido = Time.time - tiempoInicioSwipe;
            Vector2 direccion = pos - posicionInicioSwipe;

            if (mostrarDebugSwipe)
                Debug.Log($"[InventarioSwipe] Swipe finalizado - Distancia: {direccion.magnitude}px, Tiempo: {tiempoTranscurrido}s");

            if (direccion.magnitude >= distanciaMinSwipe && tiempoTranscurrido <= tiempoMaxSwipe)
                ProcesarSwipe(direccion);

            swipeEnProgreso = false;
        }
    }

    private bool EstaEnZonaSwipe(Vector2 posicionPantalla)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;

        // Zona del botón
        if (botonToggle != null)
        {
            RectTransform btnRect = botonToggle.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(btnRect, posicionPantalla, cam))
                return true;
        }

        // Zona del panel completo (abierto o cerrado — cubre el handle visible)
        if (rectTransformPanel != null)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransformPanel, posicionPantalla, cam))
                return true;
        }

        return false;
    }

    private void ProcesarSwipe(Vector2 direccion)
    {
        // Determinar si el swipe es más vertical que horizontal
        bool esVertical = Mathf.Abs(direccion.y) > Mathf.Abs(direccion.x);
        
        if (!esVertical)
        {
            if (mostrarDebugSwipe)
                Debug.Log("[InventarioSwipe] Swipe horizontal ignorado");
            return;
        }

        bool esHaciaArriba = direccion.y > 0;
        bool debeAbrir = invertirSwipe ? !esHaciaArriba : esHaciaArriba;

        if (mostrarDebugSwipe)
        {
            Debug.Log($"[InventarioSwipe] Swipe {(esHaciaArriba ? "ARRIBA ⬆️" : "ABAJO ⬇️")} detectado");
            Debug.Log($"[InventarioSwipe] Acción: {(debeAbrir ? "ABRIR" : "CERRAR")} panel");
        }

        // Ejecutar acción solo si el estado es diferente
        if (debeAbrir && !panelVisible)
        {
            MostrarPanel();
        }
        else if (!debeAbrir && panelVisible)
        {
            OcultarPanel();
        }
        else
        {
            if (mostrarDebugSwipe)
                Debug.Log($"[InventarioSwipe] Panel ya está {(panelVisible ? "abierto" : "cerrado")}");
        }
    }

public void TogglePanel()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }

        panelVisible = !panelVisible;
        OnVisibilityChanged?.Invoke(panelVisible);

        // 🎵 Sonido
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoTogglePanel();
        }

        // 🔗 Controlar canvas de objetos
        if (ocultarCanvasObjetos && ObjectInfoUIManager.Instance != null)
        {
            if (panelVisible)
            {
                ObjectInfoUIManager.Instance.OcultarCanvasTemporalmente();
                Debug.Log("[InventarioToggle] Canvas de objetos ocultado");
            }
            else
            {
                ObjectInfoUIManager.Instance.MostrarCanvasTemporalmente();
                Debug.Log("[InventarioToggle] Canvas de objetos mostrado");
            }
        }
        
        // 🔄 Animar flecha
        if (habilitarRotacionFlecha && imagenFlecha != null)
        {
            AnimarRotacionFlecha(panelVisible);
        }

        animacionActual = StartCoroutine(AnimarSlidePanel(panelVisible));
        ActualizarTextoBoton();
        
        Debug.Log($"[InventarioToggle] Panel {(panelVisible ? "mostrándose" : "ocultándose")}");
    }

    // 🔄 NUEVO: Animar rotación de la flecha
    private void AnimarRotacionFlecha(bool abrir)
    {
        if (rotacionActual != null)
        {
            StopCoroutine(rotacionActual);
        }

        float duracion = usarMismaDuracionQuePanel ? duracionAnimacion : duracionRotacion;
        rotacionActual = StartCoroutine(AnimarRotacion(abrir, duracion));
    }

    private IEnumerator AnimarRotacion(bool abrir, float duracion)
    {
        float rotacionInicial = imagenFlecha.localEulerAngles.z;
        float rotacionFinal = abrir ? rotacionAbierto : rotacionCerrado;
        
        // Normalizar ángulos para evitar rotaciones raras
        if (Mathf.Abs(rotacionInicial - rotacionFinal) > 180f)
        {
            if (rotacionInicial > rotacionFinal)
                rotacionInicial -= 360f;
            else
                rotacionFinal -= 360f;
        }

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / duracion;
            float curveValue = curvaRotacion.Evaluate(progreso);

            float rotacionActualZ = Mathf.Lerp(rotacionInicial, rotacionFinal, curveValue);
            imagenFlecha.localRotation = Quaternion.Euler(0, 0, rotacionActualZ);

            yield return null;
        }

        // Asegurar rotación final exacta
        imagenFlecha.localRotation = Quaternion.Euler(0, 0, rotacionFinal);
        rotacionActual = null;
    }

    private IEnumerator AnimarSlidePanel(bool mostrar)
    {
        float tiempoTranscurrido = 0f;
        
        Vector2 posicionInicial = mostrar ? posicionOculta : posicionVisible;
        Vector2 posicionFinal = mostrar ? posicionVisible : posicionOculta;

        while (tiempoTranscurrido < duracionAnimacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / duracionAnimacion;
            float curveValue = curvaAnimacion.Evaluate(progreso);

            if (rectTransformPanel != null)
            {
                rectTransformPanel.anchoredPosition = Vector2.Lerp(posicionInicial, posicionFinal, curveValue);
            }

            yield return null;
        }

        if (rectTransformPanel != null)
        {
            rectTransformPanel.anchoredPosition = posicionFinal;
        }

        animacionActual = null;
    }

    private void ActualizarTextoBoton()
    {
        if (textoBoton != null)
        {
            textoBoton.text = panelVisible ? textoOcultar : textoMostrar;
        }
    }

    // Métodos públicos para control externo
    public void MostrarPanel()
    {
        if (!panelVisible)
        {
            TogglePanel();
        }
    }

    public void OcultarPanel()
    {
        if (panelVisible)
        {
            TogglePanel();
        }
    }

    public bool EstaPanelVisible()
    {
        return panelVisible;
    }
// ✅ OPCIONAL - Para acceso más directo (singleton pattern)
public static InventarioToggleController Instance { get; private set; }

// ✅ OPCIONAL - Método para configurar el singleton en Awake()
private void Awake()
{
    // Last-wins: ver comentario en InputRouter.Awake() — evita que esta
    // instancia nueva se autodestruya (y se lleve consigo todo el
    // GameController de la escena) cuando Instance quedó "viva" por
    // compartir GameObject con un componente DontDestroyOnLoad.
    if (Instance != null && Instance != this)
    {
        Destroy(Instance.gameObject);
    }
    Instance = this;
}
    public void RecalcularPosiciones()
    {
        CalcularPosiciones();
        if (panelVisible && rectTransformPanel != null)
        {
            rectTransformPanel.anchoredPosition = posicionVisible;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }

        if (rotacionActual != null)
        {
            StopCoroutine(rotacionActual);
        }

    }

    // Context menus para testing
    [ContextMenu("Toggle Panel")]
    public void TestToggle()
    {
        TogglePanel();
    }

    [ContextMenu("🧪 Simular Swipe Arriba")]
    public void TestSwipeArriba()
    {
        ProcesarSwipe(new Vector2(0, 100));
    }

    [ContextMenu("🧪 Simular Swipe Abajo")]
    public void TestSwipeAbajo()
    {
        ProcesarSwipe(new Vector2(0, -100));
    }

    [ContextMenu("🔄 Test Rotar Flecha")]
    public void TestRotarFlecha()
    {
        if (imagenFlecha != null)
        {
            AnimarRotacionFlecha(!panelVisible);
        }
    }

    [ContextMenu("Recalcular Posiciones")]
    public void ContextRecalcularPosiciones()
    {
        RecalcularPosiciones();
    }

    [ContextMenu("📊 Debug Estado Panel")]
    public void DebugEstadoPanel()
    {
        Debug.Log("=== 📊 ESTADO INVENTARIO TOGGLE ===");
        Debug.Log($"Panel Visible: {panelVisible}");
        Debug.Log($"Panel Activo: {panelInventario?.activeSelf}");
        Debug.Log($"CanvasGroup Alpha: {canvasGroupPanel?.alpha}");
        Debug.Log($"Interactable: {canvasGroupPanel?.interactable}");
        Debug.Log($"Blocks Raycasts: {canvasGroupPanel?.blocksRaycasts}");
        Debug.Log($"Posición Actual: {rectTransformPanel?.anchoredPosition}");
        Debug.Log($"Posición Visible: {posicionVisible}");
        Debug.Log($"Posición Oculta: {posicionOculta}");
        Debug.Log($"Ocultar Canvas Objetos: {ocultarCanvasObjetos}");
        
        // Estado de swipe
        Debug.Log($"--- SWIPE ---");
        Debug.Log($"Habilitado: {habilitarSwipe}");
        Debug.Log($"Distancia Mínima: {distanciaMinSwipe}px");
        Debug.Log($"Tiempo Máximo: {tiempoMaxSwipe}s");
        Debug.Log($"Invertir: {invertirSwipe}");

        // Estado de flecha
        Debug.Log($"--- FLECHA ---");
        Debug.Log($"Habilitado: {habilitarRotacionFlecha}");
        Debug.Log($"Imagen Flecha: {imagenFlecha?.name ?? "NULL"}");
        Debug.Log($"Rotación Actual: {imagenFlecha?.localEulerAngles.z ?? 0}°");
        Debug.Log($"Rotación Cerrado: {rotacionCerrado}°");
        Debug.Log($"Rotación Abierto: {rotacionAbierto}°");
        
        if (ObjectInfoUIManager.Instance != null)
        {
            bool tieneCanvas = ObjectInfoUIManager.Instance.TieneCanvasActivo();
            Debug.Log($"ObjectInfoUI tiene canvas activo: {tieneCanvas}");
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && rectTransformPanel != null)
        {
            CalcularPosiciones();
        }

        // Validar valores de swipe
        if (distanciaMinSwipe < 10f)
        {
            distanciaMinSwipe = 10f;
        }

        if (tiempoMaxSwipe < 0.1f)
        {
            tiempoMaxSwipe = 0.1f;
        }

        // Validar duración de rotación
        if (duracionRotacion < 0.1f)
        {
            duracionRotacion = 0.1f;
        }
    }
}