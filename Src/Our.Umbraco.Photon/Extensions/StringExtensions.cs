using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.Photon.Extensions
{
	internal static class StringExtensions
	{
		internal static bool DetectIsJson(this string input)
		{
			input = input.Trim();
			return (input.StartsWith("{") && input.EndsWith("}"))
				   || (input.StartsWith("[") && input.EndsWith("]"));
		}
	}
}
