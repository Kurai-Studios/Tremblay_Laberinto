using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSocketAvanzado : MonoBehaviour, IDropHandler
{
    [Header("Configuración")]
    public string idCorrecto; 

    [Header("Feedback visual")]
    public Image socketImage;
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    private Color colorOriginal;

    [Header("Feedback sonoro")]
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;
    public AudioSource audioSource;

    [Header("Audio Global")]
    [SerializeField] private bool usarAudioGlobal = true;

    private DraggableItem objetoEnSocket;

    void Awake()
    {
        if (socketImage != null)
            colorOriginal = socketImage.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem nuevoObjeto = eventData.pointerDrag?.GetComponent<DraggableItem>();

        if (nuevoObjeto != null && nuevoObjeto.esArrastrable)
        {
            // Si ya había un objeto, devolverlo a la lista
            if (objetoEnSocket != null && objetoEnSocket != nuevoObjeto)
            {
                objetoEnSocket.transform.SetParent(objetoEnSocket.startParent);
                objetoEnSocket.transform.position = objetoEnSocket.startPosition;
                objetoEnSocket = null;
            }

            // Colocar nuevo objeto en este socket
            nuevoObjeto.transform.SetParent(transform);
            nuevoObjeto.transform.position = transform.position;
            objetoEnSocket = nuevoObjeto;

            // Verificar si es correcto
            bool esCorrecto = nuevoObjeto.objetoID == idCorrecto;
            
            Debug.Log($"[DropSocket] Drop en socket {name} - Objeto: {nuevoObjeto.objetoID} - Esperado: {idCorrecto} - Correcto: {esCorrecto}");
            
            // 🎨 Feedback visual
            FeedbackVisual(esCorrecto);
            
            // 🎵 Feedback sonoro - CORREGIDO
            FeedbackSonoro(esCorrecto);
            
            // 📳 Vibración
            Vibrar(esCorrecto);
            
            // Notificar al manager para actualizar UI
            ObjectInfoUIManager.Instance?.ActualizarEstadoMisiones();
        }
    }

    public void VaciarSocket()
    {
        objetoEnSocket = null;
        if (socketImage != null)
            socketImage.color = colorOriginal;
    }

    void FeedbackVisual(bool correcto)
    {
        if (socketImage != null)
            socketImage.color = correcto ? colorCorrecto : colorIncorrecto;
    }

    // 🔧 MÉTODO COMPLETAMENTE CORREGIDO - FeedbackSonoro
    void FeedbackSonoro(bool correcto)
    {
        Debug.Log($"[DropSocket] FeedbackSonoro - Correcto: {correcto}, UsarGlobal: {usarAudioGlobal}");

        // PRIORIDAD 1: GlobalAudioManager
        if (usarAudioGlobal && GlobalAudioManager.Instance != null)
        {
            Debug.Log("[DropSocket] Usando GlobalAudioManager");
            
            if (correcto)
            {
                GlobalAudioManager.Instance.ReproducirSonidoSoltarExitoso();
                Debug.Log("[DropSocket] ✅ Sonido éxito desde GlobalAudioManager");
            }
            else
            {
                GlobalAudioManager.Instance.ReproducirSonidoSoltarFallido();
                Debug.Log("[DropSocket] ❌ Sonido fallo desde GlobalAudioManager");
            }
            
            // TAMBIÉN reproducir sonido específico del socket si existe
            AudioClip clipEspecifico = correcto ? sonidoCorrecto : sonidoIncorrecto;
            if (clipEspecifico != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoSFX(clipEspecifico);
                Debug.Log($"[DropSocket] 🎵 Sonido específico del socket: {clipEspecifico.name}");
            }
            
            return;
        }

        // PRIORIDAD 2: AudioSource local
        Debug.Log("[DropSocket] Usando AudioSource local");
        
        AudioClip clipAReproducir = correcto ? sonidoCorrecto : sonidoIncorrecto;
        
        if (clipAReproducir != null && audioSource != null)
        {
            audioSource.clip = clipAReproducir;
            audioSource.Play();
            Debug.Log($"[DropSocket] ✅ Sonido local reproducido: {clipAReproducir.name}");
        }
        else
        {
            Debug.LogWarning($"[DropSocket] ❌ No se pudo reproducir sonido - Clip: {clipAReproducir?.name ?? "null"}, AudioSource: {audioSource?.name ?? "null"}");
        }
    }

    void Vibrar(bool correcto)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (correcto)
        {
            Handheld.Vibrate();
        }
        else
        {
            Handheld.Vibrate();
        }
#endif
        Debug.Log($"[DropSocket] Vibración - Correcto: {correcto}");
    }

    // 🔧 MÉTODOS DE TESTING
    [ContextMenu("🎵 Test Sonido Correcto")]
    public void TestSonidoCorrecto()
    {
        Debug.Log("[DropSocket] Testing sonido correcto");
        FeedbackSonoro(true);
    }

    [ContextMenu("🎵 Test Sonido Incorrecto")]
    public void TestSonidoIncorrecto()
    {
        Debug.Log("[DropSocket] Testing sonido incorrecto");
        FeedbackSonoro(false);
    }
}