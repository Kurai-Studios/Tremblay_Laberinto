using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 🔄 StoryUIController - VERSIÓN INTEGRADA CON CANVAS EXISTENTE
/// Esta versión NO crea su propio Canvas, sino que se integra con el Canvas principal de la escena
/// Esto evita conflictos con otros elementos de UI como inventario y misiones
/// </summary>
public class StoryUIController : MonoBehaviour
{
    [Header("📱 Referencias UI")]
    [SerializeField] private Canvas canvasExistente; // ✅ Usar Canvas existente de la escena
    [SerializeField] private GameObject panelPrincipal;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("🎨 Elementos Visuales")]
    [SerializeField] private Image imagenFondo;
    [SerializeField] private Image imagenFondoSprite;
    [SerializeField] private Image viñeta;
    [SerializeField] private Image imagenAdicional;
    
    [Header("📝 Texto")]
    [SerializeField] private TextMeshProUGUI textoFragmento;
    [SerializeField] private TextMeshProUGUI textoNombreFragmento;
    
    [Header("🎮 Controles")]
    [SerializeField] private Button botonContinuar;
    [SerializeField] private Button botonSaltar;
    [SerializeField] private GameObject indicadorCargando;
    
    [Header("⚙️ Configuración")]
    [SerializeField] private bool crearUIAutomaticamente = true;
    [SerializeField] private bool buscarCanvasAutomaticamente = true;
    
    [Header("🖼️ Configuración de Imágenes")]
    [SerializeField] private bool mostrarImagenFondoCompleta = true;
    [SerializeField] private bool usarTransparenciaAdaptiva = true;
    [SerializeField] private Color colorFondoFallback = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] [Range(0f, 1f)] private float opacidadImagenFondo = 0.8f;
    
    // ✅ NUEVO: Control de sorting para no interferir con otros UI
    [Header("🎭 Control de Capas")]
    [SerializeField] private int sortingOrderStory = 50; // Menor que otros UI críticos
    [SerializeField] private bool respetarOtrosCanvas = true;
    
    private Coroutine animacionActual;
    private Coroutine typewriterActual;
    private bool fragmentoCompleto = false;
    private bool saltado = false;
    private StoryFragment fragmentoActual;

    [Header("📱 Configuración Móvil")]
    [SerializeField] private float factorEscalaMobil = 1.2f;
    [SerializeField] private int tamanoFuenteBaseMobil = 24;
    [SerializeField] private int tamanoFuenteBaseTablet = 28;
    
    [Header("⏱️ Control de Timing")]
    [SerializeField] private bool esperarFinAudio = true;
    [SerializeField] private float tiempoEsperaExtra = 0.5f;
    [SerializeField] private bool usarSincronizacionAudio = true;

    [Header("🔗 Transición entre fragmentos encadenados")]
    [Tooltip("Duración del crossfade del contenido (texto/imagen) cuando un fragmento está encadenado al siguiente, sin ocultar el panel completo")]
    [SerializeField] private float duracionTransicionContenido = 0.25f;

    private bool esMobil;
    private AudioSource audioActual;

    // ✅ Persisten durante toda una cadena de fragmentos encadenados, para
    // ocultar otros UI al inicio de la cadena y restaurarlos solo al final.
    private bool inventarioEstabaAbierto = false;
    private bool objectInfoEstabaAbierto = false;

    /// <summary>
    /// True si, al terminar el último MostrarFragmento, el panel quedó visible
    /// esperando un crossfade de contenido hacia fragmento.siguienteFragmento
    /// (es decir, no se hizo fade out ni se ocultó la UI).
    /// </summary>
    public bool PanelListoParaContinuacion { get; private set; }
    
    // ✅ Referencias a otros componentes de UI que NO deben ser afectados
    private InventarioToggleController inventarioToggle;
    private ObjectInfoUIManager objectInfoUI;
    
    void Awake()
    {
        Debug.Log("📖 [StoryUI] 🔄 INICIANDO VERSIÓN INTEGRADA");
        DetectarTipoDispositivo();
        // Las referencias y la UI se configuran en Start() para garantizar que
        // todos los Canvas de la escena ya estén activos e inicializados.
    }

    void Start()
    {
        // Buscar referencias ahora que todos los objetos de la escena están activos
        BuscarReferenciasSistemaExistente();

        if (canvasExistente == null || panelPrincipal == null)
        {
            Debug.Log($"📖 [StoryUI] Referencias faltantes (canvas={canvasExistente != null}, panel={panelPrincipal != null}) — auto-creando...");
            CrearUIEnCanvasExistente();
        }

        ConfigurarUI();

        // Asegurar que el panel empiece oculto
        if (panelPrincipal != null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            panelPrincipal.SetActive(false);
        }

        Debug.Log("📖 [StoryUI] ✅ Sistema inicializado correctamente");
    }
    
    // ✅ NUEVO: Buscar componentes del sistema existente
    private void BuscarReferenciasSistemaExistente()
    {
        // Buscar Canvas existente — includeInactive:true para no fallar si el Canvas
        // está momentáneamente inactivo al cargar la escena.
        if (buscarCanvasAutomaticamente && canvasExistente == null)
        {
            canvasExistente = FindObjectOfType<Canvas>(true);

            if (canvasExistente != null)
                Debug.Log($"📖 [StoryUI] ✅ Canvas encontrado: {canvasExistente.name}");
            else
                Debug.LogWarning("📖 [StoryUI] ⚠️ No se encontró Canvas en la escena. Asignar 'canvasExistente' en inspector.");
        }
        
        // Buscar referencias a otros sistemas
        inventarioToggle = InventarioToggleController.Instance;
        objectInfoUI = ObjectInfoUIManager.Instance;
        
        Debug.Log($"📖 [StoryUI] Referencias encontradas - Inventario: {inventarioToggle != null} | ObjectInfo: {objectInfoUI != null}");
    }
    
    private void DetectarTipoDispositivo()
    {
        float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float diagonal = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight) / dpi;
        
        float aspectRatio = screenHeight > 0 ? (float)Screen.width / Screen.height : 1f;
        bool esAspectRatioMovil = aspectRatio < 0.75f || aspectRatio > 1.5f;
        
        esMobil = diagonal < 7.0f || esAspectRatioMovil || Screen.width < 1200;
        
        Debug.Log($"📱 [StoryUI] Dispositivo: {(esMobil ? "MÓVIL" : "TABLET")} | " +
                 $"Diagonal: {diagonal:F1}\" | Resolución: {Screen.width}x{Screen.height}");
    }

    public IEnumerator MostrarFragmento(StoryFragment fragmento, bool esContinuacion = false)
    {
        if (fragmento == null || !fragmento.EsValido())
        {
            Debug.LogWarning("📖 [StoryUI] ⚠️ Fragmento inválido");
            yield break;
        }

        fragmentoActual = fragmento;
        fragmentoCompleto = false;
        saltado = false;
        audioActual = null;

        Debug.Log($"📖 [StoryUI] 🎬 MOSTRANDO FRAGMENTO: {fragmento.fragmentID} (continuación: {esContinuacion})");

        // ✅ GESTIÓN INTELIGENTE DE OTROS UI
        // Solo al inicio de una cadena: si es continuación, los otros UI ya
        // fueron ocultados por el fragmento anterior y no deben volver a tocarse.
        if (!esContinuacion && respetarOtrosCanvas)
        {
            inventarioEstabaAbierto = false;
            objectInfoEstabaAbierto = false;

            // Verificar y cerrar temporalmente otros UI si están abiertos
            if (inventarioToggle != null && inventarioToggle.EstaPanelVisible())
            {
                inventarioEstabaAbierto = true;
                inventarioToggle.OcultarPanel();
                Debug.Log("📖 [StoryUI] Inventario temporalmente cerrado para mostrar historia");
            }

            if (objectInfoUI != null && objectInfoUI.TieneCanvasActivo())
            {
                objectInfoEstabaAbierto = true;
                objectInfoUI.OcultarCanvasTemporalmente();
                Debug.Log("📖 [StoryUI] ObjectInfo temporalmente oculto para mostrar historia");
            }
        }

        if (esContinuacion)
        {
            // El panel ya está visible (alpha = 1): hacer un crossfade del
            // contenido (texto/imagen) en lugar de ocultar y volver a mostrar el panel.
            yield return StartCoroutine(FadeOutContenido(duracionTransicionContenido));
            ConfigurarParaFragmento(fragmento);
            yield return StartCoroutine(FadeInContenido(duracionTransicionContenido));
        }
        else
        {
            ConfigurarParaFragmento(fragmento);
            yield return StartCoroutine(FadeIn(fragmento.tiempoFadeIn));
        }

        float tiempoInicioAudio = Time.time;
        if (fragmento.audioNarracion != null)
        {
            ReproducirAudio(fragmento);
        }

        if (!string.IsNullOrEmpty(fragmento.textoFragmento))
        {
            if (fragmento.usarTypewriter)
            {
                typewriterActual = StartCoroutine(MostrarTextoTypewriter(
                    fragmento.textoFragmento,
                    fragmento.velocidadTypewriter
                ));
                yield return typewriterActual;
                typewriterActual = null;
            }
            else
            {
                textoFragmento.text = LimpiarMarcadoresInstantaneos(fragmento.textoFragmento);
            }
        }

        fragmentoCompleto = true;

        if (fragmento.avanceAutomatico)
        {
            yield return StartCoroutine(EsperarFinalizacionFragmento(fragmento, tiempoInicioAudio));
        }
        else
        {
            botonContinuar.gameObject.SetActive(true);
            yield return StartCoroutine(EsperarBotonContinuar());
        }
        
        bool hayContinuacion = !saltado && fragmento.siguienteFragmento != null;
        PanelListoParaContinuacion = hayContinuacion;

        if (hayContinuacion)
        {
            // El siguiente fragmento de la cadena hará el crossfade de contenido,
            // así que el panel se mantiene visible (sin fade out ni ocultar UI).
            // Solo se libera el audio de este fragmento.
            if (audioActual != null)
            {
                Destroy(audioActual.gameObject);
                audioActual = null;
            }
        }
        else
        {
            if (!saltado)
            {
                yield return StartCoroutine(FadeOut(fragmento.tiempoFadeOut));
            }

            OcultarUI();

            // ✅ RESTAURAR OTROS UI SI ESTABAN ABIERTOS (fin de la cadena)
            if (respetarOtrosCanvas)
            {
                if (inventarioEstabaAbierto && inventarioToggle != null)
                {
                    yield return new WaitForSeconds(0.2f); // Pequeña pausa
                    inventarioToggle.MostrarPanel();
                    Debug.Log("📖 [StoryUI] Inventario restaurado después de la historia");
                }

                if (objectInfoEstabaAbierto && objectInfoUI != null)
                {
                    objectInfoUI.MostrarCanvasTemporalmente();
                    Debug.Log("📖 [StoryUI] ObjectInfo restaurado después de la historia");
                }

                inventarioEstabaAbierto = false;
                objectInfoEstabaAbierto = false;
            }
        }

        fragmentoActual = null;
    }
    
    private IEnumerator EsperarFinalizacionFragmento(StoryFragment fragmento, float tiempoInicioAudio)
    {
        if (fragmento.duracionTotal > 0f)
        {
            // El usuario definió una duración explícita: respetarla sin importar el audio.
            // Se descuenta lo que ya transcurrió desde que empezó a sonar el audio
            // (fade-in + typewriter) para que el tiempo total visible coincida con duracionTotal.
            float yaTranscurrido = Time.time - tiempoInicioAudio;
            float tiempoEspera = Mathf.Max(0f, fragmento.duracionTotal - yaTranscurrido);

            float elapsed = 0f;
            while (elapsed < tiempoEspera && !saltado)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Si el audio sigue sonando y la duración configurada es menor, cortarlo
            if (audioActual != null && audioActual.isPlaying)
            {
                audioActual.Stop();
            }
        }
        else if (fragmento.audioNarracion != null && esperarFinAudio && usarSincronizacionAudio)
        {
            // Sin duración explícita: esperar a que termine el audio (descontando lo ya
            // transcurrido mientras se mostraba el typewriter, etc.)
            float yaTranscurrido = Time.time - tiempoInicioAudio;
            float duracion = Mathf.Max(0f, fragmento.audioNarracion.length - yaTranscurrido);

            float elapsed = 0f;
            while (elapsed < duracion && !saltado)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!saltado && tiempoEsperaExtra > 0f)
                yield return new WaitForSeconds(tiempoEsperaExtra);
        }
        else
        {
            float tiempoEspera = Mathf.Max(0f, fragmento.ObtenerDuracionTotal() - fragmento.tiempoFadeIn);
            float elapsed = 0f;
            while (elapsed < tiempoEspera && !saltado)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    private void ConfigurarParaFragmento(StoryFragment fragmento)
    {
        Debug.Log($"📖 [StoryUI] ⚙️ Configurando fragmento: {fragmento.fragmentID}");
        
        if (imagenFondo != null)
        {
            imagenFondo.color = fragmento.colorFondo;
        }
        
        if (fragmento.imagenFondo != null)
        {
            ConfigurarImagenFondoFragmento(fragmento);
        }
        else
        {
            OcultarImagenesFondo();
        }
        
        if (textoFragmento != null)
        {
            textoFragmento.color = fragmento.colorTexto;
            int tamañoFinal = CalcularTamañoFuente(fragmento.tamañoFuente);
            textoFragmento.fontSize = tamañoFinal;
            textoFragmento.text = "";
        }
        
        if (textoNombreFragmento != null && !string.IsNullOrEmpty(fragmento.nombreFragmento))
        {
            textoNombreFragmento.text = fragmento.nombreFragmento;
            textoNombreFragmento.gameObject.SetActive(true);
        }
        else if (textoNombreFragmento != null)
        {
            textoNombreFragmento.gameObject.SetActive(false);
        }
        
        if (viñeta != null)
        {
            Color colorViñeta = new Color(0f, 0f, 0f, fragmento.intensidadViñeta);
            viñeta.color = colorViñeta;
        }
        
        Debug.Log($"📖 [StoryUI] ✅ Fragmento configurado");
    }
    
    private void ConfigurarImagenFondoFragmento(StoryFragment fragmento)
    {
        Image targetImage = imagenFondoSprite != null ? imagenFondoSprite : imagenAdicional;
        
        if (targetImage != null)
        {
            targetImage.sprite = fragmento.imagenFondo;
            
            if (mostrarImagenFondoCompleta)
            {
                RectTransform rectTransform = targetImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
                
                targetImage.type = Image.Type.Simple;
                targetImage.preserveAspect = esMobil;
            }
            
            Color colorImagen = Color.white;
            if (usarTransparenciaAdaptiva)
            {
                colorImagen.a = esMobil ? opacidadImagenFondo * 0.8f : opacidadImagenFondo;
            }
            else
            {
                colorImagen.a = opacidadImagenFondo;
            }
            
            targetImage.color = colorImagen;
            targetImage.gameObject.SetActive(true);
            
            Debug.Log($"🖼️ [StoryUI] ✅ Imagen de fondo configurada: {fragmento.imagenFondo.name}");
        }
        else
        {
            Debug.LogWarning("📖 [StoryUI] ⚠️ No se encontró componente Image para imagen de fondo");
        }
    }
    
    private void OcultarImagenesFondo()
    {
        if (imagenFondoSprite != null)
        {
            imagenFondoSprite.gameObject.SetActive(false);
        }
        
        if (imagenAdicional != null)
        {
            imagenAdicional.gameObject.SetActive(false);
        }
    }
    
    private int CalcularTamañoFuente(int tamañoBase)
    {
        return esMobil ? Mathf.Max(tamañoBase, tamanoFuenteBaseMobil) 
                       : Mathf.Max(tamañoBase, tamanoFuenteBaseTablet);
    }
    
    private void ReproducirAudio(StoryFragment fragmento)
    {
        if (fragmento.audioNarracion == null) return;
        
        GameObject audioObj = new GameObject("AudioFragmento");
        audioObj.transform.SetParent(transform);
        
        audioActual = audioObj.AddComponent<AudioSource>();
        audioActual.clip = fragmento.audioNarracion;
        audioActual.volume = fragmento.volumenAudio;
        audioActual.Play();
        
        Debug.Log($"🔊 [StoryUI] Reproduciendo audio: {fragmento.audioNarracion.name}");
    }
    
    private IEnumerator MostrarTextoTypewriter(string texto, float velocidad)
    {
        if (textoFragmento == null) yield break;

        List<SegmentoTexto> segmentos = ParsearTexto(texto);

        int totalAnimados = 0;
        var pausas = new Dictionary<int, float>();

        foreach (var seg in segmentos)
        {
            if (seg.tipo == TipoSegmentoTexto.Animado)
            {
                totalAnimados++;
            }
            else if (seg.tipo == TipoSegmentoTexto.Pausa)
            {
                float duracion = 0f;
                float.TryParse(seg.contenido, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out duracion);

                pausas.TryGetValue(totalAnimados, out float acumulado);
                pausas[totalAnimados] = acumulado + duracion;
            }
        }

        float tiempoPorCaracter = 1f / velocidad;

        textoFragmento.text = ConstruirTextoVisible(segmentos, 0);

        for (int i = 0; i <= totalAnimados; i++)
        {
            if (saltado) break;

            textoFragmento.text = ConstruirTextoVisible(segmentos, i);

            if (pausas.TryGetValue(i, out float duracionPausa) && duracionPausa > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duracionPausa && !saltado)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (saltado) break;

            yield return new WaitForSeconds(tiempoPorCaracter);
        }

        textoFragmento.text = ConstruirTextoVisible(segmentos, totalAnimados);
    }

    private enum TipoSegmentoTexto { Tag, Instantaneo, Animado, Pausa }

    private struct SegmentoTexto
    {
        public TipoSegmentoTexto tipo;
        public string contenido;
    }

    /// <summary>
    /// Divide el texto del fragmento en segmentos:
    /// - "[Nombre]" → texto instantáneo (sin animación), se muestra desde el inicio.
    /// - "&lt;tag&gt;" → etiquetas de rich text de TMP (color, bold, etc.), siempre presentes.
    /// - resto → caracteres animados por el typewriter, uno por uno.
    /// </summary>
    private List<SegmentoTexto> ParsearTexto(string texto)
    {
        var segmentos = new List<SegmentoTexto>();
        int i = 0;

        while (i < texto.Length)
        {
            char c = texto[i];

            if (c == '[')
            {
                int cierre = texto.IndexOf(']', i + 1);
                if (cierre >= 0)
                {
                    string contenido = texto.Substring(i + 1, cierre - i - 1);
                    segmentos.Add(new SegmentoTexto { tipo = TipoSegmentoTexto.Instantaneo, contenido = contenido });
                    i = cierre + 1;
                    continue;
                }
            }

            if (c == '<')
            {
                int cierre = texto.IndexOf('>', i + 1);
                if (cierre >= 0)
                {
                    string tag = texto.Substring(i, cierre - i + 1);
                    segmentos.Add(new SegmentoTexto { tipo = TipoSegmentoTexto.Tag, contenido = tag });
                    i = cierre + 1;
                    continue;
                }
            }

            if (c == '{')
            {
                int cierre = texto.IndexOf('}', i + 1);
                if (cierre >= 0)
                {
                    string contenido = texto.Substring(i + 1, cierre - i - 1);
                    segmentos.Add(new SegmentoTexto { tipo = TipoSegmentoTexto.Pausa, contenido = contenido });
                    i = cierre + 1;
                    continue;
                }
            }

            segmentos.Add(new SegmentoTexto { tipo = TipoSegmentoTexto.Animado, contenido = c.ToString() });
            i++;
        }

        return segmentos;
    }

    /// <summary>
    /// Construye el texto visible (con tags de rich text válidos) revelando
    /// solo "caracteresRevelados" caracteres animados. Las etiquetas abiertas
    /// que aún no llegaron a su cierre se cierran automáticamente para que
    /// TMP no rompa el formato a mitad del typewriter.
    /// </summary>
    private string ConstruirTextoVisible(List<SegmentoTexto> segmentos, int caracteresRevelados)
    {
        var sb = new System.Text.StringBuilder();
        var pilaCierres = new Stack<string>();
        int revelados = 0;

        foreach (var seg in segmentos)
        {
            switch (seg.tipo)
            {
                case TipoSegmentoTexto.Tag:
                    sb.Append(seg.contenido);

                    if (seg.contenido.StartsWith("</"))
                    {
                        if (pilaCierres.Count > 0) pilaCierres.Pop();
                    }
                    else if (!seg.contenido.EndsWith("/>"))
                    {
                        pilaCierres.Push(ObtenerTagCierre(seg.contenido));
                    }
                    break;

                case TipoSegmentoTexto.Instantaneo:
                    sb.Append(seg.contenido);
                    break;

                case TipoSegmentoTexto.Animado:
                    if (revelados < caracteresRevelados)
                    {
                        sb.Append(seg.contenido);
                        revelados++;
                    }
                    else
                    {
                        while (pilaCierres.Count > 0)
                            sb.Append(pilaCierres.Pop());

                        return sb.ToString();
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    private string ObtenerTagCierre(string tagApertura)
    {
        int finNombre = tagApertura.IndexOfAny(new[] { '=', ' ', '>' });
        if (finNombre <= 1) return "";

        string nombre = tagApertura.Substring(1, finNombre - 1);
        return $"</{nombre}>";
    }

    /// <summary>
    /// Quita los marcadores "[ ]" (texto instantáneo) y "{ }" (pausas del
    /// typewriter), dejando solo el contenido visible. Usado cuando el texto
    /// se muestra de una sola vez (sin typewriter) o al saltar el fragmento.
    /// </summary>
    private string LimpiarMarcadoresInstantaneos(string texto)
    {
        texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\{[^}]*\}", "");
        return texto.Replace("[", "").Replace("]", "");
    }
    
    private IEnumerator FadeIn(float duracion)
    {
        if (canvasGroup == null) 
        {
            Debug.LogWarning("📖 [StoryUI] ⚠️ CanvasGroup es null en FadeIn");
            yield break;
        }
        
        Debug.Log($"📖 [StoryUI] 🎭 Iniciando FadeIn ({duracion}s)");
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        
        if (panelPrincipal != null)
        {
            panelPrincipal.SetActive(true);
        }
        
        float tiempo = 0f;
        while (tiempo < duracion && duracion > 0)
        {
            tiempo += Time.deltaTime;
            canvasGroup.alpha = tiempo / duracion;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        Debug.Log("📖 [StoryUI] ✅ FadeIn completado");
    }
    
    private IEnumerator FadeOut(float duracion)
    {
        if (canvasGroup == null) yield break;
        
        Debug.Log($"📖 [StoryUI] 🎭 Iniciando FadeOut ({duracion}s)");
        
        float tiempo = 0f;
        float alphaInicial = canvasGroup.alpha;
        
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            canvasGroup.alpha = alphaInicial * (1f - tiempo / duracion);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Debug.Log("📖 [StoryUI] ✅ FadeOut completado");
    }

    /// <summary>
    /// Gráficos visuales del contenido (texto/imágenes) usados para el crossfade
    /// entre fragmentos encadenados. No incluye los botones.
    /// </summary>
    private List<Graphic> ObtenerGraficosContenido()
    {
        var graficos = new List<Graphic>();

        if (textoFragmento != null) graficos.Add(textoFragmento);
        if (textoNombreFragmento != null && textoNombreFragmento.gameObject.activeSelf) graficos.Add(textoNombreFragmento);
        if (imagenFondo != null) graficos.Add(imagenFondo);
        if (imagenFondoSprite != null && imagenFondoSprite.gameObject.activeSelf) graficos.Add(imagenFondoSprite);
        if (imagenAdicional != null && imagenAdicional.gameObject.activeSelf) graficos.Add(imagenAdicional);
        if (viñeta != null) graficos.Add(viñeta);

        return graficos;
    }

    /// <summary>
    /// Crossfade de salida: atenúa el contenido actual a alpha 0 (el panel
    /// completo permanece visible).
    /// </summary>
    private IEnumerator FadeOutContenido(float duracion)
    {
        var graficos = ObtenerGraficosContenido();
        var alfaInicial = new float[graficos.Count];
        for (int i = 0; i < graficos.Count; i++) alfaInicial[i] = graficos[i].color.a;

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = duracion > 0f ? Mathf.Clamp01(tiempo / duracion) : 1f;

            for (int i = 0; i < graficos.Count; i++)
            {
                Color c = graficos[i].color;
                c.a = Mathf.Lerp(alfaInicial[i], 0f, t);
                graficos[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < graficos.Count; i++)
        {
            Color c = graficos[i].color;
            c.a = 0f;
            graficos[i].color = c;
        }
    }

    /// <summary>
    /// Crossfade de entrada: lleva el contenido recién configurado (ya con sus
    /// colores/alphas finales asignados por ConfigurarParaFragmento) desde
    /// alpha 0 hasta su valor objetivo.
    /// </summary>
    private IEnumerator FadeInContenido(float duracion)
    {
        var graficos = ObtenerGraficosContenido();
        var alfaObjetivo = new float[graficos.Count];
        for (int i = 0; i < graficos.Count; i++)
        {
            alfaObjetivo[i] = graficos[i].color.a;

            Color c = graficos[i].color;
            c.a = 0f;
            graficos[i].color = c;
        }

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = duracion > 0f ? Mathf.Clamp01(tiempo / duracion) : 1f;

            for (int i = 0; i < graficos.Count; i++)
            {
                Color c = graficos[i].color;
                c.a = Mathf.Lerp(0f, alfaObjetivo[i], t);
                graficos[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < graficos.Count; i++)
        {
            Color c = graficos[i].color;
            c.a = alfaObjetivo[i];
            graficos[i].color = c;
        }
    }

    private IEnumerator EsperarBotonContinuar()
    {
        bool continuarPresionado = false;
        
        botonContinuar.onClick.RemoveAllListeners();
        botonContinuar.onClick.AddListener(() => continuarPresionado = true);
        
        while (!continuarPresionado && !saltado)
        {
            yield return null;
        }
        
        botonContinuar.gameObject.SetActive(false);
    }
    
    public void SaltarFragmento()
    {
        saltado = true;
        fragmentoCompleto = true;

        // NO llamar StopCoroutine(typewriterActual): si se cancela mid-typewriter,
        // detener la coroutine hija deja a MostrarFragmento colgada en yield return.
        // El typewriter checa `saltado` internamente y termina solo en el siguiente frame.

        if (audioActual != null)
            audioActual.Stop();

        if (fragmentoActual != null && textoFragmento != null)
            textoFragmento.text = LimpiarMarcadoresInstantaneos(fragmentoActual.textoFragmento);

        Debug.Log("📖 [StoryUI] ⏭️ Fragmento saltado");
    }
    
    // ✅ MÉTODO SUAVE PARA OCULTAR (NO AFECTA OTROS UI)
    public void OcultarUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        if (audioActual != null)
        {
            Destroy(audioActual.gameObject);
            audioActual = null;
        }
        
        Debug.Log("📖 [StoryUI] UI ocultado (alpha = 0, otros UI no afectados)");
    }
    
    private void ConfigurarUI()
    {
        if (botonSaltar != null)
        {
            botonSaltar.onClick.RemoveAllListeners();
            botonSaltar.onClick.AddListener(SaltarFragmento);
        }
        
        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(false);
        }
        
        Debug.Log("📖 [StoryUI] ✅ UI configurado");
    }
    
    // ✅ CREAR UI EN CANVAS EXISTENTE (NO CREAR NUEVO CANVAS)
    private void CrearUIEnCanvasExistente()
    {
        if (canvasExistente == null)
        {
            Debug.LogError("📖 [StoryUI] ❌ No hay Canvas existente para crear UI");
            return;
        }
        
        Debug.Log($"📖 [StoryUI] 🏗️ Creando UI en Canvas existente: {canvasExistente.name}");
        
        int tamanoFuenteBase = esMobil ? tamanoFuenteBaseMobil : tamanoFuenteBaseTablet;
        
        // ✅ Panel principal - HIJO del Canvas existente
        GameObject panelObj = new GameObject("StoryPanel");
        panelObj.transform.SetParent(canvasExistente.transform, false);
        
        // ✅ CONFIGURAR SORTING ORDER RELATIVO
        Canvas panelCanvas = panelObj.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = sortingOrderStory; // Menor que otros UI críticos
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        panelPrincipal = panelObj;
        canvasGroup = panelObj.AddComponent<CanvasGroup>();

        // Crear elementos básicos
        CrearImagenFondoBase(panelObj);
        CrearImagenFondoSprite(panelObj);
        CrearTextoBasico(panelObj, tamanoFuenteBase);
        CrearBotonesBasicos(panelObj);

        // Ocultar inmediatamente — solo se muestra cuando MostrarFragmento() es invocado
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelPrincipal.SetActive(false);

        Debug.Log("📖 [StoryUI] ✅ UI creado en Canvas existente exitosamente (oculto hasta primer fragmento)");
    }
    
    private void CrearImagenFondoBase(GameObject parent)
    {
        GameObject fondoObj = new GameObject("ImagenFondoBase");
        fondoObj.transform.SetParent(parent.transform, false);
        
        RectTransform fondoRect = fondoObj.AddComponent<RectTransform>();
        fondoRect.anchorMin = Vector2.zero;
        fondoRect.anchorMax = Vector2.one;
        fondoRect.sizeDelta = Vector2.zero;
        
        imagenFondo = fondoObj.AddComponent<Image>();
        imagenFondo.color = colorFondoFallback;
    }
    
    private void CrearImagenFondoSprite(GameObject parent)
    {
        GameObject imagenSpriteObj = new GameObject("ImagenFondoSprite");
        imagenSpriteObj.transform.SetParent(parent.transform, false);
        
        RectTransform spriteRect = imagenSpriteObj.AddComponent<RectTransform>();
        spriteRect.anchorMin = Vector2.zero;
        spriteRect.anchorMax = Vector2.one;
        spriteRect.sizeDelta = Vector2.zero;
        
        imagenFondoSprite = imagenSpriteObj.AddComponent<Image>();
        imagenFondoSprite.type = Image.Type.Simple;
        imagenFondoSprite.gameObject.SetActive(false);
        
        // También crear imagenAdicional para compatibilidad
        GameObject adicionalObj = new GameObject("ImagenAdicional");
        adicionalObj.transform.SetParent(parent.transform, false);
        
        RectTransform adicionalRect = adicionalObj.AddComponent<RectTransform>();
        adicionalRect.anchorMin = Vector2.zero;
        adicionalRect.anchorMax = Vector2.one;
        adicionalRect.sizeDelta = Vector2.zero;
        
        imagenAdicional = adicionalObj.AddComponent<Image>();
        imagenAdicional.gameObject.SetActive(false);
    }
    
    private void CrearTextoBasico(GameObject parent, int tamanoFuenteBase)
    {
        GameObject textoObj = new GameObject("TextoFragmento");
        textoObj.transform.SetParent(parent.transform, false);
        
        RectTransform textoRect = textoObj.AddComponent<RectTransform>();
        if (esMobil)
        {
            textoRect.anchorMin = new Vector2(0.05f, 0.25f);
            textoRect.anchorMax = new Vector2(0.95f, 0.75f);
        }
        else
        {
            textoRect.anchorMin = new Vector2(0.1f, 0.2f);
            textoRect.anchorMax = new Vector2(0.9f, 0.8f);
        }
        textoRect.sizeDelta = Vector2.zero;
        
        textoFragmento = textoObj.AddComponent<TextMeshProUGUI>();
        textoFragmento.fontSize = tamanoFuenteBase;
        textoFragmento.color = Color.white;
        textoFragmento.alignment = TextAlignmentOptions.Center;
        textoFragmento.enableWordWrapping = true;
        textoFragmento.lineSpacing = esMobil ? 1.2f : 1.0f;
    }
    
    private void CrearBotonesBasicos(GameObject parent)
    {
        // Botón Continuar
        GameObject botonObj = new GameObject("BotonContinuar");
        botonObj.transform.SetParent(parent.transform, false);
        
        RectTransform botonRect = botonObj.AddComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(0.5f, 0.1f);
        botonRect.anchorMax = new Vector2(0.5f, 0.1f);
        botonRect.sizeDelta = esMobil ? new Vector2(280, 70) : new Vector2(200, 50);
        botonRect.anchoredPosition = Vector2.zero;
        
        Image botonImg = botonObj.AddComponent<Image>();
        botonImg.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        
        botonContinuar = botonObj.AddComponent<Button>();
        botonContinuar.gameObject.SetActive(false);
        
        // Botón Saltar
        GameObject saltarObj = new GameObject("BotonSaltar");
        saltarObj.transform.SetParent(parent.transform, false);
        
        RectTransform saltarRect = saltarObj.AddComponent<RectTransform>();
        saltarRect.anchorMin = new Vector2(1f, 1f);
        saltarRect.anchorMax = new Vector2(1f, 1f);
        saltarRect.sizeDelta = esMobil ? new Vector2(140, 60) : new Vector2(100, 40);
        saltarRect.anchoredPosition = esMobil ? new Vector2(-80, -40) : new Vector2(-60, -30);
        
        Image saltarImg = saltarObj.AddComponent<Image>();
        saltarImg.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        
        botonSaltar = saltarObj.AddComponent<Button>();
    }
    
    [ContextMenu("🧪 Test UI Básico")]
    public void TestUIBasico()
    {
        StartCoroutine(TestUICoroutine());
    }
    
    private IEnumerator TestUICoroutine()
    {
        StoryFragment fragmentoPrueba = ScriptableObject.CreateInstance<StoryFragment>();
        fragmentoPrueba.fragmentID = "test_integrado";
        fragmentoPrueba.nombreFragmento = "Prueba Versión Integrada";
        fragmentoPrueba.textoFragmento = "✅ PRUEBA INTEGRADA: El Canvas no interfiere con inventario ni misiones.";
        fragmentoPrueba.usarTypewriter = false;
        fragmentoPrueba.avanceAutomatico = false;
        
        yield return StartCoroutine(MostrarFragmento(fragmentoPrueba));
        
        Debug.Log("📖 [StoryUI] ✅ Test integrado completado");
    }
    
    [ContextMenu("📖 Debug Estado Completo")]
    public void DebugEstadoCompleto()
    {
        Debug.Log("=== 📖 ESTADO STORY UI INTEGRADO ===");
        Debug.Log($"Canvas Existente: {canvasExistente?.name ?? "NULL"}");
        Debug.Log($"Panel Principal: {panelPrincipal != null} | Activo: {(panelPrincipal != null ? panelPrincipal.activeInHierarchy : false)}");
        Debug.Log($"CanvasGroup: {canvasGroup != null} | Alpha: {(canvasGroup != null ? canvasGroup.alpha.ToString("F2") : "N/A")}");
        Debug.Log($"Sorting Order: {sortingOrderStory}");
        Debug.Log($"Respetar Otros Canvas: {respetarOtrosCanvas}");
        Debug.Log($"Inventario Toggle: {inventarioToggle != null}");
        Debug.Log($"ObjectInfo UI: {objectInfoUI != null}");
        Debug.Log($"Dispositivo: {(esMobil ? "MÓVIL" : "TABLET")}");
        Debug.Log($"Fragmento Actual: {fragmentoActual?.fragmentID ?? "ninguno"}");
        
        // Estado de otros sistemas
        if (inventarioToggle != null)
        {
            Debug.Log($"Inventario Panel Visible: {inventarioToggle.EstaPanelVisible()}");
        }
        
        if (objectInfoUI != null)
        {
            Debug.Log($"ObjectInfo Canvas Activo: {objectInfoUI.TieneCanvasActivo()}");
        }
        
        Debug.Log("========================================");
    }
    
    void OnDestroy()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }
        
        if (typewriterActual != null)
        {
            StopCoroutine(typewriterActual);
        }
        
        if (audioActual != null && audioActual.gameObject != null)
        {
            Destroy(audioActual.gameObject);
        }
    }
}