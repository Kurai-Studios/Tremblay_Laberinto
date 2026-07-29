using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }
    void Awake()
    {
        // Last-wins: ver comentario en InputRouter.Awake(). Antes, si Instance
        // quedaba "viva" (por compartir GameObject con StoryManager, que es
        // DontDestroyOnLoad), esta instancia nueva nunca se registraba y el
        // juego seguía usando el MissionManager viejo de la escena anterior.
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;
    }

    public event Action<string> OnMisionDescifrada;
    public event Action<string> OnMisionCompletada;
    // 🎃 Eventos para la calabaza reactiva
    public event Action OnMisionFallida;                   // al fallar la comprobación de descifrado
    public event Action<Mission> OnNuevaMisionDisponible;  // al desbloquearse una misión nueva
    [Header("Misiones")]
    public List<Mission> misiones = new List<Mission>();
    private int indiceMisionActual = 0;

    [Header("Prefabs")]
    public GameObject textoPrefab;   
    public GameObject socketPrefab;  

    [Header("UI de la misión")]
    public Transform contenedorPartes; 

    [Header("Inventario")]
    [Tooltip("Panel donde viven los DraggableItem en tu ScrollView.")]
    public Transform panelLista;

    [Header("Feedback de resultado")]
    public TextMeshProUGUI resultadoTMP;       
    public bool autoOcultar = true;
    public float duracionMensaje = 2.5f;

    [Header("Sistema de Activación")]
    public GameObjectManager gameObjectManager;
    public TextMeshProUGUI misionesDisponiblesTMP;

    private readonly List<DropSocketAvanzado> socketsEnMision = new List<DropSocketAvanzado>();
    private Coroutine ocultarCR;
    private List<Mission> misionesDisponibles = new List<Mission>();
    private HashSet<string> misionesCompletadas = new HashSet<string>();
    private HashSet<string> misionesDescifradas = new HashSet<string>();

    // 🎃 Para detectar misiones recién desbloqueadas (calabaza reactiva)
    private readonly HashSet<string> _idsDisponiblesPrevios = new HashSet<string>();
    private bool _primeraEvaluacionMisiones = true;

    public event System.Action OnMisionesActualizadas;
    public IReadOnlyCollection<Mission> MisionesDisponibles => misionesDisponibles;
    public IReadOnlyCollection<string> MisionesCompletadas => misionesCompletadas;
    public IReadOnlyCollection<string> MisionesDescifradas => misionesDescifradas;

    [Header("Selección Manual")]
    [SerializeField] private TextMeshProUGUI textoMisionActual; // Para mostrar qué misión está cargada
    private int misionSeleccionadaIndex = -1;

    [Header("Control de Flujo")]
    [SerializeField] private bool avanceAutomaticoActivo = true;
    [SerializeField] private float tiempoParaAvanceAutomatico = 3f;

    private bool misionSeleccionadaManualmente = false;

    void Start()
    {
        if (gameObjectManager == null)
            gameObjectManager = GameObjectManager.Instance;

        if (gameObjectManager != null)
        {
            if (gameObjectManager.datosCargados)
                InicializarSistemaMisiones();
            else
                gameObjectManager.OnDatosCargados += InicializarSistemaMisiones;
        }
        else
        {
            Debug.LogWarning("[MissionManager] GameObjectManager no encontrado");
            CargarPrimeraMisionDisponible();
        }

        if (resultadoTMP != null) resultadoTMP.gameObject.SetActive(false);
    }

public void CargarMisionSeleccionada(int indice)
    {
        if (indice < 0 || indice >= misiones.Count)
        {
            Debug.LogError($"[MissionManager] Índice de misión inválido: {indice}");
            return;
        }

        Mission mision = misiones[indice];
        if (mision == null)
        {
            Debug.LogError($"[MissionManager] Misión nula en índice: {indice}");
            return;
        }

        // Verificar que la misión sea seleccionable
        if (!MisionesDisponibles.Contains(mision))
        {
            Debug.LogWarning($"[MissionManager] Misión no disponible para seleccionar: {mision.descripcion}");
            return;
        }

        if (MisionesDescifradas.Contains(mision.misionID))
        {
            Debug.LogWarning($"[MissionManager] Misión ya descifrada: {mision.descripcion}");
            return;
        }

        Debug.Log($"[MissionManager] Misión seleccionada manualmente: {mision.descripcion}");

        // Marcar como selección manual
        misionSeleccionadaManualmente = true;
        
        indiceMisionActual = indice;
        CargarMision(indice);

        // Actualizar UI
        if (resultadoTMP != null)
        {
            resultadoTMP.text = $"Misión seleccionada: {mision.descripcion}";
            resultadoTMP.gameObject.SetActive(true);
            StartCoroutine(AutoOcultarResultado());
        }
    }

    private void InicializarSistemaMisiones()
    {
        CargarEstadosMisiones();
        EvaluarTodasLasMisiones();
        CargarPrimeraMisionDisponible();
        ActualizarContadorMisiones();
        OnMisionesActualizadas?.Invoke();
        
        Debug.Log($"[MissionManager] Inicializado - Descifradas: {misionesDescifradas.Count}, Completadas: {misionesCompletadas.Count}");
    }

    private void CargarEstadosMisiones()
    {
        if (gameObjectManager != null)
        {
            MissionSaveData datos = gameObjectManager.CargarMisiones();
            
            // Cargar misiones descifradas
            misionesDescifradas = new HashSet<string>(datos.descifradas ?? new List<string>());
            foreach (string misionID in misionesDescifradas)
            {
                Mission mision = misiones.Find(m => m.misionID == misionID);
                if (mision != null) mision.descifrada = true;
            }
            
            // Cargar misiones completadas
            misionesCompletadas = new HashSet<string>(datos.completadas ?? new List<string>());
            foreach (string misionID in misionesCompletadas)
            {
                Mission mision = misiones.Find(m => m.misionID == misionID);
                if (mision != null) mision.completada = true;
            }
        }
    }

    private void GuardarEstadosMisiones()
    {
        if (gameObjectManager != null)
        {
            var datos = new MissionSaveData 
            { 
                descifradas = misionesDescifradas.ToList(),
                completadas = misionesCompletadas.ToList()
            };
            gameObjectManager.GuardarMisiones(datos);
            Debug.Log($"[MissionManager] Estados guardados - D: {misionesDescifradas.Count}, C: {misionesCompletadas.Count}");
        }
    }

    private void EvaluarTodasLasMisiones()
    {
        misionesDisponibles.Clear();

        string cuentoActual = CuentoActual.GetCuentoActual();

        foreach (Mission mision in misiones)
        {
            if (mision == null) continue;

            // Filtrar misiones que no pertenecen al cuento actual.
            // Si cuentoActual es null (sin componente CuentoActual en escena), no filtrar.
            if (!mision.PerteneceACuento(cuentoActual))
                continue;

            bool disponible = EvaluarCondicionesActivacion(mision);

            if (disponible && !misionesCompletadas.Contains(mision.misionID))
            {
                misionesDisponibles.Add(mision);
                Debug.Log($"[MissionManager] Misión disponible: {mision.misionID} - {mision.descripcion}");
            }
        }

        // 🎃 Detectar misiones recién desbloqueadas y avisar a la calabaza reactiva.
        // Se omite la primera evaluación para no reaccionar al arrancar la escena.
        if (!_primeraEvaluacionMisiones)
        {
            foreach (Mission mision in misionesDisponibles)
            {
                if (mision != null && !_idsDisponiblesPrevios.Contains(mision.misionID))
                    OnNuevaMisionDisponible?.Invoke(mision);
            }
        }
        _primeraEvaluacionMisiones = false;

        _idsDisponiblesPrevios.Clear();
        foreach (Mission mision in misionesDisponibles)
            if (mision != null) _idsDisponiblesPrevios.Add(mision.misionID);
    }

    private bool EvaluarCondicionesActivacion(Mission mision)
    {
        switch (mision.tipoActivacion)
        {
            case TipoActivacion.ActivaDesdeInicio:
                return true;

            case TipoActivacion.AlGuardarObjeto:
                return VerificarObjetosGuardados(mision.objetosRequeridos);

            case TipoActivacion.AlDescifrarMision:
                if (string.IsNullOrEmpty(mision.misionRequeridaID))
                    return true;
                
                bool requeridaCompletada = misionesCompletadas.Contains(mision.misionRequeridaID);
                bool requeridaDescifrada = misionesDescifradas.Contains(mision.misionRequeridaID);
                Debug.Log($"[MissionManager] Req: {mision.misionRequeridaID} - C: {requeridaCompletada}, D: {requeridaDescifrada}");
                return requeridaDescifrada || requeridaCompletada;

            default:
                return false;
        }
    }

    private bool VerificarObjetosGuardados(string[] objetosRequeridos)
    {
        if (gameObjectManager == null || objetosRequeridos.Length == 0) 
            return false;

        List<GameObjectData> objetosGuardados = gameObjectManager.ObtenerObjetosGuardados();
        HashSet<string> idsGuardados = new HashSet<string>(objetosGuardados.Select(o => o.id));

        foreach (string objetoID in objetosRequeridos)
            if (!idsGuardados.Contains(objetoID))
                return false;

        return true;
    }

    private void CargarPrimeraMisionDisponible()
    {
        // Si ya hay una misión seleccionada manualmente, no cargar automáticamente
        if (misionSeleccionadaIndex >= 0 && misionSeleccionadaIndex < misiones.Count)
        {
            Mission misionSeleccionada = misiones[misionSeleccionadaIndex];
            if (misionSeleccionada != null && 
                MisionesDisponibles.Contains(misionSeleccionada) && 
                !MisionesDescifradas.Contains(misionSeleccionada.misionID))
            {
                Debug.Log($"[MissionManager] Manteniendo misión seleccionada: {misionSeleccionada.descripcion}");
                return;
            }
        }

        // Verificar si todas las misiones están descifradas
        bool todasDescifradas = misiones.All(m => MisionesDescifradas.Contains(m.misionID));
        if (todasDescifradas)
        {
            if (resultadoTMP != null)
            {
                resultadoTMP.text = "🏆 ¡Todas las misiones descifradas! Ve al AR para completarlas.";
                resultadoTMP.gameObject.SetActive(true);
            }
            return;
        }

        if (MisionesDisponibles.Count > 0)
        {
            // Buscar la primera misión disponible que NO esté descifrada
            Mission primeraMision = MisionesDisponibles
                .FirstOrDefault(m => !MisionesDescifradas.Contains(m.misionID));
            
            if (primeraMision != null)
            {
                int indiceEnListaOriginal = misiones.IndexOf(primeraMision);
                
                if (indiceEnListaOriginal >= 0)
                {
                    indiceMisionActual = indiceEnListaOriginal;
                    misionSeleccionadaIndex = -1; // Reset selección manual
                    CargarMision(indiceMisionActual);
                    
                    if (textoMisionActual != null)
                    {
                        textoMisionActual.text = $"Misión Actual: {primeraMision.descripcion}";
                    }
                }
            }
            else
            {
                if (resultadoTMP != null)
                {
                    resultadoTMP.text = "🏆 ¡Todas las misiones descifradas! Ve al AR para completarlas.";
                    resultadoTMP.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            Debug.Log("[MissionManager] No hay misiones disponibles");
            if (resultadoTMP != null)
            {
                resultadoTMP.text = "No hay misiones disponibles. Recolecta más objetos.";
                resultadoTMP.gameObject.SetActive(true);
            }
        }
    }

    public string ObtenerEstadoGeneral()
    {
        int totalMisiones = misiones.Count;
        int descifradas = misionesDescifradas.Count;
        int completadas = misionesCompletadas.Count;
        
        if (completadas == totalMisiones)
            return "🎉 ¡TODAS LAS MISIONES COMPLETADAS!";
        
        if (descifradas == totalMisiones)
            return "🏆 Todas descifradas - Ve al AR a completarlas";
        
        return $"Progreso: {descifradas}/{totalMisiones} descifradas, {completadas}/{totalMisiones} completadas";
    }

   public void CargarMision(int indice)
{
    RegresarDraggablesAlPanel();

    if (contenedorPartes != null)
    {
        for (int i = contenedorPartes.childCount - 1; i >= 0; i--)
            Destroy(contenedorPartes.GetChild(i).gameObject);
    }
    socketsEnMision.Clear();

    if (indice < 0 || indice >= misiones.Count) return;
    Mission mision = misiones[indice];
    if (mision == null || contenedorPartes == null) return;

    if (!misionesDisponibles.Contains(mision))
    {
        Debug.LogWarning($"[MissionManager] Misión no disponible: {mision.descripcion}");
        return;
    }

    // 🆕 USAR COROUTINE para esperar el rebuild del layout
    StartCoroutine(CargarMisionConLayout(mision));
}

private IEnumerator CargarMisionConLayout(Mission mision)
{
    if (mision.partes == null || mision.partes.Length == 0)
    {
        Debug.LogWarning($"[MissionManager] Misión sin partes: {mision.descripcion}");
        yield break;
    }

    Debug.Log($"[MissionManager] 📋 Cargando misión: {mision.descripcion}");

    // PASO 1: Crear todas las partes
    foreach (var parte in mision.partes)
    {
        if (parte == null) continue;

        if (parte.EsSocket)
        {
            // 🔵 SOCKET
            GameObject go = Instantiate(socketPrefab, contenedorPartes);
            var drop = go.GetComponent<DropSocketAvanzado>();
            if (drop != null)
            {
                drop.idCorrecto = parte.idCorrecto;
                socketsEnMision.Add(drop);
            }

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = parte.texto;
        }
        else
        {
            // 🟢 TEXTO
            GameObject go = Instantiate(textoPrefab, contenedorPartes);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = parte.texto;
        }
    }
     // PASO 2: Esperar a que Unity procese los cambios
    yield return null; // Espera 1 frame

    // PASO 3: Forzar actualización del Canvas
    Canvas.ForceUpdateCanvases();

    // PASO 4: Forzar rebuild de layouts (TextMeshPro + ContentSizeFitter)
    RectTransform rectTransform = contenedorPartes.GetComponent<RectTransform>();
    if (rectTransform != null)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    // PASO 5: Esperar al final del frame para asegurar que todo se renderizó
    yield return new WaitForEndOfFrame();

    // PASO 6: Rebuild final (por si acaso)
    Canvas.ForceUpdateCanvases();

    Debug.Log($"[MissionManager] ✅ Misión cargada y layout actualizado");
}

public void ComprobarMision()
{
    int total = socketsEnMision.Count;

    // IDs que la misión espera en CUALQUIERA de sus sockets (para detectar "mal colocado":
    // un objeto que sí pertenece a esta misión, pero está en el socket equivocado).
    var idsEsperados = new HashSet<string>();
    foreach (var s in socketsEnMision)
        if (s != null && !string.IsNullOrEmpty(s.idCorrecto))
            idsEsperados.Add(s.idCorrecto);

    int correctos = 0, malColocados = 0, incorrectos = 0, vacios = 0;

    foreach (var socket in socketsEnMision)
    {
        if (socket == null) continue;

        DraggableItem colocado = null;
        for (int i = 0; i < socket.transform.childCount; i++)
        {
            var drag = socket.transform.GetChild(i).GetComponent<DraggableItem>();
            if (drag != null) { colocado = drag; break; }
        }

        if (colocado == null)
            vacios++;
        else if (colocado.objetoID == socket.idCorrecto)
            correctos++;
        else if (idsEsperados.Contains(colocado.objetoID))
            malColocados++;   // pertenece a la misión, pero en el socket equivocado
        else
            incorrectos++;    // no pertenece a esta misión en absoluto
    }

    MostrarResultado(correctos, malColocados, incorrectos, vacios, total, correctos == total);

    if (correctos == total && total > 0)
    {
        // 🎉 SONIDO DE MISIÓN DESCIFRADA CON ÉXITO
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoMisionDescifradaExito();
        }

        Mission misionActual = misiones[indiceMisionActual];
        if (misionActual != null && !string.IsNullOrEmpty(misionActual.misionID))
        {
            DescifrarMision(misionActual.misionID);
            
            if (resultadoTMP != null)
            {
                resultadoTMP.text = $"¡Misión descifrada! Ve al AR para completarla.";
                resultadoTMP.gameObject.SetActive(true);
            }
        }

        StartCoroutine(AvanzarSiguienteMisionTras(2f));
    }
    else
    {
        // ❌ SONIDO DE ERROR AL DESCIFRAR MISIÓN
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoMisionDescifradaError();
        }

        // 🎃 Notificar a la calabaza reactiva (comprobación fallida)
        OnMisionFallida?.Invoke();
    }
}

     public void DescifrarMision(string misionID)
{
    if (string.IsNullOrEmpty(misionID)) return;

    Mission mision = misiones.Find(m => m.misionID == misionID);
    if (mision != null && !misionesDescifradas.Contains(misionID))
    {
        mision.descifrada = true;
        misionesDescifradas.Add(misionID);
        OnMisionDescifrada?.Invoke(misionID);

        GuardarEstadosMisiones();
        Debug.Log($"[MissionManager] Misión DESCIFRADA: {misionID}");

        // 📖 NUEVO: Reproducir fragmento al descifrar
        if (mision.fragmentoAlDescifrar != null && StoryManager.Instance != null)
        {
            Debug.Log($"[MissionManager] 📖 Reproduciendo fragmento: {mision.fragmentoAlDescifrar.fragmentID}");
            StoryManager.Instance.OnMisionDescifrada(mision, mision.fragmentoAlDescifrar);
        }

        // Reevaluar para desbloquear misiones dependientes
        EvaluarTodasLasMisiones();
        OnMisionesActualizadas?.Invoke();

        // Decidir si avanzar automáticamente o no
        if (avanceAutomaticoActivo && !misionSeleccionadaManualmente)
        {
            StartCoroutine(AvanzarSiguienteMisionTras(tiempoParaAvanceAutomatico));
        }
        else
        {
            // Si fue selección manual, mostrar mensaje diferente
            StartCoroutine(MostrarMensajeSeleccionManual());
        }
    }
}

    private IEnumerator MostrarMensajeSeleccionManual()
    {
        yield return new WaitForSeconds(2f);

        if (resultadoTMP != null)
        {
            resultadoTMP.text = "Misión descifrada! Puedes seleccionar otra o continuar automáticamente.";
            resultadoTMP.gameObject.SetActive(true);
        }

        // Reset flag después del mensaje
        misionSeleccionadaManualmente = false;
    }

    public List<Mission> ObtenerMisionesPorObjetoDestino(string objetoID, bool soloDescifradas = true)
    {
        if (soloDescifradas)
        {
            return misiones.Where(m => 
                m.idObjetoDestino == objetoID &&
                misionesDescifradas.Contains(m.misionID) &&
                !misionesCompletadas.Contains(m.misionID)
            ).ToList();
        }
        else
        {
            return misiones.Where(m => 
                m.idObjetoDestino == objetoID &&
                !misionesCompletadas.Contains(m.misionID)
            ).ToList();
        }
    }

    public bool TieneMisionesDescifradasPendientes(string objetoID)
    {
        return misiones.Any(m => 
            m.idObjetoDestino == objetoID &&
            misionesDescifradas.Contains(m.misionID) &&
            !misionesCompletadas.Contains(m.misionID)
        );
    }

    public void CompletarMisionAR(string misionID, bool reproducirFragmento = true)
    {
        if (string.IsNullOrEmpty(misionID)) return;

        Mission mision = misiones.Find(m => m.misionID == misionID);
        if (mision != null && !misionesCompletadas.Contains(misionID))
        {
            // Asegurar que está descifrada primero
            if (!misionesDescifradas.Contains(misionID))
            {
                mision.descifrada = true;
                misionesDescifradas.Add(misionID);
            }

            mision.completada = true;
            OnMisionCompletada?.Invoke(misionID);
            if (reproducirFragmento)
            {
                ReproducirFragmentoCompletado(misionID);
            }
            misionesCompletadas.Add(misionID);

            GuardarEstadosMisiones();
            Debug.Log($"[MissionManager] Misión COMPLETADA en AR: {misionID}");

            EvaluarTodasLasMisiones();
            OnMisionesActualizadas?.Invoke();
        }
    }

    /// <summary>
    /// Reproduce el fragmento de historia asociado a "completar" la misión, si existe.
    /// Se llama automáticamente desde CompletarMisionAR, o de forma diferida (por ejemplo,
    /// después de que termine un Timeline de animación en AR).
    /// </summary>
    public void ReproducirFragmentoCompletado(string misionID)
    {
        Mission mision = misiones.Find(m => m.misionID == misionID);
        if (mision == null) return;

        if (mision.fragmentoAlCompletar != null && StoryManager.Instance != null)
        {
            Debug.Log($"[MissionManager] 📖 Reproduciendo fragmento completado");
            StoryManager.Instance.OnMisionCompletada(mision, mision.fragmentoAlCompletar);
        }
    }

    private void MostrarResultado(int correctos, int malColocados, int incorrectos, int vacios, int total, bool completo)
    {
        if (resultadoTMP == null) return;

        resultadoTMP.text = ConstruirMensajeResultado(correctos, malColocados, incorrectos, vacios, total);
        resultadoTMP.gameObject.SetActive(true);

        if (ocultarCR != null) StopCoroutine(ocultarCR);
        if (autoOcultar && !completo)
            ocultarCR = StartCoroutine(AutoOcultarResultado());
    }

    /// <summary>
    /// Arma un párrafo de feedback explicando POR QUÉ falló la comprobación:
    /// si no se colocó nada, si faltan objetos, si hay objetos correctos pero en el
    /// socket equivocado (mal colocados), o si hay objetos que no pertenecen a la misión.
    /// </summary>
    private string ConstruirMensajeResultado(int correctos, int malColocados, int incorrectos, int vacios, int total)
    {
        if (correctos == total && total > 0)
            return $"Complete! ({total}/{total})";

        var partes = new List<string>
        {
            $"This mission needs {total} object{(total == 1 ? "" : "s")}."
        };

        int colocados = total - vacios;
        if (colocados == 0)
        {
            partes.Add("You haven't placed any objects yet.");
            return string.Join(" ", partes);
        }

        if (vacios > 0)
            partes.Add($"You still need to place {vacios} more object{(vacios == 1 ? "" : "s")}.");

        if (malColocados > 0)
            partes.Add($"{malColocados} object{(malColocados == 1 ? "" : "s")} {(malColocados == 1 ? "is" : "are")} in the wrong spot.");

        if (incorrectos > 0)
            partes.Add($"{incorrectos} object{(incorrectos == 1 ? "" : "s")} {(incorrectos == 1 ? "doesn't" : "don't")} belong to this mission.");

        partes.Add($"Correct: {correctos} of {total}.");

        return string.Join(" ", partes);
    }

    private IEnumerator AutoOcultarResultado()
    {
        yield return new WaitForSeconds(duracionMensaje);
        if (resultadoTMP != null) resultadoTMP.gameObject.SetActive(false);
    }

    private IEnumerator AvanzarSiguienteMisionTras(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Verificar si TODAS las misiones están descifradas
        bool todasDescifradas = misiones.All(m => misionesDescifradas.Contains(m.misionID));
        if (todasDescifradas)
        {
            if (resultadoTMP != null)
            {
                resultadoTMP.text = "¡Todas las misiones descifradas! Ve al AR para completarlas.";
                resultadoTMP.gameObject.SetActive(true);
            }
            yield break;
        }

        EvaluarTodasLasMisiones();
        ActualizarContadorMisiones();

        Mission siguienteMision = null;
        int siguienteIndice = -1;

        // Buscar siguiente misión disponible NO DESCIFRADA
        for (int i = indiceMisionActual + 1; i < misiones.Count; i++)
        {
            if (MisionesDisponibles.Contains(misiones[i]) && 
                !misionesDescifradas.Contains(misiones[i].misionID) &&
                !misionesCompletadas.Contains(misiones[i].misionID))
            {
                siguienteMision = misiones[i];
                siguienteIndice = i;
                break;
            }
        }

        // Si no hay siguiente, buscar desde el principio
        if (siguienteMision == null)
        {
            for (int i = 0; i < misiones.Count; i++)
            {
                if (MisionesDisponibles.Contains(misiones[i]) && 
                    !misionesDescifradas.Contains(misiones[i].misionID) &&
                    !misionesCompletadas.Contains(misiones[i].misionID))
                {
                    siguienteMision = misiones[i];
                    siguienteIndice = i;
                    break;
                }
            }
        }

        if (siguienteMision != null)
        {
            indiceMisionActual = siguienteIndice;
            misionSeleccionadaManualmente = false; // Reset para la siguiente
            CargarMision(indiceMisionActual);

            if (resultadoTMP != null)
            {
                resultadoTMP.text = $"Siguiente misión: {siguienteMision.descripcion}";
                resultadoTMP.gameObject.SetActive(true);
            }
        }
        else
        {
            if (resultadoTMP != null)
            {
                resultadoTMP.text = "¡Todas las misiones descifradas! Ve al AR para completarlas.";
                resultadoTMP.gameObject.SetActive(true);
            }
        }

        OnMisionesActualizadas?.Invoke();
    }

    public void ConfigurarAvanceAutomatico(bool activar)
    {
        avanceAutomaticoActivo = activar;
        Debug.Log($"[MissionManager] Avance automático: {(activar ? "ACTIVADO" : "DESACTIVADO")}");
    }

    // NUEVO: Método para forzar avance a siguiente misión
    public void AvanzarASiguienteMision()
    {
        if (misionesDescifradas.Count == 0)
        {
            Debug.LogWarning("[MissionManager] No hay misiones descifradas para avanzar");
            return;
        }

        misionSeleccionadaManualmente = false;
        StartCoroutine(AvanzarSiguienteMisionTras(0.1f));
    }

    // NUEVO: Obtener información de estado
    public string ObtenerEstadoSistema()
    {
        int totalMisiones = misiones.Count;
        int descifradas = misionesDescifradas.Count;
        int disponibles = MisionesDisponibles.Count;
        
        string estadoAvance = avanceAutomaticoActivo ? "Automático" : "Manual";
        string seleccion = misionSeleccionadaManualmente ? "(Selección Manual)" : "(Avance Automático)";
        
        return $"Misiones: {descifradas}/{totalMisiones} descifradas | {disponibles} disponibles | Modo: {estadoAvance} {seleccion}";
    }

public Mission ObtenerMisionActual()
    {
        if (indiceMisionActual >= 0 && indiceMisionActual < misiones.Count)
        {
            return misiones[indiceMisionActual];
        }
        return null;
    }

    // NUEVO: Método para verificar si hay misión cargada
    public bool TieneMisionCargada()
    {
        return indiceMisionActual >= 0 && indiceMisionActual < misiones.Count;
    }

    /// <summary>
    /// True si TODAS las misiones del cuento actual están completadas (fin del juego).
    /// Filtra por cuento igual que EvaluarTodasLasMisiones, para no exigir misiones de otros cuentos.
    /// </summary>
    public bool TodasMisionesCompletadas()
    {
        if (misiones == null) return false;

        string cuentoActual = CuentoActual.GetCuentoActual();
        bool hayAlguna = false;

        foreach (var m in misiones)
        {
            if (m == null) continue;
            if (!m.PerteneceACuento(cuentoActual)) continue; // solo las de este cuento
            hayAlguna = true;
            if (!misionesCompletadas.Contains(m.misionID)) return false;
        }

        return hayAlguna;
    }

    private void RegresarDraggablesAlPanel()
    {
        if (panelLista == null) return;

        List<DropSocketAvanzado> socketsOrigen = new List<DropSocketAvanzado>();
        if (socketsEnMision.Count > 0) socketsOrigen.AddRange(socketsEnMision);
        else if (contenedorPartes != null)
            socketsOrigen.AddRange(contenedorPartes.GetComponentsInChildren<DropSocketAvanzado>(true));

        foreach (var socket in socketsOrigen)
        {
            if (socket == null) continue;

            for (int i = socket.transform.childCount - 1; i >= 0; i--)
            {
                Transform hijo = socket.transform.GetChild(i);
                var drag = hijo.GetComponent<DraggableItem>();
                if (drag == null) continue;

                hijo.SetParent(panelLista, false);
                Canvas.ForceUpdateCanvases();
                drag.startParent = panelLista;
                drag.startPosition = drag.transform.position;
                hijo.SetAsLastSibling();
            }
            socket.VaciarSocket();
        }
    }

    private void ActualizarContadorMisiones()
    {
        if (misionesDisponiblesTMP != null)
        {
            int completadas = misionesCompletadas.Count;
            int descifradas = misionesDescifradas.Count;
            int disponibles = misionesDisponibles.Count;
            int total = misiones.Count;
            
            misionesDisponiblesTMP.text = $"Misiones: {completadas}/{total} completadas | {descifradas} descifradas | {disponibles} disponibles";
        }
    }

    /// <summary>
    /// Reinicia TODO el progreso de misiones EN MEMORIA (no solo el JSON). Lo usa el reset:
    /// limpia los HashSet de descifradas/completadas y los bools de cada Mission, resetea el estado
    /// de selección y vuelve a evaluar desde cero. Sin esto, tras un reset el estado en memoria
    /// seguía "completo" y el panel de fin de juego se disparaba en bucle.
    /// </summary>
    public void ReiniciarProgresoMisiones()
    {
        Debug.Log("[MissionManager] Reiniciando progreso de misiones (memoria)...");

        misionesCompletadas.Clear();
        misionesDescifradas.Clear();

        foreach (var m in misiones)
        {
            if (m == null) continue;
            m.descifrada = false;
            m.completada = false;
        }

        misionSeleccionadaIndex = -1;
        indiceMisionActual = 0;
        misionSeleccionadaManualmente = false;

        // Que la 1ª reevaluación tras el reset NO dispare "nueva misión disponible" (calabaza).
        _idsDisponiblesPrevios.Clear();
        _primeraEvaluacionMisiones = true;

        GuardarEstadosMisiones();          // persistir el estado limpio
        EvaluarTodasLasMisiones();
        CargarPrimeraMisionDisponible();
        ActualizarContadorMisiones();
        OnMisionesActualizadas?.Invoke();  // el panel de fin de juego reevalúa → se oculta
    }

    public void ReevaluarMisiones()
    {
        Debug.Log("[MissionManager] Reevaluando misiones...");
        EvaluarTodasLasMisiones();
        ActualizarContadorMisiones();
        OnMisionesActualizadas?.Invoke();

        if (indiceMisionActual < 0 || indiceMisionActual >= misiones.Count || 
            !misionesDisponibles.Contains(misiones[indiceMisionActual]))
        {
            CargarPrimeraMisionDisponible();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (gameObjectManager != null)
            gameObjectManager.OnDatosCargados -= InicializarSistemaMisiones;
    }
}