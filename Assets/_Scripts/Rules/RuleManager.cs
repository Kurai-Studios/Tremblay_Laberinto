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
    [Tooltip("Angulo minimo (en grados) que avanza el camino entre un nivel y el siguiente")]
    public float minStepAngle = 20f;
    [Tooltip("Angulo maximo (en grados) que avanza el camino entre un nivel y el siguiente")]
    public float maxStepAngle = 100f;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de cambiar de direccion (horario/antihorario) en cada nivel")]
    public float directionChangeChance = 0.3f;

    [Header("Path - Plataformas Extra")]
    [Tooltip("Cantidad minima de plataformas extra (ademas de la principal) en cada nivel intermedio del camino")]
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
    public List<PathPlatform> GeneratePath(int totalLevels)
    {
        List<PathPlatform> path = new List<PathPlatform>();

        if (totalLevels <= 0) return path;

        float currentAngle = Random.Range(0f, 360f);
        float direction = Random.value < 0.5f ? 1f : -1f;

        for (int level = 0; level < totalLevels; level++)
        {
            string tag = GetTagForLevel(level, totalLevels);

            path.Add(new PathPlatform(level, currentAngle, tag));

            // Plataformas extra en niveles intermedios (Normal o, con cierta probabilidad,
            // Trampa), encadenadas en una sola direccion para que se sientan como una
            // plataforma larga
            bool isEndpoint = level == 0 || level == totalLevels - 1;
            if (!isEndpoint)
            {
                // Una trampa solo puede aparecer si tanto el nivel anterior como el
                // siguiente del camino principal son Normal, es decir, si este nivel no
                // esta pegado a Spawn (nivel 0) ni a Final (ultimo nivel)
                bool canBeTrap = level > 1 && level < totalLevels - 2;

                int extraCount = Random.Range(minExtraPlatformsPerLevel, maxExtraPlatformsPerLevel + 1);
                float offsetSign = Random.value < 0.5f ? 1f : -1f;

                for (int e = 1; e <= extraCount; e++)
                {
                    string extraTag = (canBeTrap && Random.value < trapChance) ? "Trampa" : "Normal";
                    path.Add(new PathPlatform(level, extraTag, offsetSign));
                }
            }

            // Avanzar al siguiente nivel con un paso aleatorio, cambiando de direccion
            // ocasionalmente para que el camino no sea una espiral perfectamente uniforme
            if (level < totalLevels - 1)
            {
                if (Random.value < directionChangeChance)
                    direction *= -1f;

                float step = Random.Range(minStepAngle, maxStepAngle) * direction;
                currentAngle = NormalizeAngle(currentAngle + step);
            }
        }

        return path;
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
