using UnityEngine;

public class CustomNetworkManagerSample : MonoBehaviour
{
    public CustomNetworkManager customNetworkManager;

    protected virtual void Start()
    {
        this.customNetworkManager.networkAddress = "127.0.0.1";
        this.customNetworkManager.networkPort = 5555;
    }

    protected virtual void Update ()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            this.customNetworkManager.autoConnect = !this.customNetworkManager.autoConnect;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            this.customNetworkManager.StartServerSafe();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            this.customNetworkManager.StartHostSafe();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            this.customNetworkManager.StartClientSafe();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            this.customNetworkManager.Stop();
        }
    }

    protected virtual void OnGUI()
    {
        GUILayout.Label("= Log =");

        foreach (CustomNetworkManager.UNETStatusMessage statusMessage
                in this.customNetworkManager.StatusMessages)
        {
            GUILayout.Label(statusMessage.time.ToLongTimeString() + " - "+ statusMessage.message);
        }
    }
}