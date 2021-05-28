﻿using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4A
{
    [TraitInterface]
    interface IAddress
    {
        string Address { get; set; }
        string City { get; set; }
        string State { get; set; }
        string ZipCode { get; set; }

        string GetAddress();
    }
}
