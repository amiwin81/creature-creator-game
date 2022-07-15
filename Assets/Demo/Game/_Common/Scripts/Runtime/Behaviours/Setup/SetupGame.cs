// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class SetupGame : MonoBehaviour
    {
        #region Fields
        [SerializeField] private NetworkObject playerPrefab;
        #endregion

        #region Methods
        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            EditorManager.Instance.UnlockedBodyParts = ProgressManager.Data.UnlockedBodyParts;
            EditorManager.Instance.UnlockedPatterns = ProgressManager.Data.UnlockedPatterns;
            EditorManager.Instance.BaseCash = ProgressManager.Data.Cash;
            EditorManager.Instance.HiddenBodyParts = SettingsManager.Data.HiddenBodyParts;
            EditorManager.Instance.HiddenPatterns = SettingsManager.Data.HiddenPatterns;

            if (NetworkConnectionManager.IsConnected)
            {
                SetupMP();
            }
            else
            {
                SetupSP();
            }
        }
        
        public void SetupMP()
        {
            if (NetworkManager.Singleton.IsHost)//
            {
                //EditorManager.Instance.Player = SpawnPlayer(NetworkManager.Singleton.LocalClientId).GetComponent<Player>();

                //SpawnPlayer(NetworkManager.Singleton.LocalClientId);
                //NetworkManager.Singleton.OnClientConnectedCallback += (ulong clientId) => SpawnPlayer(clientId);
            }
            else//
            {
                //EditorManager.Instance.Player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>();
            }


            //EditorManager.Instance.Setup();

            //NetworkCreaturesManager.Instance.Setup();

            //Lobby lobby = LobbyHelper.Instance.JoinedLobby;
            //World world = new World(lobby);
            //if (NetworkManager.Singleton.IsHost)
            //{
            //    if (world.IsPrivate)
            //    {
            //        InformationDialog.Inform("Private World", $"The code to your private world is:\n<u><b>{lobby.Id}</b></u>\n\nPress the button below to copy it to your clipboard. Press {KeybindingsManager.Data.ViewPlayers} to view it again.", "Copy", true, delegate
            //        {
            //            GUIUtility.systemCopyBuffer = lobby.Id;
            //        });
            //    }
            //    if (!world.SpawnNPC)
            //    {
            //        foreach (NetworkCreatureNonPlayer creature in FindObjectsOfType<NetworkCreatureNonPlayer>())
            //        {
            //            creature.Despawn();
            //        }
            //    }
            //}
        }
        private void SetupSP()
        {
            EditorManager.Instance.Player = Instantiate(playerPrefab).GetComponent<Player>();
            EditorManager.Instance.Setup();
        }



        public NetworkObject SpawnPlayer(ulong clientId)
        {
            NetworkObject player = Instantiate(playerPrefab);
            player.SpawnAsPlayerObject(clientId);

            return player;
        }
        #endregion
    }
}