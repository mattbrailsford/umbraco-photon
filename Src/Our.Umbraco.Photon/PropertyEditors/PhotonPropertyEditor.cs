using System.Collections.Generic;
using System.Linq;
using ClientDependency.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Photon.Helpers;
using Our.Umbraco.Photon.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;

namespace Our.Umbraco.Photon.PropertyEditors
{
	[PropertyEditorAsset(ClientDependencyType.Css, "/App_Plugins/Photon/Css/imgareaselect-photon.css")]
	[PropertyEditorAsset(ClientDependencyType.Css, "/App_Plugins/Photon/Css/photon.css")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Photon/Js/jquery.imgareaselect.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Photon/Js/photon.js")]
	[PropertyEditor(PropertyEditorAlias, "Photon", "/App_Plugins/Photon/Views/photon.html", HideLabel = false, ValueType = "JSON")]
	public class PhotonPropertyEditor : PropertyEditor
	{
		public const string PropertyEditorAlias = "Our.Umbraco.Photon";

		private IDictionary<string, object> _defaultPreValues;
		public override IDictionary<string, object> DefaultPreValues
		{
			get { return _defaultPreValues; }
			set { _defaultPreValues = value; }
		}

		public PhotonPropertyEditor()
		{
			_defaultPreValues = new Dictionary<string, object>
			{
				{"backgroundColor", "#F8F8F8"},
				{"imageWidth", "500"}
			};
		}

		#region Pre Value Editor

		protected override PreValueEditor CreatePreValueEditor()
		{
			return new PhotonPreValueEditor();
		}

		internal class PhotonPreValueEditor : PreValueEditor
		{
			[PreValueField("metaDataDocType", "Meta Data DocType", "textstring", Description = "Enter the doctype alias of the doctype to use for your image tag meta data.")]
			public string MetaDataDocType { get; set; }

			[PreValueField("backgroundColor", "Background Color", "textstring", Description = "Enter a HEX background color to use behind the preview image (handy if your preview image is a transparent PNG).")]
			public int BackgroundColor { get; set; }

			[PreValueField("imageWidth", "Image Width", "number", Description = "Enter the width to preview the image at.")]
			public int ImageWidth { get; set; }

			[PreValueField("hideLabel", "Hide Label", "boolean", Description = "Select whether to hide the property label or not.")]
			public bool HideLabel { get; set; }
		}

		#endregion 

		#region Value Editor

		protected override PropertyValueEditor CreateValueEditor()
		{
			return new PhotonPropertyValueEditor(base.CreateValueEditor());
		}

		internal class PhotonPropertyValueEditor : PropertyValueEditorWrapper
		{
			public PhotonPropertyValueEditor(PropertyValueEditor wrapped)
				: base(wrapped)
			{ }

			public override void ConfigureForDisplay(PreValueCollection preValues)
			{
				base.ConfigureForDisplay(preValues);

				var asDictionary = preValues.FormatAsDictionary();
				if (asDictionary.ContainsKey("hideLabel"))
				{
					var boolAttempt = asDictionary["hideLabel"].Value.TryConvertTo<bool>();
					if (boolAttempt.Success)
					{
						HideLabel = boolAttempt.Result;
					}
				}
			}

			#region DB to String

			public override string ConvertDbToString(Property property, PropertyType propertyType,
				IDataTypeService dataTypeService)
			{
				// Make sure we have a value
				if (property.Value == null || string.IsNullOrWhiteSpace(property.Value.ToString()))
					return string.Empty;

				// Parse to photon value for ease
				var value = JsonConvert.DeserializeObject<PhotonValue>(property.Value.ToString());
				if (value == null)
					return string.Empty;

				// Loop tags and covert meta data
				if (value.Tags != null && value.Tags.Count > 0)
				{
					// Get the meta data doc type
					var metaDataDocTypeAlias = PhotonHelper.GetMetaDataDocType(propertyType.DataTypeDefinitionId);
					var metaDataDocType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(metaDataDocTypeAlias);

					foreach (var tag in value.Tags)
					{
						tag.RawMetaData = ConvertDbToString_DocType(metaDataDocType, tag.RawMetaData);
					}
				}

				// Update the value on the property
				property.Value = JsonConvert.SerializeObject(value);

				// Pass the call down
				return base.ConvertDbToString(property, propertyType, dataTypeService);
			}

			protected object ConvertDbToString_DocType(IContentType contentType, object value)
			{
				// Loop through properties
				var propValues = ((JObject)value);
				var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
				if (contentType != null && contentType.PropertyTypes != null)
				{
					foreach (var propKey in propValueKeys)
					{
						// Lookup the property type on the content type
						var propType = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propKey);

						if (propType == null)
						{
							// Property missing so just delete the value
							propValues[propKey] = null;
						}
						else
						{
							// Create a fake property using the property abd stored value
							var prop = new Property(propType, propValues[propKey] == null ? null : propValues[propKey].ToString());

							// Lookup the property editor
							var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

							// Get the editor to do it's conversion, and store it back
							propValues[propKey] = propEditor.ValueEditor.ConvertDbToString(prop, propType,
								ApplicationContext.Current.Services.DataTypeService);
						}
					}
				}

				// Serialize the dictionary back
				return propValues;
			}

			#endregion	

			#region DB to Editor

			public override object ConvertDbToEditor(Property property, PropertyType propertyType, 
				IDataTypeService dataTypeService)
			{
				// Make sure we have a value
				if (property.Value == null || string.IsNullOrWhiteSpace(property.Value.ToString()))
					return string.Empty;

				// Parse to photon value for ease
				var value = JsonConvert.DeserializeObject<PhotonValue>(property.Value.ToString());
				if (value == null)
					return string.Empty;

				// Loop tags and covert meta data
				if (value.Tags != null && value.Tags.Count > 0)
				{
					// Get the meta data doc type
					var metaDataDocTypeAlias = PhotonHelper.GetMetaDataDocType(propertyType.DataTypeDefinitionId);
					var metaDataDocType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(metaDataDocTypeAlias);

					foreach (var tag in value.Tags)
					{
						tag.RawMetaData = ConvertDbToEditor_DocType(metaDataDocType, tag.RawMetaData);
					}
				}

				// We serialize back down as we want the editor to handle
				// the data as a generic object type, not our specific classes
				property.Value = JsonConvert.SerializeObject(value);

				return base.ConvertDbToEditor(property, propertyType, dataTypeService);
			}

			protected object ConvertDbToEditor_DocType(IContentType contentType, object value)
			{
				// Loop through properties
				var propValues = value as JObject;
				if (propValues != null)
				{
					if (contentType != null && contentType.PropertyTypes != null)
					{
						var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
						foreach (var propKey in propValueKeys)
						{
							// Lookup the property type on the content type
							var propType = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propKey);

							if (propType == null)
							{
								// Property missing so just remove the value
								propValues[propKey] = null;
							}
							else
							{
								// Create a fake property using the property abd stored value
								var prop = new Property(propType, propValues[propKey] == null ? null : propValues[propKey].ToString());

								// Lookup the property editor
								var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

								// Get the editor to do it's conversion
								var newValue = propEditor.ValueEditor.ConvertDbToEditor(prop, propType,
									ApplicationContext.Current.Services.DataTypeService);

								// Store the value back
								propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
							}
						}
					}
				}

				return propValues;
			}

			#endregion	

			#region Editor to DB

			public override object ConvertEditorToDb(ContentPropertyData editorValue, 
				object currentValue)
			{
				var newValue = new PhotonValue();

				//get the new src path
				if (editorValue.Value != null)
				{
					newValue = PhotonValue.Parse(editorValue.Value.ToString());

                    // Loop tags and covert meta data
                    if (newValue != null && newValue.Tags != null && newValue.Tags.Count > 0)
                    {
                        // Get the meta data doc type
                        var metaDataDocTypeAlias = PhotonHelper.GetMetaDataDocType(editorValue.PreValues);
                        var metaDataDocType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(metaDataDocTypeAlias);

                        foreach (var tag in newValue.Tags)
                        {
                            tag.RawMetaData = ConvertEditorToDb_DocType(metaDataDocType, tag.RawMetaData);
                        }
                    }

                    // Return json
                    return JsonConvert.SerializeObject(newValue);
                }

			    return null;
			}

			protected object ConvertEditorToDb_DocType(IContentType contentType, object value)
			{
				// Loop through doc type properties
				var propValues = ((JObject)value);
				var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
				if (contentType != null && contentType.PropertyTypes != null)
				{
					foreach (var propKey in propValueKeys)
					{
						// Fetch the current property type
						var propType = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propKey);

						if (propType == null)
						{
							// Property missing so just remove the value
							propValues[propKey] = null;
						}
						else
						{
							// Fetch the property types prevalue
							var propPreValues =
								ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(
									propType.DataTypeDefinitionId);

							// Lookup the property editor
							var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

							// Create a fake content property data object
							var contentPropData = new ContentPropertyData(
								propValues[propKey] == null ? null : propValues[propKey].ToString(), propPreValues,
								new Dictionary<string, object>());

							// Get the property editor to do it's conversion
							var newValue = propEditor.ValueEditor.ConvertEditorToDb(contentPropData, propValues[propKey]);

							// Store the value back
							propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
						}
					}
				}

				return propValues;
			}

			#endregion
		}

		#endregion

	}
}
