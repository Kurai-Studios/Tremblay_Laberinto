using UnityEngine;

[RequireComponent(typeof(Animation))]
public class PlayAllClips : MonoBehaviour
{
    [Header("Clips del FBX � arrastra desde el Project panel")]
    public AnimationClip[] clips;

    private Animation animComponent;

    void Awake()
    {
        animComponent = GetComponent<Animation>();

        foreach (var clip in clips)
        {
            if (clip == null) continue;

            clip.legacy = true;
            clip.wrapMode = WrapMode.Once;

            if (animComponent.GetClip(clip.name) == null)
                animComponent.AddClip(clip, clip.name);

            Debug.Log($"[PlayAllClips] Clip registrado: {clip.name}");
        }
    }

    public void Play()
    {
        Debug.Log($"[PlayAllClips] Play() llamado. Clips disponibles: {clips.Length}");

        foreach (var clip in clips)
        {
            if (clip == null) continue;

            AnimationState state = animComponent[clip.name];
            if (state != null)
            {
                state.time = 0f;
                state.speed = 1f;
                state.weight = 1f;
                state.enabled = true;   // activa sin cancelar los demás
                Debug.Log($"[PlayAllClips] Ejecutando: {clip.name} | duracion: {clip.length}s");
            }
            else
            {
                Debug.LogError($"[PlayAllClips] Estado no encontrado para: {clip.name}");
            }
        }

        // Necesario para que el componente Animation procese los estados activos
        animComponent.Sample();
    }

    public void Stop()
    {
        animComponent.Stop();
    }
}