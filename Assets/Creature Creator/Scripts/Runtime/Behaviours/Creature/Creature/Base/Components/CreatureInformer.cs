using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(CreatureConstructor))]
    [RequireComponent(typeof(CreaturePhotographer))]
    [RequireComponent(typeof(CreatureHealth))]
    [RequireComponent(typeof(CreatureHunger))]
    [RequireComponent(typeof(CreatureAge))]
    public class CreatureInformer : MonoBehaviour
    {
        #region Fields
        [SerializeField] private CreatureInformationMenu informationMenu;

        [Header("Debug")]
        [SerializeField, ReadOnly] private CreatureInformation information;
        #endregion

        #region Properties
        public CreaturePhotographer Photographer { get; private set; }
        public CreatureConstructor Constructor { get; private set; }
        public CreatureHealth Health { get; private set; }
        public CreatureHunger Hunger { get; private set; }
        public CreatureAge Age { get; private set; }

        public CreatureInformation Information => information;
        #endregion

        #region Methods
        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            Constructor = GetComponent<CreatureConstructor>();
            Photographer = GetComponent<CreaturePhotographer>();
            Health = GetComponent<CreatureHealth>();
            Hunger = GetComponent<CreatureHunger>();
            Age = GetComponent<CreatureAge>();
        }

        public void Setup(CreatureInformationMenu menu)
        {
            informationMenu = menu;
            informationMenu.Setup(information);

            Health.OnHealthChanged += InformHealth;
            Hunger.OnHungerChanged += InformHunger;
            Age.OnAgeChanged += InformAge;

            Constructor.OnConstructCreature += Capture;
        }

        private void InformHealth(float health)
        {
            Information.Health = Health.HealthPercentage;
        }
        private void InformHunger(float hunger)
        {
            Information.Hunger = hunger;
        }
        private void InformAge(int age)
        {
            Information.Age = age;
        }

        public void Capture()
        {
            Information.Reset();

            Photographer.TakePhoto(128, (Texture2D photo) =>
            {
                Information.Photo = photo;
            });
            Information.Name = Constructor.Data.Name;
        }
        #endregion
    }
}