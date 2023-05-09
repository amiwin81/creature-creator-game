using GoogleMobileAds.Api;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace DanielLochner.Assets.CreatureCreator
{
    public class PremiumMenu : Dialog<PremiumMenu>
    {
        #region Fields
        [Header("Paid")]
        [SerializeField] private RectTransform paidRT;
        [SerializeField] private TextMeshProUGUI priceText;

        [Header("Free")]
        [SerializeField] private RectTransform freeRT;
        [SerializeField] private Image requestedItemImg;
        [SerializeField] private BlinkingCanvasGroup requestedItemBCG;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private GameObject watchAdText;
        [SerializeField] private GameObject watchAdProgress;
        [SerializeField] private Sprite questionMarkIcon;
        #endregion

        #region Methods
        protected override void Start()
        {
            base.Start();
            if (PremiumManager.Instance.IsEverythingUsable())
            {
                paidRT.pivot = new Vector2(0.5f, 0.5f);
                paidRT.anchoredPosition = Vector2.zero;

                freeRT.gameObject.SetActive(false);
            }
        }

        public void RequestBodyPart(string bodyPartId)
        {
            PremiumManager.Instance.RequestedItem = new PremiumManager.RewardedItem(PremiumManager.ItemType.BodyPart, bodyPartId);
            Setup(DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", bodyPartId).Icon, false);
        }
        public void RequestPattern(string patternId)
        {
            PremiumManager.Instance.RequestedItem = new PremiumManager.RewardedItem(PremiumManager.ItemType.Pattern, patternId);
            Setup(DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", patternId).Icon, false);
        }
        public void RequestNothing()
        {
            PremiumManager.Instance.RequestedItem = null;
            Setup(questionMarkIcon, true);
        }
        private void Setup(Sprite icon, bool isBlinking)
        {
            if (!CodelessIAPStoreListener.initializationComplete)
            {
                return;
            }

            // Paid
            priceText.text = CodelessIAPStoreListener.Instance.GetProduct("cc_premium").metadata.localizedPriceString;

            // Free
            requestedItemBCG.IsBlinking = isBlinking;
            requestedItemBCG.CanvasGroup.alpha = 1f;
            requestedItemImg.sprite = icon;

            Open();
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);

            watchAdProgress.SetActive(true);
            watchAdText.SetActive(false);
            watchAdButton.interactable = true;

            PremiumManager.Instance.RequestRewardAd(OnRewardAdLoaded);
        }

        public void WatchAd()
        {
            PremiumManager.Instance.ShowRewardAd();
            watchAdButton.interactable = false;
        }
        public void OnRewardAdLoaded(RewardedAd ad, LoadAdError error)
        {
            watchAdProgress.SetActive(false);
            watchAdText.SetActive(true);
        }

        public void OnPurchaseComplete(Product product)
        {
            if (product.definition.id == "cc_premium")
            {
                PremiumManager.Instance.OnPremiumPurchased();
            }
        }
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            PremiumManager.Instance.OnPremiumFailed(reason.ToString());
        }
        #endregion
    }
}