using UnityEngine;

[System.Serializable]
public class MissionPart
{
    public enum PartType { Texto, Socket }

    public PartType tipo;
    public string texto;         // Lo que se muestra (ej: "la", "bote", "manzana")
    public string idCorrecto;    // Solo para sockets

    public bool EsSocket => tipo == PartType.Socket;
}

