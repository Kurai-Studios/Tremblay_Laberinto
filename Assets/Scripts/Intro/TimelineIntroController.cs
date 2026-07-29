using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

/// <summary>
/// Controla la animación de intro con Timeline (PlayableDirector).
/// Soporta avance y reversa con pausas en puntos definidos (como pasar hojas).
///
/// Montaje:
/// - Asignar el PlayableDirector (Wrap Mode = Hold, Play On Awake desactivado).
/// - Poner Signal Emitters en el Timeline conectados a PausarTimeline().
/// - Copiar los mismos fotogramas de los Signals al array "Frames De Pausa".
/// - Conectar botón adelante a ReanudarTimeline() y botón atrás a RevertirTimeline().
/// </summary>
public class TimelineIntroController : MonoBehaviour
{
    [Header("🎬 Timeline")]
    [SerializeField] private PlayableDirector director;
    [Tooltip("Si está activo, el timeline arranca solo al iniciar la escena.")]
    [SerializeField] private bool reproducirAlIniciar = true;

    [Header("🔘 Botón de continuar (opcional)")]
    [SerializeField] private Button botonContinuar;
    [SerializeField] private bool botonSoloAlPausar = true;

    [Header("⏸️ Puntos de pausa")]
    [Tooltip("Fotogramas donde el timeline debe pausarse. " +
             "Poner los mismos fotogramas donde están los Signal Emitters.")]
    [SerializeField] private int[] framesDePausa;

    [Tooltip("Frame rate del Timeline (por defecto 60). Revisar en el Timeline window.")]
    [SerializeField] private float frameRate = 60f;

    public bool EstaPausado { get; private set; }

    private double tiempoAnterior;
    private float[] puntosDePausa;
    private int indicePausaActual = -1;
    private bool ignorarSignals;

    void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
    }

    void Start()
    {
        if (director == null)
        {
            Debug.LogError("[TimelineIntroController] ❌ No hay PlayableDirector asignado.");
            return;
        }

        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveListener(ReanudarTimeline);
            botonContinuar.onClick.AddListener(ReanudarTimeline);
        }

        if (framesDePausa != null && framesDePausa.Length > 0)
        {
            System.Array.Sort(framesDePausa);
            puntosDePausa = new float[framesDePausa.Length];
            for (int i = 0; i < framesDePausa.Length; i++)
                puntosDePausa[i] = framesDePausa[i] / frameRate;
        }

        if (reproducirAlIniciar)
            IniciarTimeline();
        else
            ActualizarBoton();
    }

    void Update()
    {
        if (director == null || EstaPausado) return;
        if (!director.playableGraph.IsValid()) return;

        double velocidad = director.playableGraph.GetRootPlayable(0).GetSpeed();
        if (velocidad == 0) return;

        // Desactivar el bloqueo de signals después de un frame
        if (ignorarSignals)
        {
            ignorarSignals = false;
            tiempoAnterior = director.time;
            return;
        }

        double tiempoActual = director.time;

        if (velocidad < 0 && puntosDePausa != null)
        {
            for (int i = puntosDePausa.Length - 1; i >= 0; i--)
            {
                if (i == indicePausaActual) continue;

                float punto = puntosDePausa[i];
                if (tiempoAnterior > punto + 0.001f && tiempoActual <= punto + 0.001f)
                {
                    director.time = punto;
                    director.Evaluate();
                    indicePausaActual = i;
                    PausarTimelineInterno();
                    Debug.Log($"[TimelineIntroController] ⏸️ Pausa en reversa en frame {framesDePausa[i]}");
                    break;
                }
            }
        }

        if (velocidad < 0 && tiempoActual <= 0.001)
        {
            director.time = 0;
            director.Evaluate();
            indicePausaActual = -1;
            PausarTimelineInterno();
        }

        tiempoAnterior = tiempoActual;
    }

    public void IniciarTimeline()
    {
        director.time = 0;
        tiempoAnterior = 0;
        EstaPausado = false;
        indicePausaActual = -1;
        ignorarSignals = false;
        director.Play();
        ActualizarBoton();
        Debug.Log("[TimelineIntroController] ▶️ Timeline iniciado");
    }

    /// <summary>
    /// Llamado por los Signal Emitters vía Signal Receiver.
    /// Se ignora si acabamos de reanudar para evitar re-pausar en el mismo punto.
    /// </summary>
    public void PausarTimeline()
    {
        if (ignorarSignals)
        {
            Debug.Log("[TimelineIntroController] 🚫 Signal ignorado (recién reanudado)");
            return;
        }

        PausarTimelineInterno();
    }

    private void PausarTimelineInterno()
    {
        if (director == null || !director.playableGraph.IsValid()) return;
        director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        EstaPausado = true;

        if (puntosDePausa != null)
        {
            float t = (float)director.time;
            for (int i = 0; i < puntosDePausa.Length; i++)
            {
                if (Mathf.Abs(t - puntosDePausa[i]) < 0.02f)
                {
                    indicePausaActual = i;
                    break;
                }
            }
        }

        ActualizarBoton();
        Debug.Log($"[TimelineIntroController] ⏸️ Pausado en t={director.time:0.00}s (indice={indicePausaActual})");
    }

    public void ReanudarTimeline()
    {
        if (director == null || !director.playableGraph.IsValid()) return;
        if (!EstaPausado) return;

        EstaPausado = false;
        ignorarSignals = true;
        tiempoAnterior = director.time;
        director.playableGraph.GetRootPlayable(0).SetSpeed(1);
        ActualizarBoton();
        Debug.Log($"[TimelineIntroController] ▶️ Reanudado desde t={director.time:0.00}s");
    }

    public void RevertirTimeline()
    {
        if (director == null || !director.playableGraph.IsValid()) return;

        EstaPausado = false;
        ignorarSignals = true;
        tiempoAnterior = director.time;
        director.playableGraph.GetRootPlayable(0).SetSpeed(-1);
        ActualizarBoton();
        Debug.Log($"[TimelineIntroController] ⏪ Reversa desde t={director.time:0.00}s");
    }

    private void ActualizarBoton()
    {
        if (botonContinuar == null || !botonSoloAlPausar) return;
        botonContinuar.gameObject.SetActive(EstaPausado);
    }

    void OnDestroy()
    {
        if (botonContinuar != null)
            botonContinuar.onClick.RemoveListener(ReanudarTimeline);
    }
}
