using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Granden.AvatarSDK
{
	/**************************
	 * 常數
	 * ************************/
	public static class AvatarDefine
	{
#if DEBUG
		public const string BaseUrl = "https://greatwig-backend-api-greatwig-backend-api-staging.azurewebsites.net";
#else
		public const string BaseUrl = "https://greatwig-backend-api.azurewebsites.net";		
#endif
	}

	public static class ErrorDefine
	{
		public const string EXTERNAL_CAMERA_REQUIRED	= "EXTERNAL_CAMERA_REQUIRED";
		public const string CAMERA_RAWIMAGE_NULL		= "CAMERA_RAWIMAGE_NULL";
		public const string AVATARSDK_INITIAL_FAILED	= "AVATARSDK_INITIAL_FAILED";
		public const string LICENSE_INVALID				= "LICENSE_INVALID";
		public const string GENERATED_HEAD_FAILED		= "GENERATED_HEAD_FAILED";
		public const string GENERATED_HAIR_FAILED		= "GENERATED_HAIR_FAILED";
		public const string GET_PARAMETERS_FAILED		= "GET_PARAMETERS_FAILED";
		public const string BEAUTIFUL_API_FAILED		= "BEAUTIFUL_API_FAILED";
	}
}