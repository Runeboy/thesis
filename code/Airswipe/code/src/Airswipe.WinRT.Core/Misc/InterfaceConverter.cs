using System;

namespace Airswipe.WinRT.Core.Misc
{
    public class InterfaceConverter<TInterface, TConcrete> : Newtonsoft.Json.Converters.CustomCreationConverter<TInterface>
    where TConcrete : TInterface, new()
    {
        public override TInterface Create(Type objectType)
        {
            return new TConcrete();
        }
    }
}
