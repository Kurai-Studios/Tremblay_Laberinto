using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TMPFontReplacer : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset defaultTMPFont;

#if UNITY_EDITOR
    [ContextMenu("Reemplazar fuentes TMP en la escena")]
    void ReplaceFontsInScene()
    {
        if (defaultTMPFont == null)
        {
            Debug.LogError("⚠️ No has asignado una fuente TMP por defecto.");
            return;
        }

        var texts = FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;

        foreach (var t in texts)
        {
            if (t.font != defaultTMPFont)
            {
                t.font = defaultTMPFont;
                EditorUtility.SetDirty(t);
                count++;
            }
        }

        Debug.Log($"✅ Se reemplazaron {count} textos TMP en la escena por {defaultTMPFont.name}");
    }
#endif
}
