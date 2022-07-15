// Interactions
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets
{
    public class Interactor : MonoBehaviour
    {
        #region Fields
        [SerializeField] protected Camera interactionCamera;
        #endregion

        #region Properties
        public Camera InteractionCamera => interactionCamera;
        #endregion

        #region Methods
        public virtual void Setup()
        {
            InteractionsManager.Instance.Interactor = this;
        }
        private void Destroy()
        {
            if (InteractionsManager.Instance.Interactor == this) { InteractionsManager.Instance.Interactor = null; }
        }
        #endregion
    }
}