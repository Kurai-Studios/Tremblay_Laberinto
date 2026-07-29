using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;
    
    [Header("📚 Referencias")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private GameObjectManager gameObjectManager;
    [SerializeField] private StoryUIController storyUI;
    
    [Header("📖 Configuración")]
    [SerializeField] private bool reproducirFragmentosAutomaticamente = true;
    [SerializeField] private bool permitirSaltarFragmentos = true;
    [SerializeField] private float delayEntreFragmentos = 0.5f;

    [Header("🎬 Prólogo")]
    [Tooltip("Fragmento que se reproduce automáticamente al iniciar el juego (solo la primera vez)")]
    [SerializeField] private StoryFragment fragmentoPrologo;
    [SerializeField] private float delayPrologo = 1f;
    
    [Header("💾 Sistema de Guardado")]
    [SerializeField] private string nombreArchivoProgreso = "story_progress.json";
    [SerializeField] private bool debugMode = true;
    
    private Queue<StoryFragment> colaFragmentos = new Queue<StoryFragment>();
    private StoryFragment fragmentoActual;
    private bool reproduciendoFragmento = false;
    private HashSet<string> fragmentosVistos = new HashSet<string>();
    private Coroutine reproduccionActual;
    private StoryProgressData progresoActual;
    
    void Awake()
    {
        // StoryManager es POR-ESCENA: cada cuento tiene su propia narrativa
        // (fragmentos, prólogo, referencias de inspector distintas), así que NO
        // debe persistir entre escenas. Se destruye con la escena y la siguiente
        // crea el suyo desde cero. El ÚNICO manager que persiste (DontDestroyOnLoad)
        // es GlobalAudioManager, y debe vivir en su propio GameObject dedicado,
        // separado de todos los managers por-escena.
        if (Instance != null && Instance != this)
        {
            // Duplicado dentro de la misma escena (setup erróneo): descartar solo
            // este componente, sin tocar el GameObject compartido.
            Destroy(this);
            return;
        }

        Instance = this;
        InicializarSistema();
    }
    
    void Start()
    {
        BuscarReferencias();
        SuscribirseAEventos();
        CargarProgreso();
        ReproducirPrologo();
    }

    /// <summary>
    /// Reproduce el fragmento-prólogo inicial (la intro que encadena otros) si no se ha visto.
    /// Se llama en Start y también tras un reset (Play Again), para que la intro vuelva a
    /// reproducirse sin recargar la escena.
    /// </summary>
    public void ReproducirPrologo()
    {
        if (fragmentoPrologo == null) return;
        if (FragmentoYaVisto(fragmentoPrologo.fragmentID)) return;

        StartCoroutine(ReproducirConDelay(fragmentoPrologo, delayPrologo));
    }
    
    private void InicializarSistema()
    {
        progresoActual = new StoryProgressData();
        
        if (debugMode)
            Debug.Log("📖 [StoryManager] Sistema de narrativa inicializado");
    }
    
    private void BuscarReferencias()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (gameObjectManager == null)
            gameObjectManager = GameObjectManager.Instance;
        
        if (storyUI == null)
            storyUI = FindObjectOfType<StoryUIController>();
        
        if (storyUI == null)
        {
            Debug.LogError("📖 [StoryManager] No se encontró StoryUIController. Creando UI básico...");
            CrearUIBasico();
        }
    }
    
    private void SuscribirseAEventos()
    {
        if (missionManager != null)
        {
            missionManager.OnMisionesActualizadas += HandleMisionesActualizadas;
            
            if (debugMode)
                Debug.Log("📖 [StoryManager] Suscrito a eventos de MissionManager");
        }
    }
    
    public void ReproducirFragmento(StoryFragment fragmento, bool forzarReproduccion = false)
    {
        if (fragmento == null || !fragmento.EsValido())
        {
            Debug.LogWarning("📖 [StoryManager] Intento de reproducir fragmento inválido");
            return;
        }
        
        if (!forzarReproduccion && fragmentosVistos.Contains(fragmento.fragmentID))
        {
            if (debugMode)
                Debug.Log($"📖 [StoryManager] Fragmento ya visto: {fragmento.fragmentID}");
            return;
        }
        
        if (reproduciendoFragmento)
        {
            colaFragmentos.Enqueue(fragmento);
            
            if (debugMode)
                Debug.Log($"📖 [StoryManager] Fragmento agregado a cola: {fragmento.fragmentID}");
            
            return;
        }
        
        if (reproduccionActual != null)
            StopCoroutine(reproduccionActual);
        
        reproduccionActual = StartCoroutine(ReproducirFragmentoCoroutine(fragmento));
    }
    
    private IEnumerator ReproducirFragmentoCoroutine(StoryFragment fragmento, bool esContinuacion = false)
    {
        reproduciendoFragmento = true;
        fragmentoActual = fragmento;

        if (debugMode)
            Debug.Log($"📖 [StoryManager] ▶️ Reproduciendo: {fragmento.fragmentID} (continuación: {esContinuacion})");

        if (!esContinuacion)
            PausarGameplay(true);

        if (storyUI != null)
        {
            yield return StartCoroutine(storyUI.MostrarFragmento(fragmento, esContinuacion));
        }
        else
        {
            yield return StartCoroutine(ReproducirSoloAudio(fragmento));
        }

        MarcarFragmentoVisto(fragmento.fragmentID);
        GuardarProgreso();

        if (fragmento.siguienteFragmento != null && fragmento.siguienteFragmento.EsValido())
        {
            // Si el panel quedó visible esperando un crossfade, el siguiente
            // fragmento se reproduce como continuación (sin fade del panel completo).
            bool continuarConCrossfade = storyUI != null && storyUI.PanelListoParaContinuacion;

            yield return new WaitForSeconds(fragmento.delayAntesSiguiente);
            yield return StartCoroutine(ReproducirFragmentoCoroutine(fragmento.siguienteFragmento, continuarConCrossfade));
        }
        else
        {
            PausarGameplay(false);
        }

        if (!esContinuacion)
        {
            reproduciendoFragmento = false;
            fragmentoActual = null;

            if (colaFragmentos.Count > 0)
            {
                yield return new WaitForSeconds(delayEntreFragmentos);
                StoryFragment siguiente = colaFragmentos.Dequeue();
                ReproducirFragmento(siguiente);
            }
        }
    }
    
    private IEnumerator ReproducirSoloAudio(StoryFragment fragmento)
    {
        if (fragmento.audioNarracion != null && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoSFX(
                fragmento.audioNarracion, 
                fragmento.volumenAudio
            );
            
            yield return new WaitForSeconds(fragmento.ObtenerDuracionTotal());
        }
        else
        {
            yield return new WaitForSeconds(5f);
        }
    }
    
    private void HandleMisionesActualizadas()
    {
        if (!reproducirFragmentosAutomaticamente) return;
        
        if (debugMode)
            Debug.Log("📖 [StoryManager] Misiones actualizadas, verificando fragmentos...");
    }
    
    public void OnMisionDescifrada(Mission mision, StoryFragment fragmento)
    {
        if (fragmento == null) return;
        
        if (debugMode)
            Debug.Log($"📖 [StoryManager] Misión descifrada: {mision.misionID} → Fragmento: {fragmento.fragmentID}");
        
        StartCoroutine(ReproducirConDelay(fragmento, 1f));
    }
    
    public void OnMisionCompletada(Mission mision, StoryFragment fragmento)
    {
        if (fragmento == null) return;
        
        if (debugMode)
            Debug.Log($"📖 [StoryManager] Misión completada: {mision.misionID} → Fragmento: {fragmento.fragmentID}");
        
        StartCoroutine(ReproducirConDelay(fragmento, 1.5f));
    }
    
    private IEnumerator ReproducirConDelay(StoryFragment fragmento, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReproducirFragmento(fragmento);
    }
    
    private void MarcarFragmentoVisto(string fragmentoID)
    {
        if (!fragmentosVistos.Contains(fragmentoID))
        {
            fragmentosVistos.Add(fragmentoID);
            progresoActual.fragmentosVistos.Add(fragmentoID);
            
            if (debugMode)
                Debug.Log($"📖 [StoryManager] ✅ Fragmento marcado como visto: {fragmentoID}");
        }
    }
    
    /// <summary>
    /// Ruta del archivo de progreso, namespaciada por cuento. Cada cuento lleva
    /// su propia narrativa (story_progress_halloween.json, story_progress_christmas.json…)
    /// para que los fragmentos vistos de un cuento no afecten a otro. Si no hay
    /// CuentoActual en la escena, usa el nombre base (compat hacia atrás).
    /// </summary>
    private string RutaProgreso()
    {
        string cuento = CuentoActual.GetCuentoActual();
        string archivo = string.IsNullOrEmpty(cuento)
            ? nombreArchivoProgreso
            : $"{System.IO.Path.GetFileNameWithoutExtension(nombreArchivoProgreso)}_{cuento}.json";
        return System.IO.Path.Combine(Application.persistentDataPath, archivo);
    }

    public void GuardarProgreso()
    {
        // Guard: si este era el duplicado destruido en Awake, progresoActual es null
        if (progresoActual == null) return;

        try
        {
            progresoActual.fragmentosVistos = fragmentosVistos.ToList();
            progresoActual.ultimaActualizacion = System.DateTime.Now.ToString();

            string json = JsonUtility.ToJson(progresoActual, true);
            string ruta = RutaProgreso();
            System.IO.File.WriteAllText(ruta, json);
            
            if (debugMode)
                Debug.Log($"📖 [StoryManager] 💾 Progreso guardado: {fragmentosVistos.Count} fragmentos");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"📖 [StoryManager] Error guardando progreso: {e.Message}");
        }
    }
    
    public void CargarProgreso()
    {
        try
        {
            string ruta = RutaProgreso();

            if (System.IO.File.Exists(ruta))
            {
                string json = System.IO.File.ReadAllText(ruta);
                progresoActual = JsonUtility.FromJson<StoryProgressData>(json);
                
                fragmentosVistos = new HashSet<string>(progresoActual.fragmentosVistos);
                
                if (debugMode)
                    Debug.Log($"📖 [StoryManager] 📂 Progreso cargado: {fragmentosVistos.Count} fragmentos vistos");
            }
            else
            {
                progresoActual = new StoryProgressData();
                
                if (debugMode)
                    Debug.Log("📖 [StoryManager] No hay progreso previo, creando nuevo");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"📖 [StoryManager] Error cargando progreso: {e.Message}");
            progresoActual = new StoryProgressData();
        }
    }
    
    private void PausarGameplay(bool pausar)
    {
        if (debugMode)
            Debug.Log($"📖 [StoryManager] Gameplay {(pausar ? "pausado" : "reanudado")}");
    }
    
    public bool FragmentoYaVisto(string fragmentoID)
    {
        return fragmentosVistos.Contains(fragmentoID);
    }

    /// <summary>
    /// True si en este momento se está reproduciendo un fragmento de narración.
    /// Lo usa el panel de fin de juego para esperar a que termine antes de aparecer.
    /// </summary>
    public bool EstaReproduciendo => reproduciendoFragmento;
    
    public void SaltarFragmentoActual()
    {
        if (!permitirSaltarFragmentos || !reproduciendoFragmento) return;
        
        if (storyUI != null)
        {
            storyUI.SaltarFragmento();
        }
        
        if (reproduccionActual != null)
        {
            StopCoroutine(reproduccionActual);
            reproduciendoFragmento = false;
        }
    }
    
    public void LimpiarColaFragmentos()
    {
        colaFragmentos.Clear();
        
        if (debugMode)
            Debug.Log("📖 [StoryManager] Cola de fragmentos limpiada");
    }
    
    public int ObtenerFragmentosVistos()
    {
        return fragmentosVistos.Count;
    }
    
    public List<string> ObtenerListaFragmentosVistos()
    {
        return fragmentosVistos.ToList();
    }
    
    [ContextMenu("📖 Debug Estado Completo")]
    public void DebugEstadoCompleto()
    {
        Debug.Log("=== 📖 ESTADO STORY MANAGER ===");
        Debug.Log($"Reproduciendo: {reproduciendoFragmento}");
        Debug.Log($"Fragmento actual: {fragmentoActual?.fragmentID ?? "ninguno"}");
        Debug.Log($"Cola: {colaFragmentos.Count} fragmentos");
        Debug.Log($"Fragmentos vistos: {fragmentosVistos.Count}");
        
        if (fragmentosVistos.Count > 0)
        {
            Debug.Log("Fragmentos vistos:");
            foreach (string id in fragmentosVistos)
            {
                Debug.Log($"  - {id}");
            }
        }
    }
    
    [ContextMenu("🔄 Resetear Progreso")]
    public void ResetearProgreso()
    {
        // 1) Limpiar estado EN MEMORIA (para que los fragmentos se repitan en esta misma sesión)
        fragmentosVistos.Clear();
        progresoActual = new StoryProgressData();

        // 2) Escribir vacío en el archivo del cuento actual
        GuardarProgreso();

        // 3) Borrar también el archivo base (sin cuento) por limpieza, si existe y es distinto
        try
        {
            string rutaBase = System.IO.Path.Combine(Application.persistentDataPath, nombreArchivoProgreso);
            if (System.IO.File.Exists(rutaBase) && rutaBase != RutaProgreso())
                System.IO.File.Delete(rutaBase);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"📖 [StoryManager] No se pudo borrar el progreso base: {e.Message}");
        }

        Debug.Log("📖 [StoryManager] Progreso de historia reseteado");
    }
    
    private void CrearUIBasico()
    {
        GameObject uiObj = new GameObject("StoryUI");
        storyUI = uiObj.AddComponent<StoryUIController>();
        uiObj.transform.SetParent(transform);
        
        Debug.Log("📖 [StoryManager] UI básico creado");
    }
    
    void OnDestroy()
    {
        // Liberar el singleton al destruirse con la escena, para que la siguiente
        // escena registre su propio StoryManager sin referencias colgantes.
        if (Instance == this) Instance = null;

        if (missionManager != null)
        {
            missionManager.OnMisionesActualizadas -= HandleMisionesActualizadas;
        }

        GuardarProgreso();
    }
}

[System.Serializable]
public class StoryProgressData
{
    public List<string> fragmentosVistos = new List<string>();
    public string ultimaActualizacion;
}