﻿using System;

namespace UniGit.Utils
{
	public static class EnumExtensions
	{
		public static bool HasFlag(this Enum value, Enum flag)
		{
			if(value.GetType() != flag.GetType()) throw new ArgumentException("Enums must be of the same type " + value.GetType() + " " + flag.GetType(),"value");

			var valueInt = Convert.ToInt32(value);
			var flagInt = Convert.ToInt32(flag);
			return (valueInt & flagInt) == flagInt;
		}
	}
}
