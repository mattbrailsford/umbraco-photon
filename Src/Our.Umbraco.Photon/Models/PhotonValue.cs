using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Our.Umbraco.Photon.Models
{
	public class PhotonValue
	{
		[JsonProperty("imageId")]
		public int ImageId { get; set; }

        // Only ever used in Razor views, so can be considered readonly
        [JsonIgnore]
        public IPublishedContent Image { get; internal set; }

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
