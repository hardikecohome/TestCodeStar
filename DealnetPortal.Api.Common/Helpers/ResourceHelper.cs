using System;
using System.Resources;

namespace DealnetPortal.Api.Common.Helpers
{
    public static class ResourceHelper
    {
        private static readonly Lazy<ResourceManager> GlobalResourceManager = new Lazy<ResourceManager>(() => new ResourceManager(typeof(Resources.Resources)));
         
        public static string GetGlobalStringResource(string resourceName)
        {
            if (resourceName == null) return null;
            return GlobalResourceManager.Value.GetString(resourceName);
        }
    }
}
