using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 🔍 Panel de información de un item del inventario (compartido, uno solo en el Canvas).
/// Muestra el sprite del objeto, su nombre en español y un botón que reproduce el audio del nombre.
/// Aparece con un "pop", reproduce el audio al abrir, y se cierra por tiempo, con el botón X, o
/// deslizando horizontalmente (swipe) sobre el panel.
///
/// Lo dispara ScrollViewLoader desde el botoncito "BotonInfo" de cada item encontrado, llamando a
/// ItemInfoPanel.Instance.Mostrar(data).
/// </summary>
public class ItemInfoPanel : MonoBehaviour
{
    public static ItemInfoPanel Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private CanvasGroup canvasGroup;      // en la raíz del panel (mostrar/ocultar)
    [SerializeField] private RectTransform panelRect;      // raíz del panel (se escala en el pop / zona de swipe)
    [SerializeField] private Image imagenSprite;
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private Button botonAudio;
    [SerializeField] private Button botonCerrar;

    [Header("Audio (nombre en español)")]
    [Tooltip("Controller que reproduce el audio del nombre. Debe tener idioma = Español.")]
    [SerializeField] private ObjectInfoAudioController audioController;

    [Header("Auto-cierre")]
    [Tooltip("Segundos antes de cerrarse solo. 0 = no se cierra por tiempo.")]
    [SerializeField] private float tiempoAutoCierre = 4f;

    [Header("Animación de aparición (pop)")]
    [SerializeField] private bool usarPop = true;
    [SerializeField] private float duracionPop = 0.25f;
    [Tooltip("Escala desde la que arranca (0.6 = empieza pequeño).")]
    [SerializeField] private float escalaInicial = 0.6f;
    [Tooltip("Escala máxima del rebote antes de asentarse (1.08 = un pelín más grande).")]
    [SerializeField] private float escalaPico = 1.08f;
    [SerializeField] private AnimationCurve curvaPop = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Cerrar con swipe horizontal")]
    [SerializeField] private bool cerrarConSwipe = true;
    [Tooltip("Distancia mínima en píxeles para considerar un swipe.")]
    [SerializeField] private float distanciaMinSwipe = 60f;
    [Tooltip("Tiempo máximo en segundos para considerar un swipe.")]
    [SerializeField] private float tiempoMaxSwipe = 0.6f;

    [Header("Animación de salida (al cerrar con swipe)")]
    [SerializeField] private float duracionCierreSwipe = 0.25f;
    [Tooltip("Cuánto se desplaza el panel hacia el lado al salir (px, espacio del panel).")]
    [SerializeField] private float distanciaSalida = 700f;
    [SerializeField] private AnimationCurve curvaCierre = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private GameObjectData dataActual;
    private Coroutine autoCierreCR;
    private Coroutine popCR;
    private Coroutine cierreCR;
    private Vector3 escalaBase = Vector3.one;
    private Vector2 posicionBase;
    private bool panelAbierto = false;

    // Estado del gesto de swipe
    private Vector2 inicioSwipe;
    private float tiempoInicioSwipe;
    private bool swipeEnCurso = false;

    private void Awake()
    {
        // Singleton simple por escena.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Si no se asignó panelRect, usar el RectTransform del propio panel (el del CanvasGroup).
        if (panelRect == null && canvasGroup != null)
            panelRect = canvasGroup.GetComponent<RectTransform>();

        if (panelRect == null)
            Debug.LogWarning("[ItemInfoPanel] 'panelRect' no asignado y no se pudo deducir: el pop y el deslizamiento al cerrar no funcionarán (solo el fade).");

        if (panelRect != null)
        {
            escalaBase = panelRect.localScale;
            posicionBase = panelRect.anchoredPosition;
        }

        if (botonAudio != null)
        {
            botonAudio.onClick.RemoveAllListeners();
            botonAudio.onClick.AddListener(ReproducirAudio);
        }

        if (botonCerrar != null)
        {
            botonCerrar.onClick.RemoveAllListeners();
            botonCerrar.onClick.AddListener(Cerrar);
        }

        OcultarInmediato();
    }

    /// <summary>
    /// Rellena el panel con los datos del objeto y lo muestra. Reinicia el temporizador de cierre.
    /// </summary>
    public void Mostrar(GameObjectData data)
    {
        if (data == null) return;

        dataActual = data;

        // Sprite
        if (imagenSprite != null)
        {
            Sprite sprite = string.IsNullOrEmpty(data.sprite2DPath)
                ? null
                : Resources.Load<Sprite>(data.sprite2DPath);

            imagenSprite.sprite = sprite;
            imagenSprite.enabled = sprite != null;
        }

        // Nombre en español
        if (textoNombre != null)
            textoNombre.text = data.nombreEspanol;

        MostrarPanel();

        // ✨ Animación pop
        if (usarPop && panelRect != null)
        {
            if (popCR != null) StopCoroutine(popCR);
            panelRect.localScale = escalaBase * escalaInicial;
            popCR = StartCoroutine(PopCoroutine());
        }

        // 🔊 Reproducir el nombre automáticamente al abrir (además del botón de audio)
        ReproducirAudio();

        // Reiniciar auto-cierre
        if (autoCierreCR != null) StopCoroutine(autoCierreCR);
        if (tiempoAutoCierre > 0f)
            autoCierreCR = StartCoroutine(AutoCierreCoroutine());
    }

    public void Cerrar()
    {
        if (autoCierreCR != null)
        {
            StopCoroutine(autoCierreCR);
            autoCierreCR = null;
        }
        OcultarInmediato();
    }

    private void ReproducirAudio()
    {
        if (audioController != null && dataActual != null)
            audioController.ReproducirAudioNombre(dataActual);
    }

    private IEnumerator AutoCierreCoroutine()
    {
        yield return new WaitForSeconds(tiempoAutoCierre);
        autoCierreCR = null;
        OcultarInmediato();
    }

    // ✨ Pop: escala inicial → pico → base (overshoot suave).
    private IEnumerator PopCoroutine()
    {
        float subida = Mathf.Max(0.01f, duracionPop * 0.6f);
        float bajada = Mathf.Max(0.01f, duracionPop * 0.4f);

        Vector3 desde = escalaBase * escalaInicial;
        Vector3 pico = escalaBase * escalaPico;

        float t = 0f;
        while (t < subida)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaPop.Evaluate(t / subida);
            panelRect.localScale = Vector3.Lerp(desde, pico, k);
            yield return null;
        }

        t = 0f;
        while (t < bajada)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaPop.Evaluate(t / bajada);
            panelRect.localScale = Vector3.Lerp(pico, escalaBase, k);
            yield return null;
        }

        panelRect.localScale = escalaBase;
        popCR = null;
    }

    // 👆 Detección de swipe horizontal para cerrar (Pointer = toque en móvil o ratón en editor).
    private void Update()
    {
        if (!cerrarConSwipe || !panelAbierto) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            Vector2 pos = pointer.position.ReadValue();
            if (PuntoSobrePanel(pos))
            {
                inicioSwipe = pos;
                tiempoInicioSwipe = Time.unscaledTime;
                swipeEnCurso = true;
            }
        }
        else if (pointer.press.wasReleasedThisFrame && swipeEnCurso)
        {
            swipeEnCurso = false;

            Vector2 dir = pointer.position.ReadValue() - inicioSwipe;
            float dt = Time.unscaledTime - tiempoInicioSwipe;

            bool esHorizontal = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
            if (dt <= tiempoMaxSwipe && esHorizontal && Mathf.Abs(dir.x) >= distanciaMinSwipe)
                CerrarConDeslizamiento(Mathf.Sign(dir.x)); // +1 derecha, -1 izquierda
        }
    }

    private bool PuntoSobrePanel(Vector2 screenPos)
    {
        if (panelRect == null) return true; // sin referencia, aceptar el swipe en cualquier lado

        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPos, cam);
    }

    private void MostrarPanel()
    {
        // Cancelar cualquier animación de cierre en curso y volver a la posición base
        // (por si el panel había salido deslizándose hacia un lado).
        if (cierreCR != null) { StopCoroutine(cierreCR); cierreCR = null; }
        if (panelRect != null) panelRect.anchoredPosition = posicionBase;

        // Se muestra/oculta SOLO con el CanvasGroup (no con SetActive), para que este
        // script pueda vivir en cualquier GameObject (p. ej. un GameController central).
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        panelAbierto = true;
    }

    // 👉 Cierra el panel deslizándolo hacia un lado (signo: +1 derecha, -1 izquierda) + fade out.
    private void CerrarConDeslizamiento(float signo)
    {
        if (popCR != null) { StopCoroutine(popCR); popCR = null; }
        if (autoCierreCR != null) { StopCoroutine(autoCierreCR); autoCierreCR = null; }
        if (cierreCR != null) StopCoroutine(cierreCR);

        // Dejar de escuchar swipes mientras sale
        panelAbierto = false;
        swipeEnCurso = false;

        cierreCR = StartCoroutine(CerrarDeslizandoCoroutine(signo));
    }

    private IEnumerator CerrarDeslizandoCoroutine(float signo)
    {
        Vector2 desde = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
        Vector2 hasta = desde + new Vector2(signo * distanciaSalida, 0f);
        float alphaDesde = canvasGroup != null ? canvasGroup.alpha : 1f;

        if (canvasGroup != null) canvasGroup.interactable = false;

        float t = 0f;
        while (t < duracionCierreSwipe)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaCierre.Evaluate(t / duracionCierreSwipe);
            if (panelRect != null) panelRect.anchoredPosition = Vector2.Lerp(desde, hasta, k);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(alphaDesde, 0f, k);
            yield return null;
        }

        cierreCR = null;
        OcultarInmediato();
        if (panelRect != null) panelRect.anchoredPosition = posicionBase; // listo para la próxima apertura
    }

    private void OcultarInmediato()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        panelAbierto = false;
        swipeEnCurso = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
