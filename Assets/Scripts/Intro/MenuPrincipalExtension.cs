using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.IO;

/// <summary>
/// Menú principal simple que se integra con SettingsPanelManager existente
/// Solo depende de Unity y tu SettingsPanelManager - sin dependencias externas
/// Optimizado para móviles y tablets
/// </summary>
public class MenuPrincipalSimple : MonoBehaviour
{
    [Header("🎮 Configuración Principal")]
    [SerializeField] private SettingsPanelManager settingsManager;
    [SerializeField] private string nombreEscenaJuego    = "halloween";
    [SerializeField] private string nombreEscenaTutorial = "halloween-tuto1";
    [SerializeField] private string archivoTutorial      = "tutorial_completado.json";
    
    [Header("🚀 Botones del Menú")]
    [SerializeField] private Button botonIniciarJuego;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonSalir;
    [SerializeField] private Transform contenedorBotones;
    
    [Header("📱 Configuración Móvil")]
    [SerializeField] private bool autoDetectarMovil = true;
    [SerializeField] private Vector2 tamañoBotonMovil = new Vector2(320, 80);
    [SerializeField] private Vector2 tamañoBotonEscritorio = new Vector2(280, 60);
    [SerializeField] private float fontSizeMovil = 28f;
    [SerializeField] private float fontSizeEscritorio = 24f;
    [SerializeField] private float espaciadoMovil = 25f;
    [SerializeField] private float espaciadoEscritorio = 20f;
    
    [Header("🎨 Estilo Visual")]
    [SerializeField] private Color colorBotonNormal = new Color(0.2f, 0.6f, 1f, 0.9f);
    [SerializeField] private Color colorBotonHover = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color colorBotonPresionado = new Color(0.1f, 0.4f, 0.8f, 1f);
    [SerializeField] private Color colorTexto = Color.white;
    
    [Header("🎬 Animación")]
    [SerializeField] private bool usarAnimaciones = true;
    [SerializeField] private float duracionAnimacion = 0.3f;
    [SerializeField] private float escalaPulso = 1.1f;
    [SerializeField] private AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("🔊 Audio")]
    [SerializeField] private AudioClip sonidoClick;
    [SerializeField] private bool usarAudioGlobal = true;
    [SerializeField] private bool usarVibracion = true;
    
    [Header("🌟 Elementos Opcionales")]
    [SerializeField] private Image logoJuego;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoVersion;
    
    // Estado interno
    private bool esMobil;
    
    void Start()
    {
        InicializarMenu();
    }
    
    private void InicializarMenu()
    {
        DetectarPlataforma();
        BuscarComponentesAutomaticamente();
        CrearBotonesSiNoExisten();
        ConfigurarBotones();
        ConfigurarElementosVisuales();
        ConfigurarLayout();
        
        Debug.Log($"[MenuPrincipalSimple] ✅ Menú inicializado - Móvil: {esMobil}");
    }
    
    private void DetectarPlataforma()
    {
        if (autoDetectarMovil)
        {
            esMobil = Application.isMobilePlatform || 
                     Screen.width <= 1024 || 
                     Input.touchSupported;
        }
        else
        {
            esMobil = Application.isMobilePlatform;
        }
        
        Debug.Log($"[MenuPrincipalSimple] 📱 Plataforma: {(esMobil ? "Móvil" : "Escritorio")} - Resolución: {Screen.width}x{Screen.height}");
    }
    
    private void BuscarComponentesAutomaticamente()
    {
        // Buscar SettingsPanelManager si no está asignado
        if (settingsManager == null)
        {
            settingsManager = FindObjectOfType<SettingsPanelManager>();
            if (settingsManager != null)
            {
                Debug.Log("[MenuPrincipalSimple] ✅ SettingsPanelManager encontrado automáticamente");
            }
        }
        
        // Buscar elementos visuales opcionales (solo si el tag existe)
        if (logoJuego == null)
        {
            try
            {
                GameObject logoObj = GameObject.FindWithTag("Logo");
                if (logoObj != null) logoJuego = logoObj.GetComponent<Image>();
            }
            catch (UnityException) { }
        }

        if (textoTitulo == null)
        {
            try
            {
                GameObject tituloObj = GameObject.FindWithTag("TituloJuego");
                if (tituloObj != null) textoTitulo = tituloObj.GetComponent<TextMeshProUGUI>();
            }
            catch (UnityException) { }
        }
    }
    
    private void CrearBotonesSiNoExisten()
    {
        // Crear contenedor si no existe
        if (contenedorBotones == null)
        {
            CrearContenedorBotones();
        }
        
        // Crear botones si no están asignados
        if (botonIniciarJuego == null)
        {
            botonIniciarJuego = CrearBoton("BotonIniciarJuego", "🚀 INICIAR JUEGO");
        }
        
        if (botonOpciones == null)
        {
            botonOpciones = CrearBoton("BotonOpciones", "⚙️ OPCIONES");
        }
        
        if (botonSalir == null)
        {
            botonSalir = CrearBoton("BotonSalir", "🚪 SALIR");
        }
    }
    
    private void CrearContenedorBotones()
    {
        GameObject contenedor = new GameObject("ContenedorBotones");
        contenedor.transform.SetParent(transform, false);
        
        RectTransform rect = contenedor.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 300);
        rect.anchoredPosition = new Vector2(0, esMobil ? -50 : 0);
        
        contenedorBotones = contenedor.transform;
        
        Debug.Log("[MenuPrincipalSimple] 📦 Contenedor de botones creado");
    }
    
    private Button CrearBoton(string nombre, string texto)
    {
        // Crear GameObject del botón
        GameObject botonObj = new GameObject(nombre);
        botonObj.transform.SetParent(contenedorBotones, false);
        
        // Configurar RectTransform
        RectTransform rect = botonObj.AddComponent<RectTransform>();
        Vector2 tamaño = esMobil ? tamañoBotonMovil : tamañoBotonEscritorio;
        rect.sizeDelta = tamaño;
        
        // Agregar Image
        Image imagen = botonObj.AddComponent<Image>();
        imagen.color = colorBotonNormal;
        
        // Agregar Button
        Button boton = botonObj.AddComponent<Button>();
        
        // Configurar colores
        ColorBlock colores = boton.colors;
        colores.normalColor = colorBotonNormal;
        colores.highlightedColor = colorBotonHover;
        colores.pressedColor = colorBotonPresionado;
        colores.fadeDuration = 0.2f;
        boton.colors = colores;
        
        // Crear texto
        GameObject textoObj = new GameObject("Texto");
        textoObj.transform.SetParent(botonObj.transform, false);
        
        RectTransform textoRect = textoObj.AddComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.sizeDelta = Vector2.zero;
        textoRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI textoComponent = textoObj.AddComponent<TextMeshProUGUI>();
        textoComponent.text = texto;
        textoComponent.fontSize = esMobil ? fontSizeMovil : fontSizeEscritorio;
        textoComponent.color = colorTexto;
        textoComponent.alignment = TextAlignmentOptions.Center;
        textoComponent.enableAutoSizing = true;
        textoComponent.fontSizeMin = 16f;
        textoComponent.fontSizeMax = textoComponent.fontSize;
        
        Debug.Log($"[MenuPrincipalSimple] 🎮 Botón creado: {nombre}");
        return boton;
    }
    
    private void ConfigurarBotones()
    {
        // Configurar acciones de los botones
        if (botonIniciarJuego != null)
        {
            botonIniciarJuego.onClick.RemoveAllListeners();
            botonIniciarJuego.onClick.AddListener(() => OnClickIniciarJuego());
            ConfigurarAnimacionBoton(botonIniciarJuego);
        }
        
        if (botonOpciones != null)
        {
            botonOpciones.onClick.RemoveAllListeners();
            botonOpciones.onClick.AddListener(() => OnClickOpciones());
            ConfigurarAnimacionBoton(botonOpciones);
        }
        
        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(() => OnClickSalir());
            ConfigurarAnimacionBoton(botonSalir);
        }
        
        Debug.Log("[MenuPrincipalSimple] ✅ Botones configurados");
    }
    
    private void ConfigurarAnimacionBoton(Button boton)
    {
        if (!usarAnimaciones || boton == null) return;
        
        SimpleButtonAnimator animator = boton.gameObject.GetComponent<SimpleButtonAnimator>();
        if (animator == null)
        {
            animator = boton.gameObject.AddComponent<SimpleButtonAnimator>();
        }
        
        animator.ConfigurarAnimacion(escalaPulso, duracionAnimacion, curvaAnimacion);
    }
    
    private void ConfigurarElementosVisuales()
    {
        // Configurar logo
        if (logoJuego != null)
        {
            RectTransform logoRect = logoJuego.GetComponent<RectTransform>();
            if (logoRect != null)
            {
                logoRect.sizeDelta = esMobil ? new Vector2(200, 100) : new Vector2(300, 150);
                logoRect.anchoredPosition = new Vector2(0, esMobil ? 300 : 250);
            }
        }
        
        // Configurar título
        if (textoTitulo != null)
        {
            textoTitulo.fontSize = esMobil ? 32f : 42f;
            RectTransform tituloRect = textoTitulo.GetComponent<RectTransform>();
            if (tituloRect != null)
            {
                tituloRect.anchoredPosition = new Vector2(0, esMobil ? 180 : 150);
            }
        }
        
        // Configurar versión
        if (textoVersion != null)
        {
            textoVersion.fontSize = esMobil ? 16f : 14f;
            textoVersion.text = $"v{Application.version}";
            
            RectTransform versionRect = textoVersion.GetComponent<RectTransform>();
            if (versionRect != null)
            {
                versionRect.anchorMin = new Vector2(1f, 0f);
                versionRect.anchorMax = new Vector2(1f, 0f);
                versionRect.anchoredPosition = new Vector2(-20, 20);
            }
        }
    }
    
    private void ConfigurarLayout()
    {
        if (contenedorBotones == null) return;
        
        // Agregar VerticalLayoutGroup para organización automática
        VerticalLayoutGroup layout = contenedorBotones.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contenedorBotones.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        layout.spacing = esMobil ? espaciadoMovil : espaciadoEscritorio;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        
        Debug.Log("[MenuPrincipalSimple] 📐 Layout configurado");
    }
    
    // Acciones de los botones
    public void OnClickIniciarJuego()
    {
        Debug.Log("[MenuPrincipalSimple] 🚀 Iniciar Juego presionado");
        
        ReproducirSonido();
        EjecutarVibracion();
        
        StartCoroutine(IniciarJuegoConDelay());
    }
    
    private IEnumerator IniciarJuegoConDelay()
    {
        // Esperar a que termine la animación del botón
        yield return new WaitForSeconds(0.3f);

        // El menú decide adónde ir según si el tutorial ya fue completado.
        // Así evitamos cargar la escena de tutorial (con Vuforia) solo para
        // detectar que hay que saltarla, lo que rompía la escena del juego.
        string rutaTutorial = Path.Combine(Application.persistentDataPath, archivoTutorial);
        bool tutorialCompletado = File.Exists(rutaTutorial);

        string escenaDestino = tutorialCompletado ? nombreEscenaJuego : nombreEscenaTutorial;
        Debug.Log($"[MenuPrincipalSimple] → Tutorial completado: {tutorialCompletado} → cargando '{escenaDestino}'");

        try
        {
            SceneManager.LoadScene(escenaDestino);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MenuPrincipalSimple] ❌ Error al cambiar escena: {e.Message}");
        }
    }
    
    public void OnClickOpciones()
    {
        Debug.Log("[MenuPrincipalSimple] ⚙️ Opciones presionado");
        
        ReproducirSonido();
        EjecutarVibracion();
        
        if (settingsManager != null)
        {
            settingsManager.MostrarSettings();
            Debug.Log("[MenuPrincipalSimple] ✅ Panel de configuración abierto");
        }
        else
        {
            Debug.LogWarning("[MenuPrincipalSimple] ⚠️ SettingsPanelManager no encontrado");
        }
    }
    
    public void OnClickSalir()
    {
        Debug.Log("[MenuPrincipalSimple] 🚪 Salir presionado");
        
        ReproducirSonido();
        EjecutarVibracion();
        
        if (esMobil)
        {
            StartCoroutine(SalirConConfirmacion());
        }
        else
        {
            SalirDelJuego();
        }
    }
    
    private IEnumerator SalirConConfirmacion()
    {
        Debug.Log("[MenuPrincipalSimple] 📱 Confirmando salida en móvil...");
        yield return new WaitForSeconds(0.5f);
        SalirDelJuego();
    }
    
    private void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("[MenuPrincipalSimple] 🛑 Saliendo del juego (Editor)");
#else
        Application.Quit();
        Debug.Log("[MenuPrincipalSimple] 🛑 Saliendo del juego");
#endif
    }
    
    private void ReproducirSonido()
    {
        if (usarAudioGlobal && GlobalAudioManager.Instance != null)
        {
            if (sonidoClick != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoSFX(sonidoClick);
            }
            else
            {
                GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
            }
        }
    }
    
    private void EjecutarVibracion()
    {
        if (!usarVibracion || !esMobil) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
        Debug.Log("[MenuPrincipalSimple] 📳 Vibración ejecutada");
    }
    
    // Métodos públicos de configuración
    public void ConfigurarEscenaJuego(string nombreEscena)
    {
        nombreEscenaJuego = nombreEscena;
        Debug.Log($"[MenuPrincipalSimple] 🎯 Escena configurada: {nombreEscena}");
    }
    
    public void ForzarModoMovil(bool movil)
    {
        esMobil = movil;
        ConfigurarElementosVisuales();
        ConfigurarLayout();
    }
    
    // Métodos de testing
    [ContextMenu("🎮 Test Iniciar Juego")]
    public void TestIniciarJuego()
    {
        OnClickIniciarJuego();
    }
    
    [ContextMenu("⚙️ Test Opciones")]
    public void TestOpciones()
    {
        OnClickOpciones();
    }
    
    [ContextMenu("🚪 Test Salir")]
    public void TestSalir()
    {
        OnClickSalir();
    }
    
    [ContextMenu("📱 Toggle Móvil")]
    public void TestToggleMovil()
    {
        esMobil = !esMobil;
        ConfigurarElementosVisuales();
        ConfigurarLayout();
        Debug.Log($"[MenuPrincipalSimple] 📱 Modo cambiado: {(esMobil ? "Móvil" : "Escritorio")}");
    }
    
    [ContextMenu("🔄 Reinicializar")]
    public void TestReinicializar()
    {
        InicializarMenu();
    }
    
    void OnValidate()
    {
        // Validar configuraciones
        if (tamañoBotonMovil.x <= 0) tamañoBotonMovil.x = 320;
        if (tamañoBotonMovil.y <= 0) tamañoBotonMovil.y = 80;
        if (fontSizeMovil <= 0) fontSizeMovil = 28;
        if (duracionAnimacion <= 0) duracionAnimacion = 0.3f;
    }
}

/// <summary>
/// Animador simple para botones sin dependencias externas
/// </summary>
public class SimpleButtonAnimator : MonoBehaviour
{
    private Vector3 escalaOriginal;
    private float escalaPulso = 1.1f;
    private float duracionAnimacion = 0.3f;
    private AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Coroutine animacionActual;
    
    void Start()
    {
        escalaOriginal = transform.localScale;
        
        Button boton = GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick.AddListener(AnimarClick);
        }
    }
    
    public void ConfigurarAnimacion(float escala, float duracion, AnimationCurve curva)
    {
        escalaPulso = escala;
        duracionAnimacion = duracion;
        curvaAnimacion = curva;
    }
    
    public void AnimarClick()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }
        
        animacionActual = StartCoroutine(EjecutarAnimacion());
    }
    
    private IEnumerator EjecutarAnimacion()
    {
        // Escalar hacia arriba
        yield return StartCoroutine(AnimarEscala(escalaOriginal * escalaPulso, duracionAnimacion / 2));
        
        // Escalar hacia abajo
        yield return StartCoroutine(AnimarEscala(escalaOriginal, duracionAnimacion / 2));
        
        animacionActual = null;
    }
    
    private IEnumerator AnimarEscala(Vector3 escalaDestino, float duracion)
    {
        Vector3 escalaInicial = transform.localScale;
        float tiempo = 0;
        
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = curvaAnimacion.Evaluate(tiempo / duracion);
            transform.localScale = Vector3.Lerp(escalaInicial, escalaDestino, t);
            yield return null;
        }
        
        transform.localScale = escalaDestino;
    }
}