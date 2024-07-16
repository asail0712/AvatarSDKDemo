/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using UnityEngine;
using System.Linq;

namespace ItSeez3D.AvatarSdk.Core
{
	/// <summary>
	/// Open html documentation in system's default Web browser.
	/// </summary>
	public static class DocumentationHelper
	{
		public static string DocumentationBaseUrl(Flavour flavour = Flavour.FLAVOUR_UNKNOWN) {
			switch(flavour)
			{
				case Flavour.CLOUD:
				case Flavour.CLOUD_UMA:
					return string.Format("https://docs.avatarsdk.com/unity-plugin/{0}", flavour.GetTraits().DocumentationVersion);

				case Flavour.LOCAL_COMPUTE:
				case Flavour.LOCAL_COMPUTE_UMA:
					return string.Format("https://docs.avatarsdk.com/unity-plugin-local-compute/{0}", flavour.GetTraits().DocumentationVersion);

				default:
					return string.Format("https://docs.avatarsdk.com/unity-plugin/{0}", CoreTools.DetectFlavour().FirstOrDefault().GetTraits().DocumentationVersion);
			}
			
		}

		public static void OpenDocumentationInBrowser(string page, Flavour flavour)
		{
			Application.OpenURL(string.Format("{0}/{1}", DocumentationBaseUrl(flavour), page));
		}
	}
}
