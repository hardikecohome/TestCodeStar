using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Attributes;

namespace DealnetPortal.Api.Common.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumDescription(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DisplayAttribute[])fi.GetCustomAttributes(
                typeof(DisplayAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                var attribute = attributes[0];
                if (attribute.ResourceType != null)
                {
                    return (string)attribute.ResourceType.GetProperty(attribute.Name,
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .GetValue(null, null);
                }
                return attribute.Name;
            }
            return value.ToString();
        }

        public static string GetPersistentEnumDescription(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (PersistentDescriptionAttribute[])fi.GetCustomAttributes(
                typeof(PersistentDescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            return value.ToString();
        }

        public static T ConvertTo<T>(this Enum value) where T : struct, IConvertible
        {
            return (T)Enum.Parse(typeof (T), value.ToString());
        }
    }
}
