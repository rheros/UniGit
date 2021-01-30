﻿//#define TYPE_CHECK
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;


namespace UniGit.Utils
{
	internal static class FlagHelper
	{
		private static void CheckIsEnum<T>(bool withFlags)
		{
#if TYPE_CHECK
			if (!typeof(T).IsEnum)
				throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));
			if (withFlags && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
				throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));
#endif
		}

		public static bool AreNotSet<T>(this T value, params T[] flags) where T : struct, IConvertible
		{
			return flags.All(f => !IsFlagSet(value, f));
		}

		public static bool IsFlagSet<T>(this T value, T flag) where T : struct, IConvertible
		{
			CheckIsEnum<T>(true);
			var lValue = value.ToInt32(CultureInfo.InvariantCulture);
			var lFlag = flag.ToInt32(CultureInfo.InvariantCulture);
			return (lValue & lFlag) != 0;
		}

		public static T SetFlags<T>(this T value, T flags, bool on) where T : struct, IConvertible
		{
			CheckIsEnum<T>(true);
			var lValue = value.ToInt32(CultureInfo.InvariantCulture);
			var lFlag = flags.ToInt32(CultureInfo.InvariantCulture);
			if (on)
			{
				lValue |= lFlag;
			}
			else
			{
				lValue &= (~lFlag);
			}
			return (T)Enum.ToObject(typeof(T), lValue);
		}

		public static T SetFlags<T>(this T value, T flags) where T : struct, IConvertible
		{
			return value.SetFlags(flags, true);
		}

		public static T ClearFlags<T>(this T value, T flags) where T : struct, IConvertible
		{
			return value.SetFlags(flags, false);
		}

		public static T CombineFlags<T>(this IEnumerable<T> flags) where T : struct, IConvertible
		{
			CheckIsEnum<T>(true);
			var lValue = 0;
			foreach (var flag in flags)
			{
				var lFlag = Convert.ToInt32(flag);
				lValue |= lFlag;
			}
			return (T)Enum.ToObject(typeof(T), lValue);
		}

		public static string GetDescription<T>(this T value) where T : struct
		{
			CheckIsEnum<T>(false);
			var name = Enum.GetName(typeof(T), value);
            if (name == null) return null;
            var field = typeof(T).GetField(name);
            if (field == null) return name;
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
            {
                return attr.Description;
            }
            return name;
		}
	}
}