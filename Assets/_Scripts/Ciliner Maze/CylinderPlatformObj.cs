using UnityEngine;

public class CylinderPlatformObj : MonoBehaviour
{
    [Header("Platform Tags")]
    [Tooltip("Tags usados por el sistema de reglas para identificar el tipo/categoria de esta plataforma")]
    [SerializeField] private string[] tags;

    [Header("Platform Visuals")]
    [SerializeField] GameObject platformMesh;      // El mesh principal de la plataforma
    [SerializeField] GameObject platformCollider;  // El collider de la plataforma

    [Header("Ladder Connection")]
    [Tooltip("Puntos de anclaje L/R donde puede conectar una escalera hacia/desde esta plataforma. El LadderPlacer elige el mas cercano a la otra plataforma a conectar.")]
    [SerializeField] Transform anchorL;
    [SerializeField] Transform anchorR;

    [Header("Debug")]
    [Tooltip("Dibuja los anchors L/R como esferas de color en el Scene View, para verificar visualmente donde estan realmente ubicados")]
    public bool showAnchorGizmos = true;
    [Tooltip("Lanza un rayo desde cada anchor hacia afuera (en su eje X, alejandose del centro de la plataforma) buscando si hay otra plataforma conectada justo al lado (p. ej. una extra encadenada, o la plataforma del nivel siguiente), y lo dibuja en el Scene View: verde si encontro otra plataforma, gris si no encontro nada dentro de anchorConnectionCheckDistance")]
    public bool showAnchorConnectionRays = true;
    [Tooltip("Distancia (unidades de mundo) que recorre el rayo de deteccion de cada anchor")]
    public float anchorConnectionCheckDistance = 2f;

    private TowerPlatform platformData;
    private Vector3 worldPosition;
    private Vector3 basePosition;
    private float radius;
    private float levelHeight;

    // Inicializa la plataforma con los datos generados
    public void Init(TowerPlatform data, float cylinderRadius, float levelHeight, Vector3 basePosition)
    {
        // Guardar referencia a los datos
        platformData = data;
        this.radius = cylinderRadius;
        this.levelHeight = levelHeight;
        this.basePosition = basePosition;

        // Calcular posicion en el mundo
        worldPosition = CalculateWorldPosition(data.angle, data.level);

        // Posicionar la plataforma
        transform.position = worldPosition;

        // Orientar la plataforma hacia afuera del cilindro
        OrientPlatform(data.angle);

        // Activar/desactivar elementos visuales segun el estado
        UpdateVisuals();
    }

    // Calcula la posicion en el mundo de la plataforma
    Vector3 CalculateWorldPosition(float angle, int level)
    {
        return ComputeWorldPosition(angle, level, radius, levelHeight, basePosition);
    }

    // Formula compartida angulo/nivel -> posicion en el mundo. Publica y estatica para que otros
    // sistemas (como PuzzlePathPlacer) la reutilicen sin necesitar una instancia de esta clase.
    // 'level' es float para permitir posiciones intermedias entre niveles (p. ej. los escalones
    // de una rampa de PuzzlePathPlacer), ademas de los niveles enteros normales.
    public static Vector3 ComputeWorldPosition(float angle, float level, float radius, float levelHeight, Vector3 basePosition)
    {
        // Convertir angulo a radianes
        float angleRad = angle * Mathf.Deg2Rad;

        // Calcular posicion en el circulo
        float localX = radius * Mathf.Cos(angleRad);
        float localZ = radius * Mathf.Sin(angleRad);
        float localY = level * levelHeight;

        return new Vector3(
            basePosition.x + localX,
            basePosition.y + localY,
            basePosition.z + localZ);
    }

    // Verifica si hay otra plataforma (con su propio CylinderPlatformObj) fisicamente en medio
    // del tramo recto entre dos puntos, mediante un raycast entre ambos. Se usa antes de colocar
    // una escalera (LadderPlacer) o un tramo de PuzzlePathPlacer, para evitar que atraviese o
    // quede oculto detras de una plataforma que ya ocupa ese camino (p. ej. una extra encadenada
    // del mismo nivel). 'requiredTag' filtra que plataformas cuentan como bloqueo (p. ej. "MainPath"
    // para ignorar objetos que no sean plataformas del camino principal); null/vacio = cualquiera
    // cuenta. 'ignoreA'/'ignoreB' son las plataformas de origen/destino de la conexion y no
    // cuentan como bloqueo aunque el rayo las roce cerca de sus extremos.
    public static bool IsLineBlocked(Vector3 from, Vector3 to, string requiredTag = null, CylinderPlatformObj ignoreA = null, CylinderPlatformObj ignoreB = null)
    {
        return IsLineBlocked(from, to, out _, requiredTag, ignoreA, ignoreB);
    }

    // Misma comprobacion, pero devolviendo ademas el punto exacto del impacto que bloqueo la
    // linea (o Vector3.zero si no hubo bloqueo). Usado por LadderPlacer/PuzzlePathPlacer para
    // dibujar un gizmo de debug y ver donde/si realmente esta detectando la otra plataforma.
    public static bool IsLineBlocked(Vector3 from, Vector3 to, out Vector3 blockingPoint, string requiredTag = null, CylinderPlatformObj ignoreA = null, CylinderPlatformObj ignoreB = null)
    {
        blockingPoint = Vector3.zero;

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.001f) return false;

        RaycastHit[] hits = Physics.RaycastAll(from, delta / distance, distance);
        foreach (RaycastHit hit in hits)
        {
            CylinderPlatformObj hitPlatform = hit.collider.GetComponentInParent<CylinderPlatformObj>();
            if (hitPlatform == null) continue;
            if (hitPlatform == ignoreA || hitPlatform == ignoreB) continue;
            if (!string.IsNullOrEmpty(requiredTag) && !hitPlatform.HasTag(requiredTag)) continue;

            blockingPoint = hit.point;
            return true;
        }

        return false;
    }

    // Orienta la plataforma para que mire hacia afuera del cilindro
    void OrientPlatform(float angle)
    {
        // Hacer que la plataforma mire hacia afuera del cilindro
        Vector3 direction = (transform.position - Vector3.zero).normalized;
        direction.y = 0; // Mantener la plataforma horizontal

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            // Fallback: usar el angulo
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        // Asegurar que la plataforma este horizontal
        Vector3 currentRotation = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, currentRotation.y, 0);
    }

    void UpdateVisuals()
    {
        // Activar mesh principal
        if (platformMesh != null)
            platformMesh.SetActive(true);

        // Activar collider
        if (platformCollider != null)
            platformCollider.SetActive(true);
    }

    // Obtiene la posicion mundial de la plataforma
    public Vector3 GetWorldPosition()
    {
        return worldPosition;
    }

    // Obtiene los datos de la plataforma
    public TowerPlatform GetPlatformData()
    {
        return platformData;
    }

    // Verifica si la plataforma tiene un tag especifico (usado por el sistema de reglas)
    public bool HasTag(string tag)
    {
        if (tags == null || string.IsNullOrEmpty(tag)) return false;

        foreach (string t in tags)
        {
            if (string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // Obtiene todos los tags de la plataforma
    public string[] GetTags()
    {
        return tags;
    }

    // Anchors L/R usados por LadderPlacer para conectar escaleras
    public Transform GetAnchorL()
    {
        return anchorL;
    }

    public Transform GetAnchorR()
    {
        return anchorR;
    }

    // Ancho tangencial (mitad) de la plataforma, medido entre sus anchors L/R en espacio local
    // (sin escala aplicada). Usado por CylinderRender para encadenar plataformas del mismo nivel
    // por sus anchors, sin superponerse, sea cual sea el tamaño del prefab.
    public float GetTangentialHalfWidth()
    {
        if (anchorL == null || anchorR == null) return 0f;
        return Vector3.Distance(anchorL.localPosition, anchorR.localPosition) / 2f;
    }

    // Dibuja los anchors L/R como esferas de color, para poder verificar en el Scene View donde
    // estan realmente ubicados (util al depurar por que un raycast entre anchors no detecta o
    // detecta de mas otra plataforma en LadderPlacer/PuzzlePathPlacer), y opcionalmente el rayo
    // de deteccion de conexion de cada uno (ver CheckAnchorConnection). Cuando un anchor detecta
    // otra plataforma conectada, tanto su esfera como el rayo se pintan de rojo (en vez del color
    // normal cian/naranja) para que sea obvio de un vistazo cual anchor esta conectado.
    void OnDrawGizmos()
    {
        bool leftConnected = false;
        bool rightConnected = false;
        Vector3 leftHitPoint = Vector3.zero;
        Vector3 rightHitPoint = Vector3.zero;

        if (showAnchorGizmos || showAnchorConnectionRays)
        {
            // El proyecto tiene 'Physics.autoSyncTransforms' desactivado (Edit > Project Settings >
            // Physics): si alguna plataforma se reposiciono por script hace poco (p. ej. al generar
            // la torre) y todavia no corrio un paso de fisica, sus colliders quedan desincronizados
            // y el raycast de abajo no detecta nada real. Forzar el sync antes de cada rayo para que
            // este gizmo sea confiable sin depender de cuando corre el proximo FixedUpdate.
            Physics.SyncTransforms();

            // Cada anchor apunta hacia afuera en la MISMA direccion en la que ya esta desplazado
            // del centro de la plataforma (anchorL esta del lado -X local, anchorR del lado +X),
            // asi que el rayo sigue alejandose en ese mismo sentido en vez de volver hacia adentro.
            if (anchorL != null) leftConnected = CheckAnchorConnection(anchorL, -1f, out leftHitPoint);
            if (anchorR != null) rightConnected = CheckAnchorConnection(anchorR, 1f, out rightHitPoint);
        }

        if (showAnchorGizmos)
        {
            if (anchorL != null)
            {
                Gizmos.color = leftConnected ? Color.red : Color.cyan;
                Gizmos.DrawSphere(anchorL.position, 0.15f);
            }

            if (anchorR != null)
            {
                Gizmos.color = rightConnected ? Color.red : new Color(1f, 0.5f, 0f); // naranja si libre
                Gizmos.DrawSphere(anchorR.position, 0.15f);
            }
        }

        if (showAnchorConnectionRays)
        {
            DrawAnchorConnectionRay(anchorL, leftConnected, leftHitPoint);
            DrawAnchorConnectionRay(anchorR, rightConnected, rightHitPoint);
        }
    }

    // Lanza un rayo desde el anchor a lo largo de su eje X (multiplicado por 'outwardSign': -1
    // para el anchor izquierdo, +1 para el derecho) buscando si hay otra plataforma (con su
    // propio CylinderPlatformObj, ignorando esta misma) conectada justo al lado — el uso previsto
    // es detectar el anchor de la plataforma vecina encadenada o la del siguiente nivel. Se
    // recalcula en cada dibujado (no se cachea) para reflejar siempre la posicion actual de las
    // plataformas en el Scene View. 'hitPoint' es el punto de impacto si se encontro algo, o el
    // extremo del rayo (a anchorConnectionCheckDistance) si no.
    bool CheckAnchorConnection(Transform anchor, float outwardSign, out Vector3 hitPoint)
    {
        Vector3 origin = anchor.position;
        Vector3 direction = anchor.right * outwardSign;
        hitPoint = origin + direction * anchorConnectionCheckDistance;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, anchorConnectionCheckDistance);
        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            CylinderPlatformObj hitPlatform = hit.collider.GetComponentInParent<CylinderPlatformObj>();
            if (hitPlatform == null || hitPlatform == this) continue;
            if (hit.distance >= closestDistance) continue;

            closestDistance = hit.distance;
            hitPoint = hit.point;
            found = true;
        }

        return found;
    }

    // Dibuja el rayo de deteccion de un anchor: rojo (linea + esfera en el punto de impacto) si
    // encontro otra plataforma conectada, gris si no encontro nada dentro de anchorConnectionCheckDistance.
    void DrawAnchorConnectionRay(Transform anchor, bool connected, Vector3 hitPoint)
    {
        if (anchor == null) return;

        Gizmos.color = connected ? Color.red : Color.gray;
        Gizmos.DrawLine(anchor.position, hitPoint);

        if (connected)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hitPoint, 0.08f);
        }
    }

    /*void OnDrawGizmosSelected()
    {
        if (platformData != null)
        {
            // Dibujar un gizmo para visualizar la plataforma en el editor
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(worldPosition, 0.5f);

            // Dibujar linea desde el centro del cilindro hasta la plataforma
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Vector3.zero, worldPosition);
        }
    }*/
}
