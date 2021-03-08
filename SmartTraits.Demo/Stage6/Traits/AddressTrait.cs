using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage6
{
    [Trait]
    abstract partial class AddressTrait: IAddress
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }

        public string GetAddress()
        {
            return $"{Address} {City} {State} {ZipCode}";
        }

       
    }
}
