using System.IO;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Configuration;

namespace Our.Umbraco.Photon.Helpers
{
	internal static class UploadFileTypeHelper
	{
		internal static bool ValidateFileExtension(string fileName)
		{
			if (fileName.IndexOf('.') <= 0) return false;
			var extension = Path.GetExtension(fileName).TrimStart(".");
			return UmbracoConfig.For.UmbracoSettings().Content.DisallowedUploadFiles.Any(x => StringExtensions.InvariantEquals(x, extension)) == false;
		}
	}
}
