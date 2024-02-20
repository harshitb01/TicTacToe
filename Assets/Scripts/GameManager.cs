using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>(0);
    public static GameManager Instance;
    [SerializeField] private GameObject gamePrefab;
    private GameObject newGame;
    [SerializeField] private Text joinCodeText;
    [SerializeField] private InputField joinCodeInput;
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private Text statusText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private async void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log("Client with id " + clientId + " joined");
            if (NetworkManager.Singleton.IsHost &&
            NetworkManager.Singleton.ConnectedClients.Count == 2)
            {
                SpwanGame();
            }
        };

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void SpwanGame()
    {
        newGame = Instantiate(gamePrefab);
        newGame.GetComponent<NetworkObject>().Spawn();
    }

    public async void StartHost()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeText.text = joinCode;


            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void StartClient()
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

    }

    public void ShowMsg(string msg)
    {
        if (msg.Equals("won"))
        {
            statusText.text = "You Won";
            exitScreen.SetActive(true);
            ShowOpponentMsg("You Lose");
        }
        else if (msg.Equals("draw"))
        {
            statusText.text = "Game Draw";
            exitScreen.SetActive(true);
            ShowOpponentMsg("Game Draw");
        }
    }

    private void ShowOpponentMsg(string msg)
    {
        if (IsHost)
        {
            OpponentMsgClientRpc(msg);
        }
        else
        {
            OpponentMsgServerRpc(msg);
        }
    }

    [ClientRpc]
    private void OpponentMsgClientRpc(string msg)
    {
        if (IsHost) return;
        statusText.text = msg;
        exitScreen.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpponentMsgServerRpc(string msg)
    {
        statusText.text = msg;
        exitScreen.SetActive(true);
    }

    public void Restart()
    {
        if (!IsHost)
        {
            RestartServerRpc();
            exitScreen.SetActive(false);
        }
        else
        {
            Destroy(newGame);
            SpwanGame();
            RestartClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartServerRpc()
    {
        Destroy(newGame);
        SpwanGame();
        exitScreen.SetActive(false);
    }

    [ClientRpc]
    private void RestartClientRpc()
    {
        exitScreen.SetActive(false);
    }
}