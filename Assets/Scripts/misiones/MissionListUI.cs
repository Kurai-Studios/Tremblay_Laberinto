using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 🗂️ Estados por los que se puede filtrar la lista de misiones.
// El orden de los valores DEBE coincidir con el array 'botonesFiltro' del Inspector.
public enum FiltroMision
{
    Todas = 0,
    Disponible = 1,
    Descifrada = 2,
    Completada = 3,
    Bloqueada = 4
}

public class MissionListUI : MonoBehaviour
{
    [Header("Referencias")]
    public MissionManager missionManager;
    public Transform contenedorLista;
    public GameObject missionItemPrefab;

    [Header("Toggle Panel (Opcional)")]
    [SerializeField] private bool usarToggle = false; // 👈 NUEVO: Desactivar funcionalidad de toggle
    [SerializeField] private Button botonToggle;
    [SerializeField] private GameObject panelMisiones;
    [SerializeField] private TextMeshProUGUI textoBoton;
    
    [Header("Animación Slide (Solo si usarToggle = true)")]
    [SerializeField] private float duracionAnimacion = 0.5f;
    [SerializeField] private AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float alturaPanel = 300f;

    [Header("🎯 Indicador de Misión Seleccionada")]
[SerializeField] private bool mostrarMisionSeleccionada = true;
[SerializeField] private Color colorMisionActiva = new Color(1f, 0.84f, 0f, 1f); // Dorado
[SerializeField] private float anchoBordeMisionActiva = 4f;
[SerializeField] private string nombreBordeSeleccion = "BordeSeleccion";

private GameObject itemMisionActual; // Referencia al item actualmente seleccionado
    
    [Header("Configuración Visual")]
    [SerializeField] private string textoMostrar = "▼ Mostrar Misiones";
    [SerializeField] private string textoOcultar = "▲ Ocultar Misiones";

    private readonly List<GameObject> itemsInstanciados = new List<GameObject>();
    private bool panelVisible = true; // 👈 CAMBIADO: Por defecto visible
    private RectTransform rectTransformPanel;
    private CanvasGroup canvasGroupPanel;
    private Coroutine animacionActual;
    
    private Vector2 posicionOculta;
    private Vector2 posicionVisible;

    [Header("🎨 Indicador de Estado")]
[SerializeField] private string nombreIndicadorEstado = "IndicadorEstado";
[SerializeField] private float anchoIndicador = 10f;

    [Header("🎨 Configuración de Colores de Fondo")]
    [SerializeField] private Color colorMisionSeleccionable = Color.cyan;
    [SerializeField] private Color colorMisionNoSeleccionable = Color.gray;
    
    [Header("🎨 Configuración de Colores de Texto por Estado")]
    [SerializeField] private Color colorCompletada = Color.green;
    [SerializeField] private Color colorDescifrada = Color.yellow;
    [SerializeField] private Color colorDisponible = Color.blue;
    [SerializeField] private Color colorBloqueada = Color.red;
    [SerializeField] private Color colorDesconocido = Color.gray;
    
    [Header("📝 Configuración de Mensajes de Estado")]
    [SerializeField] private string mensajeCompletada = "Completada en AR";
    [SerializeField] private string mensajeDescifrada = "Descifrada - Ve al AR";
    [SerializeField] private string mensajeDisponible = "Disponible para descifrar";
    [SerializeField] private string mensajeBloqueada = "Bloqueada";
    [SerializeField] private string mensajeDesconocido = "Desconocido";

    [Header("🗂️ Filtros por Estado")]
    [Tooltip("Botones de filtro EN ESTE ORDEN: [0]Todas, [1]Disponibles, [2]Descifradas, [3]Completadas, [4]Bloqueadas.")]
    [SerializeField] private Button[] botonesFiltro;
    [Tooltip("Color del borde que resalta el filtro activo (NO toca el color de fondo del botón).")]
    [SerializeField] private Color colorBordeActivo = new Color(1f, 0.84f, 0f, 1f); // dorado
    [SerializeField] private float grosorBordeActivo = 4f;

    private FiltroMision filtroActual = FiltroMision.Todas;

    void Start()
    {
        InicializarComponentes();
        
        // Solo configurar botón toggle si está habilitado
        if (usarToggle)
        {
            ConfigurarBotonToggle();
            CalcularPosiciones();
        }
        
        if (missionManager != null)
        {
            missionManager.OnMisionesActualizadas += ActualizarLista;
        }

        // Configurar estado inicial (visible)
        ConfigurarEstadoInicial();

        // 🗂️ Dejar marcado el filtro inicial (Todas)
        ActualizarResaltadoFiltros();

        // Primera carga con delay
        Invoke(nameof(ActualizarLista), 0.1f);
    }

    // 🆕 NUEVO: Crear borde visual para misión seleccionada
private void CrearBordeSeleccion(GameObject item)
{
    // Buscar si ya existe el borde
    Transform bordeTransform = item.transform.Find(nombreBordeSeleccion);
    
    if (bordeTransform == null)
    {
        // Crear el borde
        GameObject bordeObj = new GameObject(nombreBordeSeleccion);
        bordeObj.transform.SetParent(item.transform, false);
        
        RectTransform rect = bordeObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Componente Outline para crear el borde
        Outline outline = bordeObj.AddComponent<Outline>();
        outline.effectColor = colorMisionActiva;
        outline.effectDistance = new Vector2(anchoBordeMisionActiva, anchoBordeMisionActiva);
        outline.useGraphicAlpha = true;
        
        // O usar Image si prefieres un borde más grueso
        Image bordeImg = bordeObj.AddComponent<Image>();
        bordeImg.color = Color.clear; // Transparente, solo se ve el outline
        bordeImg.raycastTarget = false;
    }
    else
    {
        // Si ya existe, asegurarse que esté visible
        bordeTransform.gameObject.SetActive(true);
    }
}

// 🆕 NUEVO: Eliminar borde de selección
private void EliminarBordeSeleccion(GameObject item)
{
    if (item == null) return;
    
    Transform bordeTransform = item.transform.Find(nombreBordeSeleccion);
    if (bordeTransform != null)
    {
        bordeTransform.gameObject.SetActive(false);
    }
}

// 🆕 NUEVO: Actualizar indicador de misión activa
public void ActualizarMisionSeleccionada(Mission misionActual)
{
    if (!mostrarMisionSeleccionada) return;
    
    // Remover borde del item anterior
    if (itemMisionActual != null)
    {
        EliminarBordeSeleccion(itemMisionActual);
    }
    
    // Encontrar el item correspondiente a la misión actual
    if (misionActual != null)
    {
        for (int i = 0; i < itemsInstanciados.Count; i++)
        {
            var item = itemsInstanciados[i];
            if (item == null) continue;
            
            // Comparar por índice (asumiendo que el orden se mantiene)
            if (i < missionManager.misiones.Count && 
                missionManager.misiones[i].misionID == misionActual.misionID)
            {
                itemMisionActual = item;
                CrearBordeSeleccion(item);
                
                Debug.Log($"[MissionListUI] ✅ Misión activa marcada: {misionActual.descripcion}");
                break;
            }
        }
    }
}
/// <summary>
/// Obtiene la prioridad de ordenamiento de una misión según su estado.
/// Menor número = Mayor prioridad (aparece primero)
/// </summary>
private int ObtenerPrioridadOrdenamiento(Mission mision)
{
    if (missionManager == null) return 999; // Sin manager, al final

    
    // 1. Descifradas (listas para completar en AR)
    if (missionManager.MisionesDescifradas.Contains(mision.misionID) &&
        !missionManager.MisionesCompletadas.Contains(mision.misionID))
        return 1;

    // 2. Disponibles (las que se pueden descifrar ahora)
    if (missionManager.MisionesDisponibles.Contains(mision) &&
        !missionManager.MisionesDescifradas.Contains(mision.misionID) &&
        !missionManager.MisionesCompletadas.Contains(mision.misionID))
        return 2;

    // 3. Completadas
    if (missionManager.MisionesCompletadas.Contains(mision.misionID))
        return 3;

    // 4. Bloqueadas
    return 4;
}

    private void InicializarComponentes()
    {
        if (panelMisiones == null)
        {
            panelMisiones = contenedorLista.parent.gameObject;
        }

        rectTransformPanel = panelMisiones.GetComponent<RectTransform>();
        if (rectTransformPanel == null)
        {
            rectTransformPanel = panelMisiones.AddComponent<RectTransform>();
        }

        canvasGroupPanel = panelMisiones.GetComponent<CanvasGroup>();
        if (canvasGroupPanel == null)
        {
            canvasGroupPanel = panelMisiones.AddComponent<CanvasGroup>();
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
        posicionOculta = new Vector2(posicionVisible.x, posicionVisible.y + alturaPanel);
        
        Debug.Log($"[MissionListUI] Posición visible: {posicionVisible}, Posición oculta: {posicionOculta}");
    }

    private void ConfigurarBotonToggle()
    {
        if (botonToggle != null)
        {
            botonToggle.onClick.RemoveAllListeners();
            botonToggle.onClick.AddListener(TogglePanel);
            
            // Ocultar botón si no se usa toggle
            if (!usarToggle)
            {
                botonToggle.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("[MissionListUI] Botón toggle no asignado.");
        }
    }

    private void ConfigurarEstadoInicial()
    {
        if (usarToggle)
        {
            // Empieza oculto cuando el toggle está activo
            panelVisible = false;

            if (rectTransformPanel != null)
                rectTransformPanel.anchoredPosition = posicionOculta;

            if (canvasGroupPanel != null)
            {
                // No se desvanece: el panel solo se desliza hacia arriba y la
                // parte que sobresale (el botón/persiana) debe seguir visible
                // y clickeable aunque esté "cerrado".
                canvasGroupPanel.alpha = 1f;
                canvasGroupPanel.interactable = true;
                canvasGroupPanel.blocksRaycasts = true;
            }
        }
        else
        {
            panelVisible = true;

            if (canvasGroupPanel != null)
            {
                canvasGroupPanel.alpha = 1f;
                canvasGroupPanel.interactable = true;
                canvasGroupPanel.blocksRaycasts = true;
            }
        }

        if (panelMisiones != null)
            panelMisiones.SetActive(true);

        ActualizarTextoBoton();
    }

    public void TogglePanel()
    {
        // Solo funciona si usarToggle está activado
        if (!usarToggle)
        {
            Debug.LogWarning("[MissionListUI] Toggle desactivado. El panel está siempre visible.");
            return;
        }

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }

        panelVisible = !panelVisible;
        
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoTogglePanel();
        }
        
        animacionActual = StartCoroutine(AnimarSlidePanel(panelVisible));
        
        ActualizarTextoBoton();
        
        Debug.Log($"[MissionListUI] Panel {(panelVisible ? "deslizando hacia abajo" : "deslizando hacia arriba")}");
    }

    private IEnumerator AnimarSlidePanel(bool mostrar)
    {
        float tiempoTranscurrido = 0f;
        
        Vector2 posicionInicial = mostrar ? posicionOculta : posicionVisible;
        Vector2 posicionFinal = mostrar ? posicionVisible : posicionOculta;

        // El panel nunca se desvanece: se mantiene visible e interactuable en
        // todo momento para que la parte que sobresale (botón/persiana) siga
        // clickeable. Solo animamos el deslizamiento.
        if (canvasGroupPanel != null)
        {
            canvasGroupPanel.alpha = 1f;
            canvasGroupPanel.interactable = true;
            canvasGroupPanel.blocksRaycasts = true;
        }

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
        if (textoBoton != null && usarToggle)
        {
            textoBoton.text = panelVisible ? textoOcultar : textoMostrar;
        }
    }

    public void ActualizarLista()
{
    // Limpiar lista anterior
    foreach (var go in itemsInstanciados)
        Destroy(go);
    itemsInstanciados.Clear();

    if (missionManager == null || missionManager.misiones.Count == 0) return;

    // Pre-computar índices para evitar IndexOf O(n) dentro del sort
    var indiceOriginal = missionManager.misiones
        .Select((m, i) => (m, i))
        .Where(x => x.m != null)
        .ToDictionary(x => x.m, x => x.i);

    // 🎯 ORDENAR MISIONES POR PRIORIDAD DE ESTADO (filtradas por el estado activo)
    var misionesOrdenadas = indiceOriginal.Keys
        .Where(CoincideConFiltro)                       // 🗂️ filtro por estado
        .OrderBy(m => ObtenerPrioridadOrdenamiento(m))
        .ThenBy(m => indiceOriginal[m])
        .ToList();

    // Crear items en el orden determinado
    foreach (var mision in misionesOrdenadas)
    {
        GameObject item = Instantiate(missionItemPrefab, contenedorLista);
        itemsInstanciados.Add(item);

        ConfigurarItemMision(item, mision);
    }

    Debug.Log($"[MissionListUI] Lista actualizada con {itemsInstanciados.Count} misiones (ordenadas por estado)");
    
    // 🆕 NUEVO: Marcar misión activa si existe
    if (missionManager.TieneMisionCargada())
    {
        Mission misionActual = missionManager.ObtenerMisionActual();
        if (misionActual != null)
        {
            ActualizarMisionSeleccionada(misionActual);
        }
    }
}

    // 🆕 MÉTODO CORREGIDO: Configurar el ColorBlock del botón SIN transparencia
private void ConfigurarColorBlockBoton(Button boton, bool esSeleccionable)
{
    ColorBlock colorBlock = boton.colors;
    
    if (esSeleccionable)
    {
        // Para misiones seleccionables: colores sutiles CON ALPHA = 1
        colorBlock.normalColor = new Color(1f, 1f, 1f, 1f);              // Blanco opaco
        colorBlock.highlightedColor = new Color(0.95f, 0.95f, 1f, 1f);   // Azul claro opaco
        colorBlock.pressedColor = new Color(0.85f, 0.85f, 0.95f, 1f);    // Azul marcado opaco
        colorBlock.selectedColor = new Color(0.9f, 0.9f, 1f, 1f);        // Azul medio opaco
        colorBlock.disabledColor = new Color(0.78f, 0.78f, 0.78f, 1f);   // Gris opaco
    }
    else
    {
        // Para misiones NO seleccionables: todo igual (gris) CON ALPHA = 1
        Color grisClaro = new Color(0.9f, 0.9f, 0.9f, 1f); // ALPHA = 1
        colorBlock.normalColor = grisClaro;
        colorBlock.highlightedColor = grisClaro;
        colorBlock.pressedColor = grisClaro;
        colorBlock.selectedColor = grisClaro;
        colorBlock.disabledColor = grisClaro;
    }
    
    // 🔧 CRÍTICO: ColorMultiplier en 1 para no modificar el alpha
    colorBlock.colorMultiplier = 1f;
    colorBlock.fadeDuration = 0.1f;
    
    boton.colors = colorBlock;
    
   
}

private void ConfigurarItemMision(GameObject item, Mission mision)
{
    var textos = item.GetComponentsInChildren<TextMeshProUGUI>();
    if (textos.Length >= 3)
    {
        textos[0].text = mision.descripcion;
        textos[1].text = mision.tipoActivacion.ToString();
        textos[2].text = ObtenerEstadoMision(mision);
        textos[2].color = ObtenerColorEstado(mision);
    }

    Button botonItem = item.GetComponent<Button>();
    if (botonItem == null)
    {
        botonItem = item.AddComponent<Button>();
    }

    bool esMisionSeleccionable = EsMisionSeleccionable(mision);
    
    botonItem.interactable = esMisionSeleccionable;
    botonItem.onClick.RemoveAllListeners();

    // 🎨 CONFIGURAR IMAGEN DE FONDO
    Image imgFondo = item.GetComponent<Image>();
    if (imgFondo == null)
    {
        imgFondo = item.AddComponent<Image>();
    }

    if (esMisionSeleccionable)
    {
        botonItem.onClick.AddListener(() => SeleccionarMision(mision));
        
        botonItem.onClick.AddListener(() => {
            if (GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoSeleccionarMision();
            }
        });
        
        // ✅ Color de fondo para seleccionables (usar variable del Inspector)
        imgFondo.color = colorMisionSeleccionable;
    }
    else
    {
        // ❌ Color de fondo para no seleccionables (usar variable del Inspector)
        imgFondo.color = colorMisionNoSeleccionable;
    }

    // 🔧 MANTENER TRANSITION NONE para evitar cambios de color
    botonItem.transition = Selectable.Transition.None;

    // 🎨 Configurar indicador de estado lateral
    ConfigurarIndicadorEstado(item, mision);
    
    // 🔧 FORZAR CANVAS GROUP SI EXISTE (para evitar transparencias)
    CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
    if (canvasGroup != null)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}

// 🆕 NUEVO MÉTODO: Configurar el indicador visual de estado
private void ConfigurarIndicadorEstado(GameObject item, Mission mision)
{
    // Buscar el indicador en el item
    Transform indicadorTransform = item.transform.Find(nombreIndicadorEstado);
    Image indicador = null;

    if (indicadorTransform != null)
    {
        indicador = indicadorTransform.GetComponent<Image>();
    }
    else
    {
        // Si no existe, crearlo dinámicamente
        GameObject indicadorObj = new GameObject(nombreIndicadorEstado);
        indicadorObj.transform.SetParent(item.transform, false);
        
        RectTransform rect = indicadorObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(anchoIndicador, 0); // Ancho fijo, altura stretch
        rect.anchoredPosition = Vector2.zero;
        
        indicador = indicadorObj.AddComponent<Image>();
        indicador.raycastTarget = false; // No interfiere con clicks
    }

    // Asignar color según el estado
    if (indicador != null)
    {
        indicador.color = ObtenerColorEstado(mision);
        
        
    }
}

    private bool EsMisionSeleccionable(Mission mision)
    {
        if (missionManager == null) return false;

        bool estaDisponible = missionManager.MisionesDisponibles.Contains(mision);
        bool noEstaDescifrada = !missionManager.MisionesDescifradas.Contains(mision.misionID);
        bool noEstaCompletada = !missionManager.MisionesCompletadas.Contains(mision.misionID);

        return estaDisponible && noEstaDescifrada && noEstaCompletada;
    }

    private void SeleccionarMision(Mission mision)
{
    if (missionManager == null)
    {
        Debug.LogError("[MissionListUI] MissionManager no encontrado para seleccionar misión");
        return;
    }

    Debug.Log($"[MissionListUI] Misión seleccionada: {mision.descripcion} (ID: {mision.misionID})");

    int indiceMision = missionManager.misiones.IndexOf(mision);
    
    if (indiceMision >= 0)
    {
        missionManager.CargarMisionSeleccionada(indiceMision);
        Debug.Log($"[MissionListUI] Misión cargada en descifrador: {mision.descripcion}");
        
        // 🆕 NUEVO: Actualizar indicador visual
        ActualizarMisionSeleccionada(mision);
        
        // 👈 OPCIONAL: Si usarToggle está activado, cerrar panel después de seleccionar
        if (usarToggle)
        {
            OcultarPanel();
        }
        
        StartCoroutine(MostrarFeedbackSeleccion(mision.descripcion));
    }
    else
    {
        Debug.LogError($"[MissionListUI] No se pudo encontrar índice de misión: {mision.misionID}");
    }
}

    private IEnumerator MostrarFeedbackSeleccion(string nombreMision)
    {
        Debug.Log($"[MissionListUI] Feedback: Misión '{nombreMision}' seleccionada");
        yield return new WaitForSeconds(2f);
        Debug.Log($"[MissionListUI] Feedback completado");
    }

    private string ObtenerEstadoMision(Mission mision)
    {
        if (missionManager == null) return mensajeDesconocido;

        if (missionManager.MisionesCompletadas.Contains(mision.misionID))
            return mensajeCompletada;

        if (missionManager.MisionesDescifradas.Contains(mision.misionID))
            return mensajeDescifrada;

        Mission misionActual = missionManager.misiones.Find(m => m.misionID == mision.misionID);
        if (misionActual != null && missionManager.MisionesDisponibles.Contains(misionActual))
            return mensajeDisponible;

        return mensajeBloqueada;
    }

    private Color ObtenerColorEstado(Mission mision)
    {
        if (missionManager == null) return colorDesconocido;

        if (missionManager.MisionesCompletadas.Contains(mision.misionID))
            return colorCompletada;

        if (missionManager.MisionesDescifradas.Contains(mision.misionID))
            return colorDescifrada;

        Mission misionActual = missionManager.misiones.Find(m => m.misionID == mision.misionID);
        if (misionActual != null && missionManager.MisionesDisponibles.Contains(misionActual))
            return colorDisponible;

        return colorBloqueada;
    }

    // ================= 🗂️ FILTROS POR ESTADO =================

    /// <summary>
    /// Clasifica una misión en su estado (reutiliza las mismas comprobaciones que ObtenerColorEstado).
    /// </summary>
    private FiltroMision EstadoDe(Mission mision)
    {
        if (missionManager == null) return FiltroMision.Bloqueada;

        if (missionManager.MisionesCompletadas.Contains(mision.misionID))
            return FiltroMision.Completada;

        if (missionManager.MisionesDescifradas.Contains(mision.misionID))
            return FiltroMision.Descifrada;

        if (missionManager.MisionesDisponibles.Contains(mision))
            return FiltroMision.Disponible;

        return FiltroMision.Bloqueada;
    }

    private bool CoincideConFiltro(Mission mision)
    {
        return filtroActual == FiltroMision.Todas || EstadoDe(mision) == filtroActual;
    }

    /// <summary>
    /// Aplica un filtro y refresca la lista. El filtro se mantiene aunque la lista se refresque
    /// luego por OnMisionesActualizadas (porque filtroActual es un campo persistente).
    /// </summary>
    public void AplicarFiltro(FiltroMision filtro)
    {
        filtroActual = filtro;

        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();

        ActualizarResaltadoFiltros();
        ActualizarLista();

        Debug.Log($"[MissionListUI] Filtro aplicado: {filtro}");
    }

    // Envoltorios para enlazar cómodamente desde el Inspector (Button ▸ OnClick)
    public void FiltrarTodas()       => AplicarFiltro(FiltroMision.Todas);
    public void FiltrarDisponibles() => AplicarFiltro(FiltroMision.Disponible);
    public void FiltrarDescifradas() => AplicarFiltro(FiltroMision.Descifrada);
    public void FiltrarCompletadas() => AplicarFiltro(FiltroMision.Completada);
    public void FiltrarBloqueadas()  => AplicarFiltro(FiltroMision.Bloqueada);

    /// <summary>
    /// Resalta el botón del filtro activo con un borde (Outline), sin tocar su color de fondo
    /// (que el usuario pone a mano). El índice del botón debe coincidir con el valor del enum.
    /// </summary>
    private void ActualizarResaltadoFiltros()
    {
        if (botonesFiltro == null) return;

        for (int i = 0; i < botonesFiltro.Length; i++)
        {
            if (botonesFiltro[i] == null) continue;

            Outline outline = botonesFiltro[i].GetComponent<Outline>();
            if (outline == null)
                outline = botonesFiltro[i].gameObject.AddComponent<Outline>();

            outline.effectColor = colorBordeActivo;
            outline.effectDistance = new Vector2(grosorBordeActivo, grosorBordeActivo);
            outline.enabled = ((int)filtroActual == i); // solo el activo muestra el borde
        }
    }

    // 👈 NUEVOS: Métodos públicos simplificados
    public void MostrarPanel()
    {
        if (!usarToggle)
        {
            Debug.LogWarning("[MissionListUI] Panel siempre visible, no se puede ocultar");
            return;
        }

        if (!panelVisible)
        {
            TogglePanel();
        }
    }

    public void OcultarPanel()
    {
        if (!usarToggle)
        {
            Debug.LogWarning("[MissionListUI] Panel siempre visible, no se puede ocultar");
            return;
        }

        if (panelVisible)
        {
            TogglePanel();
        }
    }

    public void ForzarActualizacion()
    {
        ActualizarLista();
    }

    /// <summary>
    /// True si el panel de misiones está desplegado/visible. Lo usa InputRouter para NO
    /// disparar el raycast del AR cuando este panel (casi pantalla completa) tapa la cámara.
    /// </summary>
    public bool EstaPanelVisible() => panelVisible;

    public void RecalcularPosiciones()
    {
        if (usarToggle)
        {
            CalcularPosiciones();
            if (!panelVisible)
            {
                ConfigurarEstadoInicial();
            }
        }
    }

    void OnDestroy()
    {
        if (missionManager != null)
        {
            missionManager.OnMisionesActualizadas -= ActualizarLista;
        }

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }
    }

    // 👈 NUEVOS: Context menus para testing
    [ContextMenu("🔄 Activar/Desactivar Toggle")]
    public void ToggleUsarToggle()
    {
        usarToggle = !usarToggle;
        Debug.Log($"[MissionListUI] UsarToggle: {usarToggle}");
        
        if (botonToggle != null)
        {
            botonToggle.gameObject.SetActive(usarToggle);
        }
        
        if (!usarToggle)
        {
            ConfigurarEstadoInicial();
        }
    }

    [ContextMenu("Recalcular Posiciones")]
    public void ContextRecalcularPosiciones()
    {
        RecalcularPosiciones();
    }

    [ContextMenu("Test Slide Animation")]
    public void TestSlideAnimacion()
    {
        if (usarToggle)
        {
            TogglePanel();
        }
        else
        {
            Debug.LogWarning("[MissionListUI] Toggle desactivado");
        }
    }

    [ContextMenu("📊 Debug Estado Panel")]
    public void DebugEstadoPanel()
    {
        Debug.Log("=== 📊 ESTADO MISSION LIST UI ===");
        Debug.Log($"Usar Toggle: {usarToggle}");
        Debug.Log($"Panel Visible: {panelVisible}");
        Debug.Log($"Panel Misiones Activo: {panelMisiones?.activeSelf}");
        Debug.Log($"CanvasGroup Alpha: {canvasGroupPanel?.alpha}");
        Debug.Log($"Items Instanciados: {itemsInstanciados.Count}");
        Debug.Log($"Botón Toggle Activo: {botonToggle?.gameObject.activeSelf}");
    }

    void OnValidate()
    {
        if (Application.isPlaying && rectTransformPanel != null && usarToggle)
        {
            CalcularPosiciones();
        }
    }
}