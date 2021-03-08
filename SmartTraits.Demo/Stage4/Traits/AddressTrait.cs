using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4
{
    [Trait]
    abstract partial class AddressTrait: IAddress, IAddrLabel
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }

        public string GetAddress()
        {
            return $"{GetLabel()} {Address} {City} {State} {ZipCode}";
        }
    }
}
