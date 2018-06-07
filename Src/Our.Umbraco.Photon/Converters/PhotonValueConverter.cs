using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Photon.Extensions;
using Our.Umbraco.Photon.Helpers;
using Our.Umbraco.Photon.Models;
using Our.Umbraco.Photon.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Our.Umbraco.Photon.Converters
{
	[PropertyValueCache(PropertyCacheValue.All, PropertyCacheLevel.Content)]
	public class PhotonValueConverter : PropertyValueConverterBase
	{
		private UmbracoHelper _umbraco;
		internal UmbracoHelper Umbraco
		{
			get { return _umbraco ?? (_umbraco = new UmbracoHelper(UmbracoContext.Current)); }
		}

		public override bool IsConverter(PublishedPropertyType propertyType)
		{
			return propertyType.PropertyEditorAlias.InvariantEquals(PhotonPropertyEditor.PropertyEditorAlias);
		}

		public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
		{
			try
			{
				if (source != null && !source.ToString().IsNullOrWhiteSpace() && source.ToString() != "{}")
				{
					var value = JsonConvert.DeserializeObject<PhotonValue>(source.ToString());

				    if (value.ImageId > 0)
				    {
                        // Get the image content
				        value.Image = UmbracoContext.Current.MediaCache.GetById(value.ImageId);

				        if (value.Tags != null && value.Tags.Count > 0)
				        {
				            // Get the meta data doc type
				            var metaDataDocTypeAlias = PhotonHelper.GetMetaDataDocType(propertyType.DataTypeId);
				            var metaDataDocType = PublishedContentType.Get(PublishedItemType.Content, metaDataDocTypeAlias);

				            // Loop tags and covert meta data
				            foreach (var tag in value.Tags)
				            {
				                tag.MetaData = ConvertDataToSource_DocType(propertyType, metaDataDocType, tag.RawMetaData, preview);
				            }
				        }
				    }

				    return value;
				}
			}
			catch (Exception e)
			{
				LogHelper.Error<PhotonValueConverter>("Error converting value", e);
			}

			return null;
		}

		protected IPublishedContent ConvertDataToSource_DocType(PublishedPropertyType propertyType,
			PublishedContentType contentType, object value, bool preview)
		{
			var properties = new List<IPublishedProperty>();

			// Convert all the properties
			var propValues = ((JObject)value).ToObject<Dictionary<string, object>>();
			foreach (var jProp in propValues)
			{
				var propType = contentType.GetPropertyType(jProp.Key);
				if (propType != null)
				{
					var nestedPropType = propType.ExecuteMethod<PublishedPropertyType>("Nested",
						propertyType);
					var prop = typeof(PublishedProperty).ExecuteMethod<IPublishedProperty>("GetDetached",
						nestedPropType,
						(jProp.Value == null ? "" : jProp.Value.ToString()) as object, 
						preview);
					properties.Add(prop);
				}
			}

			// Parse out the name manually
			//object nameObj = null;
			//if (propValues.TryGetValue("name", out nameObj))
			//{
			//	// Do nothing, we just want to parse out the name if we can
			//}

			return new DetachedPublishedContent(contentType, properties.ToArray());
		}
	}
}
