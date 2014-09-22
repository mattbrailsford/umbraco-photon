using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Umbraco.Core.Logging;

namespace Our.Umbraco.Photon.Models
{
	public class PhotonValue
	{
		[JsonProperty("src")]
		public string Src { get; set; }

		[JsonProperty("tags")]
		public ReadOnlyCollection<PhotonTag> Tags { get; set; }

		internal static PhotonValue Parse(string json)
		{
			try
			{
				return JsonConvert.DeserializeObject<PhotonValue>(json);
			}
			catch (Exception ex)
			{
				LogHelper.WarnWithException<PhotonValue>("Could not parse json to a PhotonValue", ex);
				return null;
			}
		}
	}
}
