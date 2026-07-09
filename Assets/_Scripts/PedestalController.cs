using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class PedestalController : MonoBehaviour
{
    [System.Serializable]
    public class PedestalEntry
    {
        public GameObject pedestal;
        public Transform placementPoint;
    }

    [SerializeField] List<PedestalEntry> pedestals = new List<PedestalEntry>();

    [Header("Interaction")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] float interactionRange = 3f;

    [Header("Placement")]
    [SerializeField] GameObject itemTemplate;
    [SerializeField] string dataFileName = "TestObj_data.json";

    Transform player;

    void Start()
    {
        LogNames();
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        PedestalEntry nearest = GetNearestPedestalInRange();
        if (nearest != null)
            PlaceItemAt(nearest);
    }

    PedestalEntry GetNearestPedestalInRange()
    {
        PedestalEntry nearest = null;
        float nearestDist = interactionRange;

        foreach (PedestalEntry entry in pedestals)
        {
            if (entry.pedestal == null)
                continue;

            float dist = Vector3.Distance(entry.pedestal.transform.position, player.position);
            if (dist <= nearestDist)
            {
                nearestDist = dist;
                nearest = entry;
            }
        }

        return nearest;
    }

    void PlaceItemAt(PedestalEntry entry)
    {
        if (entry.placementPoint == null)
        {
            Debug.Log($"{entry.pedestal.name} has no placement point assigned.");
            return;
        }

        if (itemTemplate == null)
        {
            Debug.Log("No item template assigned to PedestalController.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, dataFileName);
        if (!File.Exists(path))
        {
            Debug.Log($"No saved object data found at {path}.");
            return;
        }

        PlacedObjectData data = JsonUtility.FromJson<PlacedObjectData>(File.ReadAllText(path));

        GameObject copy = Instantiate(itemTemplate, entry.placementPoint.position, Quaternion.Euler(data.rotation));
        copy.transform.localScale = data.scale;
        copy.name = string.IsNullOrEmpty(data.id) ? data.name : data.id;
        copy.SetActive(true);

        Debug.Log($"Placed {copy.name} on {entry.pedestal.name}");
    }

    public void LogNames()
    {
        foreach (PedestalEntry entry in pedestals)
        {
            if (entry.pedestal != null)
                Debug.Log(entry.pedestal.name);
        }
    }
}
