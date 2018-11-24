namespace Airswipe.WinRT.Core.Data
{
    public class JsonObject
    {
        public string ToJson()
        {
            return StringExpert.ToJson(this);
        }
    }
}
