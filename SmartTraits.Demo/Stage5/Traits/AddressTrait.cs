using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage5
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

        // this trait is not in the strict mode (i.e. not a member of IAddress) and it is ok to have extra method/properties/fields in the trait to be added to a destination file
        public void AddrExtraMethod()
        {

        }
    }
}
