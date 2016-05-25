using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Our.Umbraco.Photon.Models;
using Umbraco.Core;

namespace Our.Umbraco.Photon.Extensions
{
	internal static class PhotonValueExtensions
	{
		public static bool HasFile(this PhotonValue value)
		{
			return value != null && value.ImageId > 0;
		}
	}
}
