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
    [Tooltip("Cuanto puede variar el paso (en grados) alrededor del angulo que alinea los anchors L/R de dos niveles consecutivos (ver GeneratePath), para que el espiral no sea perfectamente uniforme. La geometria de la escalera manda si es mas estricta: el jitter real queda acotado por la tolerancia de inclinacion de LadderPlacer")]
    public float minStepAngle = 20f;
    [Tooltip("Ver minStepAngle: junto con el forman el rango de variacion del paso alrededor del angulo que alinea anchors")]
    public float maxStepAngle = 100f;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que el camino cambie de sentido (horario/antihorario) en cada nivel intermedio, en vez de seguir girando siempre para el mismo lado. Nunca deja el tramo sin escalera (ver GeneratePath): un cambio de sentido usa un paso CHICO en vez de saltar a un angulo arbitrario, para seguir enganchando un anchor real y cercano a la vertical. Los niveles con cambio de sentido no llevan plataformas extra encadenadas (ver comentario grande de GeneratePath)")]
    public float directionChangeChance = 0.3f;

    [Header("Path - Plataformas Extra")]
    [Tooltip("Cantidad minima de plataformas extra (ademas de la principal) en cada nivel intermedio que 'continua' girando para el mismo lado (ver directionChangeChance). Se encadenan por anchors al anchor de SALIDA de la principal -- igual que al principio de la sesion -- y el paso al siguiente nivel se alarga automaticamente para compensar el ancho agregado, asi que la escalera de salida sigue enganchando el anchor libre del ULTIMO eslabon de la cadena")]
    public int minExtraPlatformsPerLevel = 2;
    [Tooltip("Cantidad maxima de plataformas extra (ademas de la principal) en un nivel que 'continua'. Junto con minExtraPlatformsPerLevel definen el total de plataformas por nivel (principal + extras)")]
    public int maxExtraPlatformsPerLevel = 3;
    // La separacion angular entre plataformas extra ya no es un valor fijo: CylinderRender la
    // calcula usando los anchors L/R de cada prefab para que queden pegadas por sus bordes sin
    // superponerse, sea cual sea su tamaño o el radio del cilindro (ver GenerateTower).

    [Header("Path - Trampas")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que una plataforma extra sea una Trampa en vez de Normal. Las trampas solo pueden aparecer en niveles intermedios cuyo nivel anterior Y siguiente del camino principal sean Normal (nunca pegadas a Spawn o Final)")]
    public float trapChance = 0.25f;

    // Genera un camino continuo desde el nivel base hasta el nivel mas alto. En cada nivel
    // intermedio decide si el paso hacia el siguiente CONTINUA girando para el mismo lado o
    // CAMBIA de sentido (ver 'directionChangeChance'); en ambos casos apunta a un angulo que deja
    // una escalera real y casi vertical -- las escaleras son la UNICA forma de subir el camino
    // principal, asi que ninguna conexion puede quedar sin apuntar a una.
    //
    // 'radius', 'levelHeight' y 'maxLadderTiltAngle' definen la geometria de la escalera.
    // 'connectorHalfWidthAngle' es el angulo (grados) que ocupa, desde el centro de una
    // plataforma principal hasta su anchor L/R, a ese radio (ver CylinderRender.HalfWidthToAngle).
    //
    // LadderPlacer no conecta los centros de dos plataformas principales: conecta el anchor de
    // cada una mas cercano a la otra, y NUNCA reutiliza para la escalera de SALIDA el mismo anchor
    // que ya uso la escalera de ENTRADA de esa plataforma (si no, quedaria una escalera literalmente
    // arriba de otra: el jugador sube y sigue subiendo sin caminar nada). Por eso el paso que se
    // apunta depende de si se continua o se cambia de sentido:
    //
    // - CONTINUAR (mismo sentido que el paso de entrada): el paso que alinea el anchor de "salida"
    //   de la plataforma de abajo con el anchor de "entrada" de la de arriba es 2 * connectorHalfWidthAngle
    //   (ver 'ladderStepCenter'). Este paso usa el anchor OPUESTO al de entrada automaticamente
    //   (geometria del anchor L/R), asi que nunca choca con el de 'lowerIncomingAnchor'. Ademas, en
    //   este caso se encadenan 'minExtraPlatformsPerLevel'..'maxExtraPlatformsPerLevel' plataformas
    //   extra desde ESE MISMO anchor de salida (como al principio de la sesion, pegadas por sus
    //   bordes), y CylinderRender redirige el anchor efectivo de salida de la principal al extremo
    //   libre del ultimo eslabon (ver CylinderPlatformObj.SetEffectiveAnchorL/R) -- por eso el paso
    //   se alarga con el ancho total de la cadena (ver 'chainedOffset' abajo), para que la escalera
    //   siga apuntando al anchor real donde queda el extremo libre.
    //
    // - CAMBIAR DE SENTIDO: si se apuntara al mismo 'ladderStepCenter' pero invertido, la escalera
    //   de salida terminaria pegada al MISMO anchor que la de entrada (misma "conexion cerrada",
    //   rompiendo el maze). Pero un paso CHICO (bien menor a 'ladderStepCenter') en el sentido
    //   invertido hace que el par de anchors mas cercano a la vertical pase a ser el mismo LADO en
    //   ambas plataformas (p. ej. anchor R de la de abajo con anchor R de la de arriba) en vez del
    //   lado opuesto -- y ese lado NO es el que uso la escalera de entrada, asi que sigue siendo un
    //   anchor libre. La distancia angular entre esos dos anchors del mismo lado es exactamente la
    //   magnitud del paso chico, asi que mientras se mantenga acotada por la tolerancia de
    //   inclinacion de la escalera (igual que el jitter de 'continuar'), la conexion sigue siendo
    //   casi vertical y valida. Ese margen es demasiado chico para sumarle encima el ancho de una
    //   cadena de extras sin salirse de la tolerancia, asi que estos niveles no llevan extras: se
    //   quedan solo con su plataforma principal.
    public List<PathPlatform> GeneratePath(int totalLevels, float radius, float levelHeight, float maxLadderTiltAngle, float connectorHalfWidthAngle)
    {
        List<PathPlatform> path = new List<PathPlatform>();

        if (totalLevels <= 0) return path;

        float ladderStepCenter = connectorHalfWidthAngle * 2f;
        float ladderTolerance = MaxStepAngleForTilt(radius, levelHeight, maxLadderTiltAngle);

        // Margen de seguridad: la formula de tolerancia asume que el anchor conector queda
        // exactamente al radio del cilindro, pero en realidad queda un poco mas lejos del eje
        // (offset tangencial => hipotenusa levemente mayor a 'radius'). Se recorta un 15% para
        // asegurar margen real, no solo el limite teorico (mismo motivo en ambos casos de abajo).
        const float toleranceSafetyFactor = 0.85f;
        float safeTolerance = ladderTolerance * toleranceSafetyFactor;

        // "Continuar": jitter alrededor del paso central (ladderStepCenter + ancho de cadena, ver
        // abajo), acotado por minStepAngle/maxStepAngle (variedad visual) y por la tolerancia real
        // de la escalera (lo que manda si es mas estricto).
        float userSpread = Mathf.Max(0f, (maxStepAngle - minStepAngle) / 2f);
        float continueJitter = Mathf.Min(userSpread, safeTolerance);

        // "Cambiar de sentido": paso chico (no ladderStepCenter) en el sentido invertido -- ver
        // comentario grande de arriba sobre por que esto es lo que evita reusar el mismo anchor.
        // Se deja un minimo chico (30% del maximo) para que el cambio de sentido sea visualmente
        // perceptible incluso cuando la tolerancia es muy ajustada.
        float reversalStepMax = Mathf.Max(0.5f, safeTolerance);
        float reversalStepMin = reversalStepMax * 0.3f;

        float currentAngle = Random.Range(0f, 360f);
        float direction = Random.value < 0.5f ? 1f : -1f;

        for (int level = 0; level < totalLevels; level++)
        {
            string tag = GetTagForLevel(level, totalLevels);
            float mainAngle = currentAngle;

            path.Add(new PathPlatform(level, mainAngle, tag));

            bool isEndpoint = level == 0 || level == totalLevels - 1;
            bool hasNextLevel = level < totalLevels - 1;

            if (hasNextLevel)
            {
                // El tramo Spawn->nivel1 y el que llega al Final se fuerzan siempre "continuar":
                // son los unicos puntos de entrada/salida garantizados del camino (ver tambien el
                // relleno de anillo de Spawn en CylinderRender), asi que se mantienen simples.
                bool mustGuaranteeLadder = level == 0 || level == totalLevels - 2;
                bool isDirectionChange = !mustGuaranteeLadder && Random.value < directionChangeChance;

                float step;
                if (isDirectionChange)
                {
                    direction *= -1f;
                    float magnitude = Random.Range(reversalStepMin, reversalStepMax);
                    step = magnitude * direction;
                }
                else
                {
                    // Cantidad de extras para ESTE nivel, encadenadas desde el anchor de salida.
                    // 'chainedOffset' es el angulo (desde el CENTRO de la principal) al extremo
                    // libre del ultimo eslabon: cada eslabon agrega 2*connectorHalfWidthAngle (su
                    // propio medio-ancho mas el de la plataforma a la que se pega, asumiendo que
                    // todos los prefabs comparten el mismo offset de anchor -- ver
                    // CylinderRender.GetConnectorHalfWidthAngle), y el propio anchor de la
                    // principal ya aporta el primer 'connectorHalfWidthAngle'. Con 0 extras
                    // (chainedOffset = connectorHalfWidthAngle) esto colapsa exactamente al caso
                    // sin cadena de siempre.
                    int extraCount = isEndpoint ? 0 : Random.Range(minExtraPlatformsPerLevel, maxExtraPlatformsPerLevel + 1);
                    float chainedOffset = (2 * extraCount + 1) * connectorHalfWidthAngle;

                    // El nivel siguiente siempre recibe la escalera en su anchor propio (nunca
                    // encadenado): el paso central es la suma de ambos offsets.
                    float centerStep = chainedOffset + connectorHalfWidthAngle;

                    step = (centerStep + Random.Range(-continueJitter, continueJitter)) * direction;

                    bool canBeTrap = level > 1 && level < totalLevels - 2;
                    for (int e = 0; e < extraCount; e++)
                    {
                        string extraTag = (canBeTrap && Random.value < trapChance) ? "Trampa" : "Normal";
                        path.Add(new PathPlatform(level, extraTag, direction));
                    }
                }

                currentAngle = NormalizeAngle(currentAngle + step);
            }
        }

        return path;
    }

    // Angulo maximo (grados) que puede desviarse el paso entre un nivel y el siguiente respecto
    // del angulo que alinea sus anchors, sin que la conexion vertical supere maxLadderTiltAngle.
    float MaxStepAngleForTilt(float radius, float levelHeight, float maxLadderTiltAngle)
    {
        if (radius <= 0f) return maxStepAngle;

        // maxLadderTiltAngle se mide desde la VERTICAL (0 = escalera perfectamente vertical).
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
