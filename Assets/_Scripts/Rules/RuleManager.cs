using System.Collections.Generic;
using UnityEngine;

// Datos de una plataforma del camino: en que nivel, a que angulo y con que tag debe colocarse
[System.Serializable]
public struct PathPlatform
{
    public int level;
    public float angle;
    public string tag;

    public PathPlatform(int level, float angle, string tag)
    {
        this.level = level;
        this.angle = angle;
        this.tag = tag;
    }
}

public class RuleManager : MonoBehaviour
{
    [Header("Path - Espiral")]
    [Tooltip("Minimo de vueltas completas (360°) que da el camino a lo largo de toda la torre")]
    public float minSpiralTurns = 1f;
    [Tooltip("Maximo de vueltas completas (360°) que da el camino a lo largo de toda la torre")]
    public float maxSpiralTurns = 2f;
    [Tooltip("Variacion aleatoria maxima (en grados) aplicada al angulo de cada nivel")]
    public float angleJitter = 15f;

    [Header("Path - Plataformas Extra")]
    [Tooltip("Cantidad minima de plataformas extra (ademas de la principal) en cada nivel intermedio del camino")]
    public int minExtraPlatformsPerLevel = 0;
    [Tooltip("Cantidad maxima de plataformas extra (ademas de la principal) en cada nivel intermedio del camino")]
    public int maxExtraPlatformsPerLevel = 1;
    [Tooltip("Separacion angular entre cada plataforma extra consecutiva del mismo nivel (se encadenan en una sola direccion para simular una plataforma larga)")]
    public float extraPlatformSpacing = 25f;

    // Genera un camino continuo desde el nivel base hasta el nivel mas alto,
    // envolviendo el cilindro (espiral) en vez de subir en linea recta.
    // Solo usa los tags Spawn (nivel base), Normal (niveles intermedios) y Final (nivel mas alto).
    public List<PathPlatform> GeneratePath(int totalLevels)
    {
        List<PathPlatform> path = new List<PathPlatform>();

        if (totalLevels <= 0) return path;

        float totalTurns = Random.Range(minSpiralTurns, maxSpiralTurns);
        float direction = Random.value < 0.5f ? 1f : -1f;
        float totalSweep = totalTurns * 360f * direction;

        float startAngle = Random.Range(0f, 360f);

        for (int level = 0; level < totalLevels; level++)
        {
            float t = totalLevels > 1 ? (float)level / (totalLevels - 1) : 0f;
            float baseAngle = startAngle + totalSweep * t;
            float jitter = Random.Range(-angleJitter, angleJitter);
            float angle = NormalizeAngle(baseAngle + jitter);

            string tag = GetTagForLevel(level, totalLevels);

            path.Add(new PathPlatform(level, angle, tag));

            // Plataformas extra en niveles intermedios (siempre Normal), encadenadas en
            // una sola direccion para que se sientan como una plataforma larga
            bool isEndpoint = level == 0 || level == totalLevels - 1;
            if (!isEndpoint)
            {
                int extraCount = Random.Range(minExtraPlatformsPerLevel, maxExtraPlatformsPerLevel + 1);
                float offsetSign = Random.value < 0.5f ? 1f : -1f;

                for (int e = 1; e <= extraCount; e++)
                {
                    float extraAngle = NormalizeAngle(angle + extraPlatformSpacing * e * offsetSign);
                    path.Add(new PathPlatform(level, extraAngle, "Normal"));
                }
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
