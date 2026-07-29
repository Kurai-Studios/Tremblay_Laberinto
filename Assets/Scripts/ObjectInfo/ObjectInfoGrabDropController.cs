using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador de agarrar/colocar objetos para ObjectInfo - Maneja objetos en la cámara
/// </summary>
public class ObjectInfoGrabDropController : MonoBehaviour
{
    // 🎃 Se dispara al agarrar un objeto para llevarlo a completar (calabaza reactiva).
    // Estático porque este componente no es singleton (mismo patrón que VuforiaTargetTracker).
    public static event Action<string> OnObjetoAgarrado;

    [Header("Grab Drop Configuration")]
    [SerializeField] private GameObject panelObjetoAgarrado;
    [SerializeField] private TextMeshProUGUI textoObjetoAgarrado;
    [SerializeField] private Button botonAbandonar;
    
    [Header("Debug Configuration")]
    [SerializeField] private bool debugAgarrarDetallado = true;

    [Header("Panel Objeto Agarrado - Posición/Transparencia")]
    [Tooltip("Desplazamiento (en X e Y) aplicado al panel cuando NO está en el Image Target donde se agarró el objeto")]
    [SerializeField] private Vector2 offsetPanelLateral = new Vector2(-400f, 0f);
    [Tooltip("Transparencia del panel cuando está desplazado")]
    [SerializeField] private float alphaPanelLateral = 0.4f;
    [SerializeField] private float duracionTransicionPanel = 0.3f;

    // Estado del sistema
    private string objetoAgarradoID = "";
    private string objetoIDOrigenAgarre = "";
    private GameObject objetoClonAgarrado;
    private Dictionary<string, List<string>> objetosColocadosEnDestino = new Dictionary<string, List<string>>();

    private RectTransform panelObjetoAgarradoRect;
    private CanvasGroup panelObjetoAgarradoCanvasGroup;
    private Vector2 posicionPanelCentral;
    private Coroutine transicionPanelCR;
    private bool panelCentrado = true;
    
    // Referencias a otros componentes
    private ObjectInfoUIManager mainManager;
    private GameObjectManager gameObjectManager;
    private ObjectInfoMissionPanel missionPanel;
    
    void Awake()
    {
        mainManager = GetComponent<ObjectInfoUIManager>();
        gameObjectManager = GameObjectManager.Instance;
        missionPanel = GetComponent<ObjectInfoMissionPanel>();
    }
    
    void Start()
    {
        ConfigurarBotonAbandonar();
        InicializarPanelObjetoAgarrado();
        OcultarPanelObjetoAgarrado();
    }

    private void InicializarPanelObjetoAgarrado()
    {
        if (panelObjetoAgarrado == null) return;

        panelObjetoAgarradoRect = panelObjetoAgarrado.GetComponent<RectTransform>();

        panelObjetoAgarradoCanvasGroup = panelObjetoAgarrado.GetComponent<CanvasGroup>();
        if (panelObjetoAgarradoCanvasGroup == null)
            panelObjetoAgarradoCanvasGroup = panelObjetoAgarrado.AddComponent<CanvasGroup>();

        if (panelObjetoAgarradoRect != null)
            posicionPanelCentral = panelObjetoAgarradoRect.anchoredPosition;
    }
    
    private void ConfigurarBotonAbandonar()
    {
        if (botonAbandonar != null)
        {
            botonAbandonar.onClick.RemoveAllListeners();
            botonAbandonar.onClick.AddListener(AbandonarObjeto);
        }
    }
    
    /// <summary>
    /// Agarra un objeto y lo coloca en la cámara
    /// </summary>
    public void AgarrarObjeto(string objetoID)
    {
        Debug.Log($"🎯 [GrabDrop] ===== INICIO PROCESO AGARRAR =====");
        Debug.Log($"🎯 [GrabDrop] ObjetoID recibido: '{objetoID}'");

        // 1. Verificar Camera.main
        if (Camera.main == null)
        {
            Debug.LogError($"❌ [GrabDrop] Camera.main es NULL!");
            return;
        }

        // 2. Verificar GameObjectManager
        if (gameObjectManager == null)
        {
            Debug.LogError($"❌ [GrabDrop] GameObjectManager es NULL!");
            return;
        }

        // 3. Buscar datos del objeto
        GameObjectData datos = gameObjectManager.BuscarObjetoPorId(objetoID);
        if (datos == null)
        {
            Debug.LogError($"❌ [GrabDrop] No se encontraron datos para ID: '{objetoID}'");
            return;
        }

        // 4. GUARDAR EL ID DEL OBJETO AGARRADO
        objetoAgarradoID = objetoID;
        objetoIDOrigenAgarre = objetoID;
        Debug.Log($"✅ [GrabDrop] ID del objeto agarrado guardado: '{objetoAgarradoID}'");

        // 5. Verificar y cargar prefab
        if (string.IsNullOrEmpty(datos.prefab3DPath))
        {
            Debug.LogError($"❌ [GrabDrop] Ruta del prefab3D está vacía para: {datos.nombreEspanol}");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(datos.prefab3DPath);
        if (prefab == null)
        {
            Debug.LogError($"❌ [GrabDrop] No se pudo cargar prefab desde: '{datos.prefab3DPath}'");
            return;
        }

        // 6. Limpiar objeto anterior
        if (objetoClonAgarrado != null) 
        {
            Destroy(objetoClonAgarrado);
        }

        // 7. Crear clon y posicionarlo
        objetoClonAgarrado = Instantiate(prefab);
        objetoClonAgarrado.name = $"AGARRADO_{datos.nombreEspanol}";

        // 8. Configurar posición del objeto agarrado
        ConfigurarPosicionObjetoAgarrado(datos);

        // 9. Configurar colisiones
        ConfigurarColisionesObjetoAgarrado();

        // 10. Mostrar panel temporal
        MostrarPanelObjetoAgarrado(datos.nombreEspanol);

        // 11. Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoAgarrarItem();
        }

        // 🎃 Notificar a la calabaza reactiva (objeto agarrado para completar)
        OnObjetoAgarrado?.Invoke(objetoID);

        // 🔄 Refrescar botones: el de "agarrar" debe ocultarse ahora que llevas el objeto
        if (mainManager != null)
            mainManager.ActualizarBotonesDinamicamente();

        Debug.Log($"🎯 [GrabDrop] ===== PROCESO COMPLETADO =====");
    }
    
    private void ConfigurarPosicionObjetoAgarrado(GameObjectData datos)
    {
        Transform camara = Camera.main.transform;
        
        Vector3 posicion;
        Vector3 rotacion;
        Vector3 escala;
        
        if (datos.usarConfiguracionPersonalizada)
        {
            // Usar configuración específica del objeto
            posicion = datos.posicionAgarradoPersonalizada;
            rotacion = datos.rotacionAgarradaPersonalizada;
            escala = datos.escalaAgarradaPersonalizada;
            
            if (debugAgarrarDetallado)
            {
                Debug.Log($"🎮 [GrabDrop] Usando config personalizada para {datos.nombreEspanol}:");
                Debug.Log($"   Posición: {posicion}");
                Debug.Log($"   Rotación: {rotacion}");
                Debug.Log($"   Escala: {escala}");
            }
        }
        else
        {
            // Configuración por defecto
            posicion = new Vector3(0, -0.2f, 0.5f);
            rotacion = Vector3.zero;
            escala = Vector3.one;
            
            if (debugAgarrarDetallado)
            {
                Debug.Log($"🎮 [GrabDrop] Usando config por defecto para {datos.nombreEspanol}");
            }
        }
        
        // Aplicar transformaciones
        objetoClonAgarrado.transform.SetParent(camara, false);
        objetoClonAgarrado.transform.localPosition = posicion;
        objetoClonAgarrado.transform.localEulerAngles = rotacion;
        objetoClonAgarrado.transform.localScale = escala;
        
        if (debugAgarrarDetallado)
        {
            Debug.Log($"🎮 [GrabDrop] Objeto posicionado en cámara:");
            Debug.Log($"   Pos final: {objetoClonAgarrado.transform.localPosition}");
            Debug.Log($"   Rot final: {objetoClonAgarrado.transform.localEulerAngles}");
            Debug.Log($"   Escala final: {objetoClonAgarrado.transform.localScale}");
        }
    }
    
    private void ConfigurarColisionesObjetoAgarrado()
    {
        // Desactivar todos los colliders del objeto agarrado
        Collider[] colliders = objetoClonAgarrado.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        if (debugAgarrarDetallado && colliders.Length > 0)
        {
            Debug.Log($"🎮 [GrabDrop] {colliders.Length} colliders desactivados en objeto agarrado");
        }
    }
    
    /// <summary>
    /// Coloca el objeto agarrado en un destino
    /// </summary>
    public void ColocarObjetoAgarrado(string objetoDestinoID)
    {
        Debug.Log($"🎯 [GrabDrop] ===== INICIO COLOCACIÓN =====");
        Debug.Log($"🎯 [GrabDrop] Destino ID: '{objetoDestinoID}'");

        if (string.IsNullOrEmpty(objetoAgarradoID))
        {
            Debug.LogWarning("⚠️ [GrabDrop] No hay objeto agarrado para colocar");
            return;
        }

        if (objetoClonAgarrado == null)
        {
            Debug.LogError("❌ [GrabDrop] objetoClonAgarrado es NULL pero objetoAgarradoID no está vacío");
            objetoAgarradoID = "";
            return;
        }

        // Agregar objeto a la lista de colocados
        if (!objetosColocadosEnDestino.ContainsKey(objetoDestinoID))
        {
            objetosColocadosEnDestino[objetoDestinoID] = new List<string>();
        }

        // 🚫 No duplicar: un objeto solo puede estar UNA vez en cada destino.
        // Si ya está, se mantiene agarrado (no se destruye) y se avisa.
        if (objetosColocadosEnDestino[objetoDestinoID].Contains(objetoAgarradoID))
        {
            Debug.Log($"🚫 [GrabDrop] '{objetoAgarradoID}' ya está colocado en {objetoDestinoID} — no se duplica");

            if (GlobalAudioManager.Instance != null)
                GlobalAudioManager.Instance.ReproducirSonidoSoltarFallido();

            if (mainManager != null)
                mainManager.MostrarMensajeMision("You already placed that one.");

            return; // el objeto sigue en la mano; el jugador puede llevarlo a otro lado o abandonarlo
        }

        objetosColocadosEnDestino[objetoDestinoID].Add(objetoAgarradoID);

        Debug.Log($"📦 [GrabDrop] Objeto {objetoAgarradoID} colocado en {objetoDestinoID}");
        Debug.Log($"📦 [GrabDrop] Total objetos en {objetoDestinoID}: {objetosColocadosEnDestino[objetoDestinoID].Count}");

        // Crear sprite visual si hay MissionPanel
        if (missionPanel != null)
        {
            missionPanel.CrearSpriteObjetoColocado(objetoAgarradoID);
        }

        // Audio feedback
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoSoltarExitoso();
        }

        // Limpiar objeto 3D agarrado
        Debug.Log($"🧹 [GrabDrop] Destruyendo objeto 3D visual");
        Destroy(objetoClonAgarrado);
        objetoClonAgarrado = null;
        objetoAgarradoID = "";
        OcultarPanelObjetoAgarrado();

        // Actualizar botones
        if (mainManager != null)
        {
            mainManager.ActualizarBotonesDinamicamente();
        }

        Debug.Log($"🎯 [GrabDrop] ===== PROCESO COMPLETADO =====");
    }
    
    /// <summary>
    /// Abandona el objeto agarrado
    /// </summary>
    public void AbandonarObjeto()
    {
        if (objetoClonAgarrado == null) return;

        Debug.Log($"🚫 [GrabDrop] Abandonando objeto: {objetoClonAgarrado.name}");
        
        // Destruir el objeto 3D agarrado
        Destroy(objetoClonAgarrado);
        objetoClonAgarrado = null;
        objetoAgarradoID = "";

        // Ocultar panel temporal
        OcultarPanelObjetoAgarrado();

        // Sonido de cancelar
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
        }

        // Actualizar botones
        if (mainManager != null)
        {
            mainManager.ActualizarBotonesDinamicamente();
        }
    }
    
    /// <summary>
    /// Muestra el panel temporal cuando se agarra un objeto
    /// </summary>
    private void MostrarPanelObjetoAgarrado(string nombreObjeto)
    {
        if (panelObjetoAgarrado == null) return;

        panelObjetoAgarrado.SetActive(true);

        if (textoObjetoAgarrado != null)
        {
            textoObjetoAgarrado.text = nombreObjeto;
        }

        // Al agarrar, el panel siempre arranca centrado y opaco
        if (transicionPanelCR != null)
        {
            StopCoroutine(transicionPanelCR);
            transicionPanelCR = null;
        }

        if (panelObjetoAgarradoRect != null)
            panelObjetoAgarradoRect.anchoredPosition = posicionPanelCentral;

        if (panelObjetoAgarradoCanvasGroup != null)
            panelObjetoAgarradoCanvasGroup.alpha = 1f;

        panelCentrado = true;
    }

    /// <summary>
    /// Oculta el panel temporal de objeto agarrado
    /// </summary>
    private void OcultarPanelObjetoAgarrado()
    {
        if (transicionPanelCR != null)
        {
            StopCoroutine(transicionPanelCR);
            transicionPanelCR = null;
        }

        if (panelObjetoAgarrado != null)
        {
            panelObjetoAgarrado.SetActive(false);
        }

        // Resetear posición/alpha para el próximo agarre
        if (panelObjetoAgarradoRect != null)
            panelObjetoAgarradoRect.anchoredPosition = posicionPanelCentral;

        if (panelObjetoAgarradoCanvasGroup != null)
            panelObjetoAgarradoCanvasGroup.alpha = 1f;

        panelCentrado = true;
        objetoIDOrigenAgarre = "";
    }

    /// <summary>
    /// Llamado cuando el jugador enfoca un Image Target distinto.
    /// Si hay un objeto agarrado y el target enfocado NO es donde se agarró,
    /// el panel se desliza a la izquierda y se vuelve semitransparente
    /// (sigue disponible/usable, pero indica que no aplica aquí).
    /// Si vuelve al target de origen, el panel regresa al centro y opaco.
    /// </summary>
    public void NotificarObjetoEnfocado(string objetoID)
    {
        if (!TieneObjetoAgarrado()) return;
        if (panelObjetoAgarrado == null || !panelObjetoAgarrado.activeSelf) return;

        bool debeCentrarse = (objetoID == objetoIDOrigenAgarre);
        if (debeCentrarse == panelCentrado) return;

        panelCentrado = debeCentrarse;

        if (transicionPanelCR != null)
            StopCoroutine(transicionPanelCR);

        transicionPanelCR = StartCoroutine(AnimarPanelObjetoAgarrado(debeCentrarse));
    }

    private IEnumerator AnimarPanelObjetoAgarrado(bool centrar)
    {
        if (panelObjetoAgarradoRect == null || panelObjetoAgarradoCanvasGroup == null)
        {
            transicionPanelCR = null;
            yield break;
        }

        Vector2 posicionInicial = panelObjetoAgarradoRect.anchoredPosition;
        Vector2 posicionFinal = centrar ? posicionPanelCentral : posicionPanelCentral + offsetPanelLateral;

        float alphaInicial = panelObjetoAgarradoCanvasGroup.alpha;
        float alphaFinal = centrar ? 1f : alphaPanelLateral;

        float tiempo = 0f;
        while (tiempo < duracionTransicionPanel)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionTransicionPanel);

            panelObjetoAgarradoRect.anchoredPosition = Vector2.Lerp(posicionInicial, posicionFinal, t);
            panelObjetoAgarradoCanvasGroup.alpha = Mathf.Lerp(alphaInicial, alphaFinal, t);

            yield return null;
        }

        panelObjetoAgarradoRect.anchoredPosition = posicionFinal;
        panelObjetoAgarradoCanvasGroup.alpha = alphaFinal;
        transicionPanelCR = null;
    }
    
    /// <summary>
    /// Verifica si hay un objeto actualmente agarrado
    /// </summary>
    public bool TieneObjetoAgarrado()
    {
        return !string.IsNullOrEmpty(objetoAgarradoID) && objetoClonAgarrado != null;
    }

    /// <summary>
    /// ID del objeto del que se agarró el objeto actual (su origen). Se usa para NO mostrar
    /// el botón "colocar" sobre el mismo objeto del que se tomó.
    /// </summary>
    public string ObjetoIDOrigenAgarre => objetoIDOrigenAgarre;
    
    /// <summary>
    /// Obtiene la lista de objetos colocados en un destino específico
    /// </summary>
    public List<string> ObtenerObjetosColocados(string objetoDestinoID)
    {
        if (objetosColocadosEnDestino.ContainsKey(objetoDestinoID))
        {
            return new List<string>(objetosColocadosEnDestino[objetoDestinoID]);
        }
        return new List<string>();
    }
    
    /// <summary>
    /// Limpia todos los objetos colocados
    /// </summary>
    public void LimpiarObjetosColocados()
    {
        objetosColocadosEnDestino.Clear();
        Debug.Log("[GrabDrop] Objetos colocados limpiados");
    }
    
    /// <summary>
    /// Retira UN objeto colocado de un destino (para poder quitarlo del panel de comprobación
    /// si se puso mal). No lo vuelve a agarrar: solo lo saca de la lista.
    /// </summary>
    public bool RetirarObjetoColocado(string objetoDestinoID, string objetoID)
    {
        if (string.IsNullOrEmpty(objetoDestinoID) || string.IsNullOrEmpty(objetoID)) return false;

        if (objetosColocadosEnDestino.TryGetValue(objetoDestinoID, out var lista))
        {
            bool removido = lista.Remove(objetoID);
            if (removido)
                Debug.Log($"[GrabDrop] Objeto retirado del panel: {objetoID} (destino {objetoDestinoID})");
            return removido;
        }
        return false;
    }

    /// <summary>
    /// Limpia los objetos colocados de un destino específico
    /// </summary>
    public void LimpiarObjetosColocadosDestino(string objetoDestinoID)
    {
        if (objetosColocadosEnDestino.ContainsKey(objetoDestinoID))
        {
            objetosColocadosEnDestino[objetoDestinoID].Clear();
            Debug.Log($"[GrabDrop] Objetos colocados limpiados para {objetoDestinoID}");
        }
    }
    
    // Getters públicos para compatibilidad
    public string ObjetoAgarradoID => objetoAgarradoID;
    public GameObject ObjetoClonAgarrado => objetoClonAgarrado;
    public Dictionary<string, List<string>> ObjetosColocadosEnDestino => objetosColocadosEnDestino;
    
    // Métodos de debug
    [ContextMenu("🎯 Debug Estado Grab/Drop")]
    public void DebugEstadoGrabDrop()
    {
        Debug.Log("=== 🎯 ESTADO GRAB/DROP ===");
        Debug.Log($"Objeto agarrado ID: '{objetoAgarradoID}'");
        Debug.Log($"Objeto clon existe: {objetoClonAgarrado != null}");
        Debug.Log($"Panel agarrado activo: {panelObjetoAgarrado != null && panelObjetoAgarrado.activeInHierarchy}");
        
        Debug.Log($"Objetos colocados en destinos:");
        foreach (var kvp in objetosColocadosEnDestino)
        {
            Debug.Log($"   {kvp.Key}: [{string.Join(", ", kvp.Value)}]");
        }
    }
}