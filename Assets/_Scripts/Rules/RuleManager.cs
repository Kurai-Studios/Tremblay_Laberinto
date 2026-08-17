using System.Collections.Generic;
using UnityEngine;

// Datos de una plataforma del camino: en que nivel, a que angulo y con que tag debe colocarse.
// Las plataformas principales traen su angulo final ya calculado (random walk). Las extras
// encadenadas (isChained = true) no traen angulo: CylinderRender lo calcula en el momento de
// instanciar, usando los anchors L/R de la plataforma anterior y la nueva para que queden
// pegadas por sus bordes sin superponerse (ver CylinderRender.GenerateTower).
[System.Serializable]
public struct PathPlatform
{
    public int level;
    public float angle;
    public string tag;
    public bool isChained;
    public float chainDirection;

    // Plataforma principal: angulo final ya definido, no encadenada
    public PathPlatform(int level, float angle, string tag)
    {
        this.level = level;
        this.angle = angle;
        this.tag = tag;
        this.isChained = false;
        this.chainDirection = 1f;
    }

    // Plataforma extra encadenada: sin angulo propio, CylinderRender lo calcula via anchors
    public PathPlatform(int level, string tag, float chainDirection)
    {
        this.level = level;
        this.angle = 0f;
        this.tag = tag;
        this.isChained = true;
        this.chainDirection = chainDirection;
    }
}

public class RuleManager : MonoBehaviour
{
    [Header("Path - Random Walk")]
    [Tooltip("Angulo minimo (en grados) que avanza el camino entre un nivel y el siguiente. Ya no es un tope absoluto: define cuanto puede variar el paso alrededor del angulo que garantiza escalera (ver GeneratePath); la geometria de la escalera manda si es mas estricta")]
    public float minStepAngle = 20f;
    [Tooltip("Angulo maximo (en grados) que avanza el camino entre un nivel y el siguiente. Ya no es un tope absoluto: define cuanto puede variar el paso alrededor del angulo que garantiza escalera (ver GeneratePath); la geometria de la escalera manda si es mas estricta")]
    public float maxStepAngle = 100f;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de cambiar de direccion (horario/antihorario) en cada nivel")]
    public float directionChangeChance = 0.3f;

    [Header("Path - Plataformas Extra")]
    [Tooltip("Cantidad minima de plataformas extra (ademas de la principal) en cada nivel intermedio del camino. Solo se cumple en niveles donde el camino tiene un lado de anchor realmente libre de escaleras (ver GeneratePath); si ambos lados estan ocupados por escaleras, ese nivel no recibe extras aunque el minimo sea mayor a 0")]
    public int minExtraPlatformsPerLevel = 0;
    [Tooltip("Cantidad maxima de plataformas extra (ademas de la principal) en cada nivel intermedio del camino")]
    public int maxExtraPlatformsPerLevel = 1;
    // La separacion angular entre plataformas extra ya no es un valor fijo: CylinderRender la
    // calcula usando los anchors L/R de cada prefab para que queden pegadas por sus bordes sin
    // superponerse, sea cual sea su tamaño o el radio del cilindro (ver GenerateTower).

    [Header("Path - Trampas")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que una plataforma extra sea una Trampa en vez de Normal. Las trampas solo pueden aparecer en niveles intermedios cuyo nivel anterior Y siguiente del camino principal sean Normal (nunca pegadas a Spawn o Final)")]
    public float trapChance = 0.25f;

    // Genera un camino continuo desde el nivel base hasta el nivel mas alto mediante un
    // random walk: en cada nivel el angulo avanza un paso aleatorio (con direccion que
    // puede cambiar de forma aleatoria), en vez de seguir una espiral uniforme fija.
    // Esto hace que el camino resultante sea distinto en cada partida y no siempre
    // tenga forma de espiral perfecta.
    // Solo usa los tags Spawn (nivel base), Normal (niveles intermedios) y Final (nivel mas alto).
    //
    // 'radius', 'levelHeight' y 'maxLadderTiltAngle' definen la geometria de la escalera.
    // 'connectorHalfWidthAngle' es el angulo (grados) que ocupa, desde el centro de una
    // plataforma principal hasta su anchor L/R, a ese radio (ver CylinderRender.HalfWidthToAngle).
    //
    // LadderPlacer no conecta los centros de dos plataformas principales: conecta el anchor de
    // cada una mas cercano a la otra. Por eso el paso angular que realmente deja la escalera
    // vertical no es "cualquier paso chico", sino uno cercano a 2 * connectorHalfWidthAngle (el
    // paso que hace coincidir el anchor de "salida" de la plataforma de abajo con el anchor de
    // "entrada" de la de arriba). GeneratePath apunta ese angulo directamente, con un margen de
    // variacion (jitter) acotado por la tolerancia de inclinacion de la escalera, en vez de
    // recortar un rango arbitrario de pasos que casi nunca caia en la ventana valida.
    //
    // Esto garantiza escalera en CADA nivel solo mientras el camino sigue girando en la misma
    // direccion. Si se apuntara a ese mismo angulo tambien al cambiar de direccion, la escalera
    // de salida de ese nivel quedaria pegada al mismo anchor que la escalera de entrada (una
    // escalera literalmente arriba de otra: el jugador sube y sigue subiendo sin caminar nada,
    // rompiendo el maze). Por eso un cambio de direccion se trata como un "salto libre" (paso
    // ancho, no dirigido a ningun anchor en particular): ese nivel queda intencionalmente sin
    // escalera de salida, hueco que el sistema de puzzle path debera cubrir mas adelante.
    public List<PathPlatform> GeneratePath(int totalLevels, float radius, float levelHeight, float maxLadderTiltAngle, float connectorHalfWidthAngle)
    {
        List<PathPlatform> path = new List<PathPlatform>();

        if (totalLevels <= 0) return path;

        // Angulo de paso que alinea el anchor de salida de una plataforma con el anchor de
        // entrada de la siguiente (escalera practicamente vertical), y cuanto se puede desviar
        // de ese angulo sin superar la inclinacion maxima permitida.
        float ladderStepCenter = connectorHalfWidthAngle * 2f;
        float ladderTolerance = MaxStepAngleForTilt(radius, levelHeight, maxLadderTiltAngle);

        // minStepAngle/maxStepAngle ya no acotan el paso de forma absoluta: controlan cuanto
        // "tambalea" el paso alrededor de ladderStepCenter (variedad visual del espiral), pero
        // la tolerancia geometrica de la escalera manda si es mas estricta que lo que pide el usuario.
        // Margen de seguridad: la formula de arriba (igual que HalfWidthToAngle) asume que el
        // anchor conector queda exactamente al radio del cilindro, pero en realidad queda un
        // poco mas lejos del eje (offset tangencial => hipotenusa levemente mayor a 'radius').
        // Ese desvio es chico, pero sin margen algunos pasos quedaban justo 0.0x grados por
        // encima de maxLadderTiltAngle y la escalera se descartaba por "TooDiagonal". Se recorta
        // la tolerancia un 15% para asegurar margen real, no solo el limite teorico.
        const float ladderToleranceSafetyFactor = 0.85f;
        float userSpread = Mathf.Max(0f, (maxStepAngle - minStepAngle) / 2f);
        float effectiveJitter = Mathf.Min(userSpread, ladderTolerance * ladderToleranceSafetyFactor);

        float currentAngle = Random.Range(0f, 360f);
        float direction = Random.value < 0.5f ? 1f : -1f;

        // Lado (±1, mismo signo que 'direction') del anchor de la plataforma principal del nivel
        // actual que ya esta ocupado por una escalera real que viene del nivel anterior. Null si
        // no hay nivel anterior, o si el paso que llego a este nivel fue un "salto libre" (ver
        // abajo) que no garantiza escalera y por lo tanto no reclama ningun lado.
        float? incomingSide = null;

        for (int level = 0; level < totalLevels; level++)
        {
            string tag = GetTagForLevel(level, totalLevels);

            path.Add(new PathPlatform(level, currentAngle, tag));

            bool isEndpoint = level == 0 || level == totalLevels - 1;
            bool hasNextLevel = level < totalLevels - 1;

            // Se decide el paso (y por lo tanto que lado de anchor va a usar la escalera hacia
            // el nivel siguiente) antes de generar las plataformas extra de este nivel, para
            // saber que lado queda realmente libre.
            //
            // Cuando el camino cambia de direccion, apuntar igual al angulo que alinea anchors
            // haria que la escalera de salida de este nivel caiga exactamente en el mismo punto
            // que la escalera de entrada (misma "conexion cerrada"), es decir, una escalera
            // literalmente arriba de otra: el jugador sube y puede seguir subiendo sin caminar
            // nada, rompiendo el maze. Por eso un cambio de direccion se trata como un "salto
            // libre": un paso amplio y no dirigido (rango minStepAngle/maxStepAngle original) que
            // intencionalmente no intenta alinear anchors. LadderPlacer simplemente no encontrara
            // escalera ahi (se descarta por angulo), dejando un hueco que el puzzle path debera
            // cubrir mas adelante.
            float? outgoingSide = null;
            float step = 0f;
            if (hasNextLevel)
            {
                bool isDirectionChange = Random.value < directionChangeChance;

                // Regla: el nivel Final tiene que llegar SI O SI por escalera (un unico tramo
                // garantizado, el que conecta totalLevels-2 con totalLevels-1), para que el
                // jugador siempre pueda completar el camino sin depender de que el puzzle path
                // haya cubierto ese tramo. Lo mismo aplica al nivel de Spawn (nivel 0): necesita
                // un punto garantizado de salida por escalera hacia el nivel 1, para que el
                // jugador nunca arranque encerrado. En ambos casos se fuerza el paso "alineado"
                // (nunca un salto libre de cambio de direccion), para que LadderPlacer siempre
                // encuentre un par de anchors verticalmente alineado en ese tramo (ver comentario
                // de mas abajo sobre por que el paso alineado garantiza esto).
                bool mustGuaranteeLadder = level == totalLevels - 2 || level == 0;
                if (mustGuaranteeLadder)
                    isDirectionChange = false;

                if (isDirectionChange)
                {
                    direction *= -1f;
                    step = FreeStepMagnitude(ladderStepCenter, ladderTolerance) * direction;
                    // outgoingSide queda null: este paso no garantiza escalera, no reclama lado.
                }
                else
                {
                    outgoingSide = direction;
                    step = (ladderStepCenter + Random.Range(-effectiveJitter, effectiveJitter)) * direction;
                }
            }

            // Plataformas extra en niveles intermedios (Normal o, con cierta probabilidad,
            // Trampa), encadenadas en un lado que no necesita ninguna escalera de este nivel.
            if (!isEndpoint)
            {
                // El anchor "de entrada" (escalera desde el nivel anterior) y el "de salida"
                // (escalera hacia el nivel siguiente) solo reclaman un lado cuando esa conexion
                // es una escalera real (no un salto libre). Si ambas reclaman lados, siempre son
                // opuestos (nunca el mismo, ver comentario arriba), asi que no queda lado libre.
                bool bothSidesUsed = incomingSide.HasValue && outgoingSide.HasValue;

                if (!bothSidesUsed)
                {
                    // Al menos un lado esta libre: si una de las dos conexiones es real, usar el
                    // lado opuesto; si ninguna lo es (dos saltos libres seguidos), cualquier lado sirve.
                    float usedSide = incomingSide ?? outgoingSide ?? (Random.value < 0.5f ? 1f : -1f);
                    float freeSide = (incomingSide.HasValue || outgoingSide.HasValue) ? -usedSide : usedSide;

                    // Una trampa solo puede aparecer si tanto el nivel anterior como el
                    // siguiente del camino principal son Normal, es decir, si este nivel no
                    // esta pegado a Spawn (nivel 0) ni a Final (ultimo nivel)
                    bool canBeTrap = level > 1 && level < totalLevels - 2;

                    int extraCount = Random.Range(minExtraPlatformsPerLevel, maxExtraPlatformsPerLevel + 1);

                    for (int e = 1; e <= extraCount; e++)
                    {
                        string extraTag = (canBeTrap && Random.value < trapChance) ? "Trampa" : "Normal";
                        path.Add(new PathPlatform(level, extraTag, freeSide));
                    }
                }
                // Si ambos lados estan reclamados por escaleras reales, este nivel no recibe
                // extras: cualquiera de los dos lados bloquearia una de las dos escaleras.
            }

            // El lado de entrada del proximo nivel es el opuesto al lado de salida que se acaba
            // de usar para llegar a el (solo si esa conexion es una escalera real).
            incomingSide = outgoingSide.HasValue ? -outgoingSide.Value : (float?)null;

            if (hasNextLevel)
                currentAngle = NormalizeAngle(currentAngle + step);
        }

        return path;
    }

    // Magnitud (grados, sin signo) de un paso "salto libre" (cambio de direccion): muestrea el
    // rango minStepAngle/maxStepAngle original, pero rechaza cualquier resultado que caiga cerca
    // de ladderStepCenter (el angulo que alinea anchors). Sin este rechazo, ese rango ancho de
    // todos modos INCLUYE la ventana que crea escalera (ladderStepCenter esta tipicamente bien
    // adentro de [minStepAngle, maxStepAngle]), asi que una fraccion de los saltos libres
    // terminaba cayendo ahi por pura casualidad y generaba la misma escalera-sobre-escalera que
    // este mecanismo existe para evitar. Se usa un margen mayor a la tolerancia real de inclinacion
    // para no depender de que el rechazo sea exacto al limite.
    float FreeStepMagnitude(float ladderStepCenter, float ladderTolerance)
    {
        float exclusionHalfWidth = ladderTolerance + 5f;

        float magnitude = Random.Range(minStepAngle, maxStepAngle);
        int attempts = 0;
        while (Mathf.Abs(magnitude - ladderStepCenter) < exclusionHalfWidth && attempts < 20)
        {
            magnitude = Random.Range(minStepAngle, maxStepAngle);
            attempts++;
        }

        return magnitude;
    }

    // Angulo maximo (grados) que puede desviarse el paso entre un nivel y el siguiente respecto
    // del angulo que alinea sus anchors, sin que la conexion vertical supere maxLadderTiltAngle.
    // Misma formula que usa PuzzlePathPlacer para su propio tope de inclinacion (atan2(run, radius)).
    float MaxStepAngleForTilt(float radius, float levelHeight, float maxLadderTiltAngle)
    {
        if (radius <= 0f) return maxStepAngle;

        // maxLadderTiltAngle se mide desde la VERTICAL (0 = escalera perfectamente vertical), a
        // diferencia del tope de inclinacion de PuzzlePathPlacer para rampas (que se mide desde la
        // HORIZONTAL: una rampa mas "tendida" necesita MAS run para la misma altura). Por eso aca
        // es "levelHeight * tan", no "levelHeight / tan": cuanto mas chico el angulo permitido
        // respecto de la vertical, menos run horizontal se tolera entre los dos anchors.
        float clampedTilt = Mathf.Clamp(maxLadderTiltAngle, 1f, 89f);
        float maxRun = levelHeight * Mathf.Tan(clampedTilt * Mathf.Deg2Rad);
        return Mathf.Atan2(maxRun, radius) * Mathf.Rad2Deg;
    }

    string GetTagForLevel(int level, int totalLevels)
    {
        if (level == 0) return "Spawn";
        if (level == totalLevels - 1) return "Final";
        return "Normal";
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}
