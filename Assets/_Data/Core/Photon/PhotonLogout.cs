using UnityEngine;
using Photon.Pun;

public class PhotonLogout : MonoBehaviour
{
    public void Logout()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Not Connected");
            return;
        }

        PhotonNetwork.Disconnect();

        Debug.Log("Disconnecting...");
    }
}