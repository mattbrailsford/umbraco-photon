using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Umbraco.Core.Models;

namespace Our.Umbraco.Photon.Models
{
	public class PhotonTag
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("x")]
		public decimal X { get; set; }

		[JsonProperty("y")]
		public decimal Y { get; set; }

		[JsonProperty("width")]
		public decimal Width { get; set; }

		[JsonProperty("height")]
		public decimal Height { get; set; }

		[JsonProperty("metaData")]
		public object RawMetaData { get; set; }

		// Only ever used in Razor views, so can be considered readonly
		[JsonIgnore]
		public IPublishedContent MetaData { get; internal set; }
	}
}
