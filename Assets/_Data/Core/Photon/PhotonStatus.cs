using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonStatus : MonoBehaviourPunCallbacks
{
    private void Update()
    {
        Debug.Log("Photon Status: " + PhotonNetwork.NetworkClientState);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected To Master Server");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected: " + cause);
    }
}