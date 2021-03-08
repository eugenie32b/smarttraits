using System;

namespace SmartTraitsDefs
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class AddTraitAttribute : System.Attribute
    {
        private Type _traitType;

        public AddTraitAttribute(Type traitType)
        {
            _traitType = traitType;
        }
    }
}
