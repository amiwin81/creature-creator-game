﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator.Abilities
{
    [CreateAssetMenu(menuName = "Creature Creator/Ability/Drop")]
    public class Drop : Ability
    {
        public override void OnPerform()
        {
            Player.Instance.PickUp.DropAll();
        }
    }
}