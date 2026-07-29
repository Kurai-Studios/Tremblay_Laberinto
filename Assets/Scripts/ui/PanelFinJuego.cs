using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 🏁 Panel de fin de juego. Aparece (modal) cuando TODAS las misiones del cuento están completadas,
/// y también al volver a entrar si ya estaba todo completo (el estado completado se guarda, así que
/// al cargar la detección lo ve). El botón "Play Again" reinicia todo el progreso reutilizando
/// ResetGameController.EjecutarResetCompleto(); el panel se oculta solo al reevaluar.
///
/// Controla el panel por REFERENCIAS (CanvasGroup), no por su propio gameObject, así que puede vivir
/// en cualquier GameObject (p. ej. el GameController central).
/// </summary>
public class PanelFinJuego : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("CanvasGroup del panel modal (cubre y bloquea la pantalla).")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Button botonJugarDeNuevo;
    [Tooltip("Si se deja vacío, se busca en la escena.")]
    [SerializeField] private ResetGameController resetController;

    [Header("Volver a selección de cuentos")]
    [SerializeField] private Button botonSeleccionCuentos;
    [Tooltip("Escena del selector de cuentos (debe estar en Build Settings).")]
    [SerializeField] private string nombreEscenaSeleccion = "Inicio";

    [Header("Animación de aparición (opcional)")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private bool usarPop = true;
    [SerializeField] private float duracionPop = 0.3f;
    [SerializeField] private float escalaInicial = 0.7f;
    [SerializeField] private float escalaPico = 1.05f;
    [SerializeField] private AnimationCurve curvaPop = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private bool sonarAlAparecer = true;

    [Header("Coordinación con la narración")]
    [Tooltip("Margen (s) para dejar que ARRANQUE la narración/animación final antes de mostrar el " +
             "panel. Si el fragmento final tarda más en empezar (ej. un timeline largo), súbelo.")]
    [SerializeField] private float tiempoEsperaNarracion = 2.5f;

    private bool visible = false;
    private Vector3 escalaBase = Vector3.one;
    private Coroutine popCR;
    private Coroutine mostrarCR;
    private MissionManager missionManager;

    void Awake()
    {
        if (panelRect != null) escalaBase = panelRect.localScale;
    }

    void Start()
    {
        if (resetController == null) resetController = FindObjectOfType<ResetGameController>();

        if (botonJugarDeNuevo != null)
        {
            botonJugarDeNuevo.onClick.RemoveAllListeners();
            botonJugarDeNuevo.onClick.AddListener(OnJugarDeNuevo);
        }

        if (botonSeleccionCuentos != null)
        {
            botonSeleccionCuentos.onClick.RemoveAllListeners();
            botonSeleccionCuentos.onClick.AddListener(OnIrASeleccionCuentos);
        }

        OcultarInmediato();

        missionManager = MissionManager.Instance;
        if (missionManager != null)
            missionManager.OnMisionesActualizadas += Reevaluar;

        Reevaluar(); // por si ya está todo completo al entrar
    }

    // Se llama cada vez que cambian las misiones (incluye la carga inicial y el reset).
    private void Reevaluar()
    {
        if (missionManager == null) missionManager = MissionManager.Instance;

        bool completo = missionManager != null && missionManager.TodasMisionesCompletadas();

        if (completo && !visible)
        {
            // No mostrar de golpe: esperar a que termine el fragmento de historia final
            // (al completar la última misión se dispara a la vez la narración).
            if (mostrarCR == null)
                mostrarCR = StartCoroutine(EsperarNarracionYMostrar());
        }
        else if (!completo)
        {
            // Dejó de estar completo (p. ej. reset): cancelar espera pendiente y ocultar.
            if (mostrarCR != null) { StopCoroutine(mostrarCR); mostrarCR = null; }
            if (visible) Ocultar();
        }
    }

    private IEnumerator EsperarNarracionYMostrar()
    {
        // 1) Margen para que ARRANQUE una posible narración final (se reproduce con retardo).
        float t = 0f;
        while (t < tiempoEsperaNarracion)
        {
            if (StoryManager.Instance != null && StoryManager.Instance.EstaReproduciendo) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 2) Si hay narración en curso, esperar a que termine.
        while (StoryManager.Instance != null && StoryManager.Instance.EstaReproduciendo)
            yield return null;

        mostrarCR = null;

        // 3) Confirmar que sigue completo (no se reinició durante la espera) y mostrar.
        if (missionManager != null && missionManager.TodasMisionesCompletadas() && !visible)
            Mostrar();
    }

    private void Mostrar()
    {
        visible = true;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true; // modal: bloquea el juego detrás
        }

        if (usarPop && panelRect != null)
        {
            if (popCR != null) StopCoroutine(popCR);
            panelRect.localScale = escalaBase * escalaInicial;
            popCR = StartCoroutine(PopCoroutine());
        }

        if (sonarAlAparecer && GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoExitoGeneral();
    }

    private void Ocultar()
    {
        visible = false;
        OcultarInmediato();
    }

    private void OcultarInmediato()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
        if (panelRect != null) panelRect.localScale = escalaBase;
    }

    private void OnJugarDeNuevo()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();

        // Ocultar ya (además el reset dispara OnMisionesActualizadas → Reevaluar lo confirmará).
        Ocultar();

        if (resetController != null)
            resetController.EjecutarResetCompleto();
        else
            Debug.LogWarning("[PanelFinJuego] ResetGameController no asignado; no se pudo reiniciar.");
    }

    private void OnIrASeleccionCuentos()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();

        if (string.IsNullOrEmpty(nombreEscenaSeleccion))
        {
            Debug.LogWarning("[PanelFinJuego] nombreEscenaSeleccion vacío; no se pudo cambiar de escena.");
            return;
        }

        SceneManager.LoadScene(nombreEscenaSeleccion);
    }

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

    void OnDestroy()
    {
        if (missionManager != null)
            missionManager.OnMisionesActualizadas -= Reevaluar;
    }
}
