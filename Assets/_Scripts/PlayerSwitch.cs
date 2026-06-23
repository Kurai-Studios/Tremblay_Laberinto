using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

[System.Serializable]
public class CharacterData
{
    public GameObject character;
    public Transform cameraTarget;
}

public class PlayerSwitch : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    [SerializeField] private int currentCharIndex = 0;
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineCamera vCam;
    [SerializeField] private bool autoFindCam = true;

    private void Start()
    {

        if (vCam == null && autoFindCam)
        {
            vCam = FindFirstObjectByType<CinemachineCamera>();

            if (vCam == null)
            {
                Debug.LogWarning("No CinemachineVirtualCamera found in scene!");
            }
        }

        if (characters.Count > 0) SwitchToCharacter(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey) && characters.Count > 0) SwitchToNextCharacter();
    }

    void SwitchToNextCharacter()
    {
        if (characters.Count == 0) return;

        int nextIndex = (currentCharIndex + 1) % characters.Count;
        SwitchToCharacter(nextIndex);
    }

    void SwitchToCharacter(int index)
    {
        if (index < 0 || index >= characters.Count) return;
        if (characters[index].character == null) return;

        // Deactivate all
        foreach (CharacterData data in characters)
        {
            if (data.character != null) data.character.SetActive(false);
        }

        // Activate selected character
        characters[index].character.SetActive(true);
        currentCharIndex = index;

        // Update Cinemachine cam
        if (vCam != null && characters[index].cameraTarget != null)
        {
            vCam.Follow = characters[index].cameraTarget;
        }
        else if (vCam != null && characters[index].character != null)
        {
            vCam.Follow = characters[index].character.transform;
            Debug.LogWarning($"No camera target assigned for {characters[index].character.name}. Using character transform.");
        }

        Debug.Log($"Switched to character {index}: {characters[index].character.name}");
    }
}