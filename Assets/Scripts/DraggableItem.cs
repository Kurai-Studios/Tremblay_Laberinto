using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public string objetoID;
    [HideInInspector] public bool esArrastrable = true;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public Transform startParent;

    [Header("Efectos de Sonido")]
    [SerializeField] private AudioClip sonidoAgarrar; // Sonido al comenzar drag
    [SerializeField] private AudioClip sonidoSoltar;  // Sonido al terminar drag
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool usarAudioGlobal = true; // Usar AudioSource global si existe

    [Header("Configuración Audio")]
    [SerializeField] private float volumenAgarrar = 0.7f;
    [SerializeField] private float volumenSoltar = 0.6f;
    [SerializeField] private bool reproducirSonidoSoloEnExito = false;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        
        ConfigurarAudio();
    }

    private void ConfigurarAudio()
    {
        // Si GlobalAudioManager está disponible lo usaremos directamente; no necesitamos nada más.
        if (usarAudioGlobal && GlobalAudioManager.Instance != null) return;

        // Fallback: AudioSource local en este GameObject
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!esArrastrable) return;

        startPosition = transform.position;
        startParent = transform.parent;

        DropSocketAvanzado socket = startParent.GetComponent<DropSocketAvanzado>();
        if (socket != null)
            socket.VaciarSocket();

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform);

        // 🎵 REPRODUCIR SONIDO AL AGARRAR
        ReproducirSonidoAgarrar();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!esArrastrable) return;
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!esArrastrable) return;

        canvasGroup.blocksRaycasts = true;
        bool dropExitoso = transform.parent != canvas.transform;

        if (!dropExitoso)
        {
            rectTransform.position = startPosition;
            transform.SetParent(startParent);
            
            // 🎵 SONIDO AL SOLTAR SIN ÉXITO
            if (!reproducirSonidoSoloEnExito)
            {
                ReproducirSonidoSoltar(false);
            }
        }
        else
        {
            if (transform.parent.CompareTag("ListaObjetos"))
            {
                startParent = transform.parent;
                startPosition = transform.position;
            }
            
            // 🎵 SONIDO AL SOLTAR CON ÉXITO - SOLO si no hay socket que lo maneje
            // El DropSocketAvanzado manejará su propio sonido
            if (!HaySocketEnParent())
            {
                ReproducirSonidoSoltar(true);
            }
        }
    }

    private bool HaySocketEnParent()
    {
        return transform.parent != null && 
               transform.parent.GetComponent<DropSocketAvanzado>() != null;
    }

    private void ReproducirSonidoAgarrar()
    {
        if (usarAudioGlobal && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoAgarrarItem();
            return;
        }
        ReproducirSonidoLocal(sonidoAgarrar, volumenAgarrar);
    }

    private void ReproducirSonidoSoltar(bool exitoso)
    {
        if (reproducirSonidoSoloEnExito && !exitoso) return;

        if (usarAudioGlobal && GlobalAudioManager.Instance != null)
        {
            if (exitoso) GlobalAudioManager.Instance.ReproducirSonidoSoltarExitoso();
            else         GlobalAudioManager.Instance.ReproducirSonidoSoltarFallido();
            return;
        }
        ReproducirSonidoLocal(sonidoSoltar, volumenSoltar);
    }

    private void ReproducirSonidoLocal(AudioClip clip, float volumen)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.volume = volumen;
            audioSource.PlayOneShot(clip);
        }
    }

    public void ConfigurarSonidos(AudioClip agarrar, AudioClip soltar)
    {
        sonidoAgarrar = agarrar;
        sonidoSoltar = soltar;
    }

    public void ConfigurarVolumen(float volAgarrar, float volSoltar)
    {
        volumenAgarrar = Mathf.Clamp01(volAgarrar);
        volumenSoltar = Mathf.Clamp01(volSoltar);
    }

    [ContextMenu("🎵 Probar Sonido Agarrar")]
    public void ProbarSonidoAgarrar()
    {
        ReproducirSonidoAgarrar();
    }

    [ContextMenu("🎵 Probar Sonido Soltar Exitoso")]
    public void ProbarSonidoSoltarExitoso()
    {
        ReproducirSonidoSoltar(true);
    }

    [ContextMenu("🎵 Probar Sonido Soltar Fallido")]
    public void ProbarSonidoSoltarFallido()
    {
        ReproducirSonidoSoltar(false);
    }
}
