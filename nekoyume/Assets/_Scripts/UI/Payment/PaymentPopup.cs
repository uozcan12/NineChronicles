using Nekoyume.L10n;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class PaymentPopup : PopupWidget
    {
        private enum PopupType
        {
            AttractAction, // 재화부족 팝업에서 재화를 얻을 수 있는 경우
            PaymentCheck, // 재화 소모 확인을 요청하는 경우
            NoneAction, // 재화부족 팝업에서 재화를 얻을 수 없는 경우
        }

#region SerializeField
        [SerializeField]
        private CostIconDataScriptableObject costIconData;
        
        [SerializeField] 
        private StakeIconDataScriptableObject stakeIconData;

        [SerializeField]
        private Image costIcon;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private GameObject attractArrowObject;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextButton buttonYes;

        [SerializeField]
        private TextButton buttonNo;

        [SerializeField]
        private Button buttonClose;

        [SerializeField]
        private GameObject titleBorder;
#endregion SerializeField

        private System.Action YesCallback { get; set; }

        protected override void Awake()
        {
            base.Awake();

            buttonNo.OnClick = Cancel;
            buttonYes.OnClick = Yes;
            buttonClose.onClick.AddListener(Cancel);
            CloseWidget = Cancel;
            SubmitWidget = Yes;
        }

        private void SetPopupType(PopupType popupType)
        {
            switch (popupType)
            {
                case PopupType.AttractAction:
                    buttonYes.gameObject.SetActive(true);
                    buttonNo.gameObject.SetActive(false);
                    buttonClose.gameObject.SetActive(true);
                    attractArrowObject.SetActive(true);
                    break;
                case PopupType.PaymentCheck:
                    buttonYes.gameObject.SetActive(true);
                    buttonNo.gameObject.SetActive(true);
                    buttonClose.gameObject.SetActive(false);
                    attractArrowObject.SetActive(false);
                    break;
                case PopupType.NoneAction:
                    buttonYes.gameObject.SetActive(false);
                    buttonNo.gameObject.SetActive(false);
                    buttonClose.gameObject.SetActive(true);
                    attractArrowObject.SetActive(false);
                    break;
            }
        }

#region LackPaymentAction
        public void ShowLackPaymentLegacy(
            CostType costType,
            string cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            SetPopupType(PopupType.AttractAction);
            costIcon.overrideSprite = costIconData.GetIcon(costType);
            var title = L10nManager.Localize("UI_TOTAL_COST");
            costText.text = cost;
            var no = L10nManager.Localize("UI_NO");
            YesCallback = onAttract;
            Show(title, content, attractMessage, no, false);
        }

        public void ShowLackPaymentLegacy(
            CostType costType,
            BigInteger cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            ShowLackPaymentLegacy(costType, cost.ToString(), content, attractMessage, onAttract);
        }

        public void ShowLackRuneStone(RuneItem runeItem, int cost)
        {
            var runeStone = runeItem.RuneStone;
            var hasAttract = HasAttractActionRuneStone(runeStone);
            SetPopupType(hasAttract ? PopupType.AttractAction : PopupType.NoneAction);
            
            costIcon.overrideSprite = GetRuneStoneSprite(runeStone);
            costText.text = cost.ToString();
            
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            var content = GetRuneStoneContent(runeStone);
            var attractMessage = GetRuneStoneAttractMessage(runeStone);
            
            YesCallback = AttractRuneStone(runeStone);
            Show(title, content, attractMessage, string.Empty, false);
        }

        public void ShowLackPaymentDust(CostType costType, BigInteger cost)
        {
            var canAttract = CanAttractDust(costType);
            SetPopupType(canAttract ? PopupType.AttractAction : PopupType.NoneAction);

            costIcon.overrideSprite = costIconData.GetIcon(costType);
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            costText.text = cost.ToString();
            var content = GetLackDustContentString(costType);

            YesCallback = () => AttractDust(costType);
            Show(title, content, GetDustAttractString(costType), string.Empty, false);
        }
        
        public void ShowLackPaymentCrystal(BigInteger cost)
        {
            SetPopupType(PopupType.AttractAction);
            
            costIcon.overrideSprite = costIconData.GetIcon(CostType.Crystal);
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            costText.text = cost.ToString();
            var content = L10nManager.Localize("UI_LACK_CRYSTAL");
            var labelYesText = L10nManager.Localize("GRIND_UI_BUTTON");

            YesCallback = AttractGrind;
            Show(title, content, labelYesText, string.Empty, false);
        }

        public void ShowLackPaymentNCG(string cost, bool isStaking = false)
        {
            var canAttract = CanAttractShop();
            SetPopupType(canAttract ? PopupType.AttractAction : PopupType.NoneAction);
            
            costIcon.overrideSprite = costIconData.GetIcon(CostType.NCG);
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            costText.text = cost;
            var content = GetLackNCGContentString(isStaking);

            YesCallback = AttractShop;
            Show(title, content, L10nManager.Localize("UI_SHOP"), string.Empty, false);
        }

        public void ShowLackMonsterCollection(int stakeLevel)
        {
            SetPopupType(PopupType.AttractAction);
            
            costIcon.overrideSprite = stakeIconData.GetIcon(stakeLevel, IconType.Small);
            var title = L10nManager.Localize("UI_REQUIRED_MONSTER_COLLECTION_LEVEL");
            costText.text = L10nManager.Localize("UI_MONSTER_COLLECTION_LEVEL_FORMAT", stakeLevel);
            var content = L10nManager.Localize("UI_LACK_MONSTER_COLLECTION");

            YesCallback = AttractToMonsterCollection;
            Show(title, content, L10nManager.Localize("UI_MENU_MONSTER_COLLECTION"), string.Empty, false);
        }

        public void ShowLackApPotion(long cost)
        {
            var itemId = 500000;
            var canBuyShop = CanAttractShop() || CanAttractMobileShop(itemId);
            SetPopupType(canBuyShop ? PopupType.AttractAction : PopupType.NoneAction);
            
            costIcon.overrideSprite = costIconData.GetIcon(CostType.ApPotion);
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            costText.text = cost.ToString();
            var content = GetLackApPotionContentString();
            
            YesCallback = AttractShop;
            YesCallback = () =>
            {
                if (CanAttractMobileShop(itemId))
                {
                    AttractMobileShopAsync(itemId).Forget();
                }
                else
                {
                    AttractShop();
                }
            };
            
            Show(title, content, L10nManager.Localize("UI_SHOP"), string.Empty, false);
        }
        
        public void ShowLackHourglass(long cost)
        {
            var itemId = 400000;
            var canBuyShop = CanAttractShop() || CanAttractMobileShop(itemId);
            SetPopupType(PopupType.AttractAction);
            
            costIcon.overrideSprite = costIconData.GetIcon(CostType.Hourglass);
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            costText.text = cost.ToString();
            var content = GetLackHourglassContentString(canBuyShop);
            var labelYesText = canBuyShop ? 
                L10nManager.Localize("UI_SHOP") : 
                L10nManager.Localize("UI_MENU_MONSTER_COLLECTION");
            
            YesCallback = () =>
            {
                if (canBuyShop)
                {
                    if (CanAttractMobileShop(itemId))
                    {
                        AttractMobileShopAsync(itemId).Forget();
                    }
                    else
                    {
                        AttractShop();
                    }
                }
                else
                {
                    AttractToMonsterCollection();
                }
            };
            Show(title, content, labelYesText, string.Empty, false);
        }
#endregion LackPaymentAction

#region PaymentCheckAction
        public void ShowCheckPaymentLegacy(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract)
        {
            SetPopupType(PopupType.PaymentCheck);
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            YesCallback = () =>
            {
                if (enoughBalance)
                {
                    onPaymentSucceed.Invoke();
                }
                else
                {
                    Close(true);
                    if (costType == CostType.NCG)
                    {
                        ShowLackPaymentNCG(cost.ToString());
                        return;
                    }
                    
                    if (costType == CostType.Crystal)
                    {
                        ShowLackPaymentCrystal(cost);
                        return;
                    }
                    
                    ShowLackPaymentLegacy(costType, cost, insufficientMessage, L10nManager.Localize("UI_YES"), onAttract);
                }
            };

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }

        public void ShowCheckPaymentDust(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
            System.Action onPaymentSucceed)
        {
            SetPopupType(PopupType.PaymentCheck);
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            YesCallback = () =>
            {
                if (enoughBalance)
                {
                    onPaymentSucceed.Invoke();
                }
                else
                {
                    Close(true);
                    ShowLackPaymentDust(costType, cost);
                }
            };

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }

        public void ShowCheckPaymentNCG(
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
            System.Action onPaymentSucceed,
            bool isStaking = false)
        {
            SetPopupType(PopupType.PaymentCheck);
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(CostType.NCG);

            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            YesCallback = () =>
            {
                if (enoughBalance)
                {
                    onPaymentSucceed.Invoke();
                }
                else
                {
                    Close(true);
                    ShowLackPaymentNCG(cost.ToString(), isStaking);
                }
            };

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }

        public void ShowCheckPaymentCrystal(
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
            System.Action onPaymentSucceed)
        {
            SetPopupType(PopupType.PaymentCheck);
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(CostType.Crystal);

            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            YesCallback = () =>
            {
                if (enoughBalance)
                {
                    onPaymentSucceed.Invoke();
                }
                else
                {
                    Close(true);
                    ShowLackPaymentCrystal(cost);
                }
            };

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }

        public void ShowCheckPaymentApPotion(BigInteger cost, System.Action onPaymentSucceed)
        {
            SetPopupType(PopupType.PaymentCheck);
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(CostType.ActionPoint);

            var inventory = States.Instance.CurrentAvatarState.inventory;
            var apPotionCount = inventory.GetUsableItemCount(CostType.ApPotion, Game.Game.instance.Agent.BlockIndex);
            var enoughBalance = apPotionCount >= 1;
            var content = L10nManager.Localize("UI_CHECK_ACTION_POINT", apPotionCount);

            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            YesCallback = () =>
            {
                if (enoughBalance)
                {
                    Close(true);
                    onPaymentSucceed?.Invoke();
                }
                else
                {
                    Close(true);
                    ShowLackApPotion(1);
                }
            };
            
            SetContent(popupTitle, content, yes, no, false);
            Show(popupTitle, content, yes, no, false);
        }
#endregion PaymentCheckAction

#region ShopHelper
        public static string GetLackNCGContentString(bool isStaking = false)
        {
            var canAttract = CanAttractShop();
            if (!canAttract)
            {
                return L10nManager.Localize("UI_LACK_NCG_PC");
            }
            
            return isStaking ?
                L10nManager.Localize("UI_LACK_NCG_STAKING") :
                L10nManager.Localize("UI_LACK_NCG");
        }
        
        public static string GetLackHourglassContentString(bool canBuyShop)
        {
            return L10nManager.Localize(canBuyShop ? "UI_LACK_HOURGLASS_SHOP" : "UI_LACK_HOURGLASS_MONSTER_COLLECTION");
        }
        
        public static string GetLackApPotionContentString()
        {
            var canAttract = CanAttractShop();
            return L10nManager.Localize(!canAttract ? "UI_LACK_AP_PORTION_PC" : "UI_LACK_AP_PORTION");
        }
        
        // TODO: 공용으로 뺄 수 없을지 확인.
        public static bool CanAttractShop()
        {
#if UNITY_ANDROID || UNITY_IOS
            return false;
#endif
            return !Game.LiveAsset.GameConfig.IsKoreanBuild;
        }

        public static bool CanAttractMobileShop(int itemId)
        {
#if UNITY_ANDROID || UNITY_IOS
            var iapStoreManager = Game.Game.instance.IAPStoreManager;
            return iapStoreManager.TryGetCategoryName(itemId, out _);
#endif
            return false;
        }
#endregion ShopHelper
        
#region Attract
        private void AttractToMonsterCollection()
        {
            // 기능 충돌 방지를 위해 StakingPopup실행 전 다른 UI를 닫음
            CloseWithOtherWidgets();
            Game.Event.OnRoomEnter.Invoke(true);
            Find<StakingPopup>().Show();
        }

        private void AttractToAdventureBoss()
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                // K빌드인 경우 이동하지 않음
                NcDebug.LogWarning("Korean build is not supported.");
                return;
            }

            CloseWithOtherWidgets();
            Find<WorldMap>().Show();

            var currState = Game.Game.instance.AdventureBossData.CurrentState.Value;
            if (currState == AdventureBossData.AdventureBossSeasonState.Progress)
            {
                WorldMapAdventureBoss.OnClickOpenAdventureBoss();
            }
        }
        
        private void AttractGrind()
        {
            Find<Menu>().Close();
            Find<WorldMap>().Close();
            Find<StageInformation>().Close();
            Find<BattlePreparation>().Close();
            Find<Craft>().Close(true);
            Find<Grind>().Show();
        }

        private void AttractShop()
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                // K빌드인 경우 이동하지 않음
                NcDebug.LogWarning("Korean build is not supported.");
                Game.Event.OnRoomEnter.Invoke(true);
                return;
            }

            AttractShopAsync().Forget();
        }

        private async UniTask AttractShopAsync()
        {
            CloseWithOtherWidgets();      
            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Shop);      
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            await Find<ShopSell>().ShowAsync();
            Find<LoadingScreen>().Close();
        }

        private void AttractToSummon()
        {
            CloseWithOtherWidgets();
            Find<Summon>().Show();
        }

        private async UniTask AttractMobileShopAsync(int id)
        {
#if !(UNITY_ANDROID || UNITY_IOS)
            return;
#endif
            var iapStoreManager = Game.Game.instance.IAPStoreManager;
            if (iapStoreManager.TryGetCategoryName(id, out var categoryName))
            {
                CloseWithOtherWidgets();      
                Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Shop);      
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                await Find<MobileShop>().ShowAsTab(categoryName);
                Find<LoadingScreen>().Close();
            }
        }
#endregion Attract

#region General
        private void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL", bool localize = true)
        {
            SetContent(title, content, labelYes, labelNo, localize);

            if (!gameObject.activeSelf)
            {
                Show();
            }
        }

        private void SetContent(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            var titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                {
                    titleText.text = L10nManager.Localize(title);
                }

                contentText.text = L10nManager.Localize(content);
                buttonYes.Text = L10nManager.Localize(labelYes);
                buttonNo.Text = L10nManager.Localize(labelNo);
            }
            else
            {
                titleText.text = title;
                contentText.text = content;
                buttonYes.Text = labelYes;
                buttonNo.Text = labelNo;
            }

            titleText.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
        }
#endregion General

// TODO: 별도 파일(PaymentHelper등)으로 분리?
#region Helper
#region DustHelper
        public static string GetLackDustContentString(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                    return L10nManager.Localize("UI_LACK_SILVER_DUST");
                case CostType.GoldDust:
                    return L10nManager.Localize("UI_LACK_GOLD_DUST");
                case CostType.RubyDust:
                    return L10nManager.Localize("UI_LACK_RUBY_DUST");
                case CostType.EmeraldDust:
                    return L10nManager.Localize(CanAttractDust(costType) ?
                        "UI_LACK_EMERALD_DUST" :
                        "UI_LACK_EMERALD_DUST_KR");
            }

            return string.Empty;
        }

        // TODO: 모든 CostType에 대해 처리 가능하면 메서드명 일반적이게 변경하고 수정
        private string GetDustAttractString(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    return L10nManager.Localize("UI_MENU_MONSTER_COLLECTION");
                case CostType.EmeraldDust:
                    return L10nManager.Localize("WORLD_NAME_ADVENTURE_BOSS");
            }

            return string.Empty;
        }

        private static bool CanAttractDust(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    return true;
                case CostType.EmeraldDust:
                    return !Game.LiveAsset.GameConfig.IsKoreanBuild;
            }

            return false;
        }

        private void AttractDust(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    AttractToMonsterCollection();
                    return;
                case CostType.EmeraldDust:
                    AttractToAdventureBoss();
                    return;
            }

            NcDebug.LogWarning($"[{nameof(PaymentPopup)}] AttractDust: Invalid costType.");
        }
#endregion DustHelper
        
#region RuneStoneHelper
        /// <summary>
        /// 해당 runeStone을 획득할 AttractAction이 있는지 확인합니다.
        /// </summary>
        /// <param name="runeStone">NoneAction인지 확인할 runeStone의 fav 값</param>
        /// <returns>PC Shop에서 거래 가능하지만, 모바일인 경우 false</returns>
        private bool HasAttractActionRuneStone(FungibleAssetValue runeStone)
        {
            if (runeStone.IsTradable())
            {
                // 에디터 안드로이드, IOS 테스트를 위해 RUN_ON_MOBILE을 사용하지 않음
#if UNITY_ANDROID || UNITY_IOS
                return false;
#endif
            }

            return true;
        }

        private string GetRuneStoneContent(FungibleAssetValue runeStone)
        {
            var hasAttract = HasAttractActionRuneStone(runeStone);
            var runeName = runeStone.GetLocalizedName();
            if (runeStone.IsTradable())
            {
                return hasAttract ? 
                    L10nManager.Localize("UI_LACK_TRADEABLE_RUNESTONE_PC", runeName) : 
                    L10nManager.Localize("UI_LACK_TRADEABLE_RUNESTONE_MOBILE");
            }
            return L10nManager.Localize("UI_LACK_UNTRADEABLE_RUNESTONE", runeName);
        }
        
        private Sprite GetRuneStoneSprite(FungibleAssetValue runeStone)
        {
            return RuneFrontHelper.TryGetRuneStoneIcon(runeStone.Currency.Ticker, out var icon) ? icon : null;
        }

        private string GetRuneStoneAttractMessage(FungibleAssetValue runeStone)
        {
            return L10nManager.Localize(runeStone.IsTradable() ? "UI_SHOP" : "UI_SUMMON");
        }
        
        private System.Action AttractRuneStone(FungibleAssetValue runeStone)
        {
            if (runeStone.IsTradable())
            {
                return AttractShop;
            }

            return AttractToSummon;
        }
#endregion RuneStoneHelper
#endregion Helper

        private void Yes()
        {
            base.Close();
            YesCallback?.Invoke();
        }

        private void Cancel()
        {
            base.Close();
        }
    }
}
