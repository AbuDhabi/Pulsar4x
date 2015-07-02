﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ComponentAbilitySD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public AbilityType Ability;
        public List<float> AbilityAmount;
        public List<float> CrewAmount;
        public List<float> WeightAmount;
        public AbilityType AffectsAbility;
        public List<float> AffectedAmount;
        public List<Guid> TechRequirements;
    }

    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ComponentSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public List<Guid> ComponentAbilitySDs;
    }
}