using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestObjInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] float interactionRange = 3f;

    [Header("Export")]
    [SerializeField] string fileName = "TestObj_data.json";
    [SerializeField] string objectId = "TestObj";

    Transform player;

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null)
            return;

        bool inRange = Vector3.Distance(transform.position, player.position) <= interactionRange;
        if (inRange && Keyboard.current.eKey.wasPressedThisFrame)
            ExportToJson();
    }

    void ExportToJson()
    {
        PlacedObjectData info = new PlacedObjectData
        {
            id = objectId,
            name = gameObject.name,
            tag = gameObject.tag,
            position = transform.position,
            rotation = transform.eulerAngles,
            scale = transform.localScale
        };

        string json = JsonUtility.ToJson(info, true);
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, json);

        Debug.Log($"Exported {gameObject.name} data to {path}");

        gameObject.SetActive(false);
    }
}
