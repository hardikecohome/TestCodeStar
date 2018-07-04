using System.Threading;

namespace DealnetPortal.Api.Core.Helpers
{
    public enum CultureType
    {
        English,
        French
    }
    public static class CultureHelper
    {
        private const string DefaultCulture = "en";

        public static CultureType CurrentCultureType
        {
            get
            {
                switch (GetCurrentNeutralCulture())
                {
                    case "en":
                    default:
                        return CultureType.English;
                    case "fr":
                        return CultureType.French;
                }
            }
        }

        public static string FilterCulture(string name)
        {
            // make sure it's not null or empty
            return string.IsNullOrEmpty(name) ? GetDefaultCulture() : name;
        }

        public static string GetDefaultCulture()
        {
            return DefaultCulture;
        }
        public static string GetCurrentCulture()
        {
            return Thread.CurrentThread.CurrentCulture.Name;
        }
        public static string GetCurrentNeutralCulture()
        {
            return GetNeutralCulture(Thread.CurrentThread.CurrentCulture.Name);
        }
        public static string GetNeutralCulture(string name)
        {
            if (!name.Contains("-")) return name;
            return name.Split('-')[0]; // Read first part only. E.g. "en", "es"
        }
    }
}
