using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Our.Umbraco.Photon.Helpers
{
	internal static class PhotonHelper
	{
		public static string GetMetaDataDocType(int dtdId)
		{
			var preValueCollection = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtdId);
			return GetMetaDataDocType(preValueCollection);
		}

		public static string GetMetaDataDocType(PreValueCollection preValueCollection)
		{
			var preValueDict = preValueCollection.PreValuesAsDictionary.ToDictionary(x => x.Key, x => x.Value.Value);
			return preValueDict.ContainsKey("metaDataDocType")
				? preValueDict["metaDataDocType"]
				: "";
		}
	}
}
