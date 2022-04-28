// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class AnimalAI<T> : StateMachine<T>
    {
        #region Fields
        [SerializeField] private TextAsset data;

        protected CreatureNonPlayer creature;
        #endregion

        #region Methods
        public override void Awake()
        {
            base.Awake();
            creature = GetComponent<CreatureNonPlayer>();
        }
        public virtual void Start()
        {
            creature.Setup();
            creature.Constructor.Construct(JsonUtility.FromJson<CreatureData>(data.text));
            creature.Animator.IsAnimated = true;
        }
        #endregion
    }
}