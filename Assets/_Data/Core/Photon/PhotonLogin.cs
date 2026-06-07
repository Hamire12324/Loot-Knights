using UnityEngine;
using Photon.Pun;

public class PhotonLogin : MonoBehaviour
{
    [SerializeField] private string gameVersion = "1.0";

    private void Start()
    {
        Login();
    }

    public void Login()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already Connected");
            return;
        }

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        Debug.Log("Connecting To Photon...");
    }
}