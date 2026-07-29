using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SettingsPanelManager : MonoBehaviour
{
    [Header("🎛️ Control Principal")]
    [SerializeField] private Button botonToggleSettings;
    [SerializeField] private GameObject panelPrincipalSettings;
    [SerializeField] private TextMeshProUGUI textoBotonToggle;
    
    [Header("📱 Paneles de Configuración")]
    [SerializeField] private SettingsPanel[] panelesSettings;
    
    [Header("🎨 Navegación")]
    [SerializeField] private Transform contenedorBotones; // Donde van los botones de navegación
    [SerializeField] private GameObject botonNavegacionPrefab; // Prefab para botones de panel
    
    [Header("🎬 Animación")]
    [SerializeField] private float duracionAnimacion = 0.4f;
    [SerializeField] private AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private TipoSlideSettings tipoSlide = TipoSlideSettings.DerechaIzquierda;
    
    [Header("📝 Textos")]
    [SerializeField] private string textoMostrar = "⚙️ Configuración";
    [SerializeField] private string textoOcultar = "❌ Cerrar";
    
    [Header("🔊 Audio")]
    [SerializeField] private bool usarSonidos = true;

    [Header("🚪 Salir a selección de cuentos")]
    [SerializeField] private Button botonSalir;
    [Tooltip("Escena del selector de cuentos (debe estar en Build Settings).")]
    [SerializeField] private string nombreEscenaSeleccion = "Inicio";

    // Estado
    private bool panelVisible = false;
    private RectTransform rectTransformPanel;
    private CanvasGroup canvasGroupPanel;
    private Coroutine animacionActual;
    private SettingsPanel panelActivo;
    
    // Posiciones para animación
    private Vector2 posicionVisible;
    private Vector2 posicionOculta;
    
    // Botones de navegación creados dinámicamente
    private Dictionary<SettingsPanel, Button> botonesNavegacion = new Dictionary<SettingsPanel, Button>();

    public enum TipoSlideSettings
    {
        ArribaAbajo,        // Panel se desliza desde arriba
        AbajoArriba,        // Panel se desliza desde abajo
        IzquierdaDerecha,   // Panel se desliza desde izquierda
        DerechaIzquierda    // Panel se desliza desde derecha
    }

    void Start()
    {
        InicializarComponentes();
        ConfigurarBotonToggle();
        ConfigurarBotonSalir();
        CrearBotonesNavegacion();
        CalcularPosiciones();
        ConfigurarEstadoInicial();
    }

    private void ConfigurarBotonSalir()
    {
        if (botonSalir == null) return;

        botonSalir.onClick.RemoveAllListeners();
        botonSalir.onClick.AddListener(SalirASeleccionCuentos);
    }

    public void SalirASeleccionCuentos()
    {
        if (usarSonidos && GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();

        if (string.IsNullOrEmpty(nombreEscenaSeleccion))
        {
            Debug.LogWarning("[SettingsPanelManager] nombreEscenaSeleccion vacío; no se pudo cambiar de escena.");
            return;
        }

        SceneManager.LoadScene(nombreEscenaSeleccion);
    }

    private void InicializarComponentes()
    {
        if (panelPrincipalSettings == null)
        {
            Debug.LogError("[SettingsPanelManager] Panel principal de settings no asignado");
            return;
        }

        rectTransformPanel = panelPrincipalSettings.GetComponent<RectTransform>();
        if (rectTransformPanel == null)
        {
            rectTransformPanel = panelPrincipalSettings.AddComponent<RectTransform>();
        }

        canvasGroupPanel = panelPrincipalSettings.GetComponent<CanvasGroup>();
        if (canvasGroupPanel == null)
        {
            canvasGroupPanel = panelPrincipalSettings.AddComponent<CanvasGroup>();
        }

        if (textoBotonToggle == null && botonToggleSettings != null)
        {
            textoBotonToggle = botonToggleSettings.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        Debug.Log($"[SettingsPanelManager] Componentes inicializados - Paneles encontrados: {panelesSettings?.Length ?? 0}");
    }

    private void ConfigurarBotonToggle()
    {
        if (botonToggleSettings != null)
        {
            botonToggleSettings.onClick.RemoveAllListeners();
            botonToggleSettings.onClick.AddListener(ToggleSettingsPanel);
        }
        else
        {
            Debug.LogWarning("[SettingsPanelManager] Botón toggle de settings no asignado");
        }
    }

    private void CrearBotonesNavegacion()
    {
        if (contenedorBotones == null || panelesSettings == null) return;

        // Limpiar botones existentes
        foreach (Transform child in contenedorBotones)
        {
            Destroy(child.gameObject);
        }
        botonesNavegacion.Clear();

        // Crear botón para cada panel
        foreach (var panel in panelesSettings)
        {
            if (panel == null) continue;

            GameObject botonObj;
            
            if (botonNavegacionPrefab != null)
            {
                botonObj = Instantiate(botonNavegacionPrefab, contenedorBotones);
            }
            else
            {
                // Crear botón básico si no hay prefab
                botonObj = new GameObject($"Boton_{panel.nombrePanel}");
                botonObj.transform.SetParent(contenedorBotones, false);
                
                Image img = botonObj.AddComponent<Image>();
                img.color = Color.white;
                
                Button btn = botonObj.AddComponent<Button>();
                
                GameObject textoObj = new GameObject("Texto");
                textoObj.transform.SetParent(botonObj.transform, false);
                TextMeshProUGUI texto = textoObj.AddComponent<TextMeshProUGUI>();
                texto.text = panel.nombrePanel;
                texto.alignment = TextAlignmentOptions.Center;
                
                RectTransform textoRect = texto.GetComponent<RectTransform>();
                textoRect.anchorMin = Vector2.zero;
                textoRect.anchorMax = Vector2.one;
                textoRect.sizeDelta = Vector2.zero;
                textoRect.anchoredPosition = Vector2.zero;
            }

            // Configurar botón
            Button boton = botonObj.GetComponent<Button>();
            if (boton != null)
            {
                boton.onClick.RemoveAllListeners();
                boton.onClick.AddListener(() => MostrarPanel(panel));
                
                // Actualizar texto si existe
                TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
                if (textoBoton != null)
                {
                    textoBoton.text = $"{panel.iconoPanel} {panel.nombrePanel}";
                }
                
                botonesNavegacion[panel] = boton;
                
                Debug.Log($"[SettingsPanelManager] Botón creado para: {panel.nombrePanel}");
            }
        }
    }

    private void CalcularPosiciones()
    {
        if (rectTransformPanel == null) return;

        posicionVisible = rectTransformPanel.anchoredPosition;
        
        float anchoPanel = rectTransformPanel.sizeDelta.x;
        float altoPanel = rectTransformPanel.sizeDelta.y;
        
        switch (tipoSlide)
        {
            case TipoSlideSettings.ArribaAbajo:
                posicionOculta = new Vector2(posicionVisible.x, posicionVisible.y + altoPanel);
                break;
            case TipoSlideSettings.AbajoArriba:
                posicionOculta = new Vector2(posicionVisible.x, posicionVisible.y - altoPanel);
                break;
            case TipoSlideSettings.IzquierdaDerecha:
                posicionOculta = new Vector2(posicionVisible.x - anchoPanel, posicionVisible.y);
                break;
            case TipoSlideSettings.DerechaIzquierda:
                posicionOculta = new Vector2(posicionVisible.x + anchoPanel, posicionVisible.y);
                break;
        }
        
        Debug.Log($"[SettingsPanelManager] Posiciones calculadas - Visible: {posicionVisible}, Oculta: {posicionOculta}");
    }

    private void ConfigurarEstadoInicial()
    {
        panelVisible = false;
        
        // Ocultar panel principal
        if (rectTransformPanel != null)
        {
            rectTransformPanel.anchoredPosition = posicionOculta;
        }
        
        if (canvasGroupPanel != null)
        {
            canvasGroupPanel.alpha = 0f;
            canvasGroupPanel.interactable = false;
            canvasGroupPanel.blocksRaycasts = false;
        }
        
        // Ocultar todos los paneles
        if (panelesSettings != null)
        {
            foreach (var panel in panelesSettings)
            {
                if (panel != null && panel.panelGameObject != null)
                {
                    panel.panelGameObject.SetActive(false);
                }
            }
        }
        
        ActualizarTextoBoton();
        ActualizarBotonesNavegacion();
    }

    public void ToggleSettingsPanel()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }

        panelVisible = !panelVisible;
        
        // 🎵 Sonido
        if (usarSonidos && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoTogglePanel();
        }
        
        animacionActual = StartCoroutine(AnimarSlidePanel(panelVisible));
        
        ActualizarTextoBoton();
        
        // Si se está cerrando, ocultar panel activo
        if (!panelVisible && panelActivo != null)
        {
            OcultarPanelActivo();
        }
        
        Debug.Log($"[SettingsPanelManager] Panel settings {(panelVisible ? "abriéndose" : "cerrándose")}");
    }

    private IEnumerator AnimarSlidePanel(bool mostrar)
    {
        float tiempoTranscurrido = 0f;
        
        Vector2 posicionInicial = mostrar ? posicionOculta : posicionVisible;
        Vector2 posicionFinal = mostrar ? posicionVisible : posicionOculta;
        
        float alphaInicial = mostrar ? 0f : 1f;
        float alphaFinal = mostrar ? 1f : 0f;

        // Configurar interactividad al inicio de mostrar
        if (mostrar && canvasGroupPanel != null)
        {
            canvasGroupPanel.interactable = true;
            canvasGroupPanel.blocksRaycasts = true;
        }

        while (tiempoTranscurrido < duracionAnimacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / duracionAnimacion;
            float curveValue = curvaAnimacion.Evaluate(progreso);

            // Animar posición
            if (rectTransformPanel != null)
            {
                rectTransformPanel.anchoredPosition = Vector2.Lerp(posicionInicial, posicionFinal, curveValue);
            }

            // Animar alpha
            if (canvasGroupPanel != null)
            {
                canvasGroupPanel.alpha = Mathf.Lerp(alphaInicial, alphaFinal, curveValue);
            }

            yield return null;
        }

        // Asegurar valores finales
        if (rectTransformPanel != null)
        {
            rectTransformPanel.anchoredPosition = posicionFinal;
        }
        
        if (canvasGroupPanel != null)
        {
            canvasGroupPanel.alpha = alphaFinal;
        }

        // Desactivar interactividad al final si se oculta
        if (!mostrar && canvasGroupPanel != null)
        {
            canvasGroupPanel.interactable = false;
            canvasGroupPanel.blocksRaycasts = false;
        }
        
        // Mostrar primer panel disponible al abrir
        if (mostrar && panelActivo == null && panelesSettings != null && panelesSettings.Length > 0)
        {
            MostrarPanel(panelesSettings[0]);
        }

        animacionActual = null;
    }

    public void MostrarPanel(SettingsPanel panel)
    {
        if (panel == null) return;

        // Ocultar panel anterior
        OcultarPanelActivo();

        // Mostrar nuevo panel
        panelActivo = panel;
        if (panel.panelGameObject != null)
        {
            panel.panelGameObject.SetActive(true);
            
            // 🎵 Sonido de navegación
            if (usarSonidos && GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
            }
        }

        ActualizarBotonesNavegacion();
        
        Debug.Log($"[SettingsPanelManager] Panel mostrado: {panel.nombrePanel}");
    }

    private void OcultarPanelActivo()
    {
        if (panelActivo != null && panelActivo.panelGameObject != null)
        {
            panelActivo.panelGameObject.SetActive(false);
            panelActivo = null;
        }
    }

    private void ActualizarTextoBoton()
    {
        if (textoBotonToggle != null)
        {
            textoBotonToggle.text = panelVisible ? textoOcultar : textoMostrar;
        }
    }

    private void ActualizarBotonesNavegacion()
    {
        foreach (var kvp in botonesNavegacion)
        {
            if (kvp.Value != null)
            {
                // Resaltar botón activo
                ColorBlock colors = kvp.Value.colors;
                colors.normalColor = (kvp.Key == panelActivo) ? Color.yellow : Color.white;
                kvp.Value.colors = colors;
            }
        }
    }

    // Métodos públicos
    public void MostrarSettings()
    {
        if (!panelVisible)
        {
            ToggleSettingsPanel();
        }
    }

    public void OcultarSettings()
    {
        if (panelVisible)
        {
            ToggleSettingsPanel();
        }
    }

    public bool EstaPanelVisible()
    {
        return panelVisible;
    }

    public void MostrarPanelPorNombre(string nombrePanel)
    {
        if (panelesSettings == null) return;
        
        var panel = panelesSettings.FirstOrDefault(p => p.nombrePanel == nombrePanel);
        if (panel != null)
        {
            if (!panelVisible)
            {
                MostrarSettings();
            }
            MostrarPanel(panel);
        }
    }

    // Context menus para testing
    [ContextMenu("🎛️ Toggle Settings")]
    public void TestToggleSettings()
    {
        ToggleSettingsPanel();
    }

    [ContextMenu("🔄 Recalcular Posiciones")]
    public void RecalcularPosiciones()
    {
        CalcularPosiciones();
        if (!panelVisible)
        {
            ConfigurarEstadoInicial();
        }
    }

    [ContextMenu("🔧 Recrear Botones")]
    public void RecrearBotones()
    {
        CrearBotonesNavegacion();
    }

    void OnDestroy()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && rectTransformPanel != null)
        {
            CalcularPosiciones();
        }
    }
    [System.Serializable]
public class SettingsPanel
{
    [Header("📋 Información")]
    public string nombrePanel = "Panel";
    public string iconoPanel = "⚙️";
    
    [Header("🎮 GameObject")]
    public GameObject panelGameObject;
    
    [Header("📝 Descripción")]
    [TextArea(2, 4)]
    public string descripcion = "Descripción del panel";
    
    [Header("🎨 Configuración")]
    public bool activoPorDefecto = false;
    public int ordenEnMenu = 0;
}
}