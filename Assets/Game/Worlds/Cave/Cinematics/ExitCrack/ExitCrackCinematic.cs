using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator.Cinematics.Cave
{
    public class ExitCrackCinematic : TeleportCinematic
    {
        #region Methods
        public override void Show()
        {
            base.Show();
            SpawnCreature(spawnPoint, TeleportManager.dataBuffer);
        }
        #endregion
    }
}