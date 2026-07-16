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
        public string acceptedObjectId;
    }

    [SerializeField] List<PedestalEntry> pedestals = new List<PedestalEntry>();

    [Header("Interaction")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] float interactionRange = 3f;

    [Header("Placement")]
    [SerializeField] string dataFileName = "TestObj_data.json";

    Transform player;
    readonly Dictionary<string, GameObject> placeableObjectsById = new Dictionary<string, GameObject>();

    void Start()
    {
        LogNames();
        BuildPlaceableObjectRegistry();
    }

    void BuildPlaceableObjectRegistry()
    {
        placeableObjectsById.Clear();
        foreach (TestObjInteractor interactor in FindObjectsByType<TestObjInteractor>(FindObjectsSortMode.None))
        {
            if (!string.IsNullOrEmpty(interactor.ObjectId))
                placeableObjectsById[interactor.ObjectId] = interactor.gameObject;
        }
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

        string path = Path.Combine(Application.persistentDataPath, dataFileName);
        if (!File.Exists(path))
        {
            Debug.Log($"No saved object data found at {path}.");
            return;
        }

        PlacedObjectData data = JsonUtility.FromJson<PlacedObjectData>(File.ReadAllText(path));

        if (entry.acceptedObjectId != data.id)
        {
            Debug.LogWarning($"{entry.pedestal.name} only accepts '{entry.acceptedObjectId}', but tried to place '{data.id}'.");
            return;
        }

        if (!placeableObjectsById.TryGetValue(data.id, out GameObject source))
        {
            Debug.Log($"No placeable object found in scene for id '{data.id}'.");
            return;
        }

        GameObject copy = Instantiate(source, entry.placementPoint.position, Quaternion.Euler(data.rotation));
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
