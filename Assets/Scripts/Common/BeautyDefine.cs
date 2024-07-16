using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Granden.AvatarSDK
{
	/**************************
	 * 常數
	 * ************************/
	public static class MeituDefine
	{
		public const string BeautyLivePhotoUrl	= "https://openapi.mtlab.meitu.com/v1/livephoto";
		public const string OldBeautyRequestUrl = "https://openapi.mtlab.meitu.com/v1/beauty_shape";
	}

	/**************************
	 * Request
	 * ************************/
	public class LivePhotoRequest
	{
		public LivePhotoRequestMediaInfo[] media_info_list	= new LivePhotoRequestMediaInfo[1];
		public LivePhotoRequestParameter parameter			= new LivePhotoRequestParameter();
		public LivePhotoRequestExtra extra					= new LivePhotoRequestExtra();
	}

	public class LivePhotoRequestMediaInfo
	{
		public string media_data							= "";
		public LivePhotoRequestMediaProfiles media_profiles = new LivePhotoRequestMediaProfiles();
	}

	public class LivePhotoRequestParameter
	{
		public LivePhotoRequestAIOptimize ai_optimize	= new LivePhotoRequestAIOptimize();
		public LivePhotoRequestEnhance enhance			= new LivePhotoRequestEnhance();
		public LivePhotoRequestFace face				= new LivePhotoRequestFace();
	}

	public class LivePhotoRequestAIOptimize
	{
		public int boy_face_beauty_alpha	= 50;
		public int girl_face_beauty_alpha	= 100;
		public int child_face_beauty_alpha	= 25;
		public int man_ai_shrink_head		= 0;
		public int girl_ai_shrink_head		= 0;
		public int child_ai_shrink_head		= 0;
	}

	public class LivePhotoRequestEnhance
	{
		public int radio = 0;
	}

	public class LivePhotoRequestFace
	{
		public int face_forehead_boy	= -40;
		public int face_forehead_girl	= -40;
		public int face_forehead_child	= -40;
		public int narrow_face_boy		= -30;
		public int narrow_face_girl		= -30;
		public int narrow_face_child	= -30;
		public int jaw_trans_boy		= 0;
		public int jaw_trans_girl		= 0;
		public int jaw_trans_child		= 0;
		public int face_trans_boy		= -100;
		public int face_trans_girl		= -100;
		public int face_trans_child		= -30;
	}

	public class LivePhotoRequestMediaProfiles
	{
		public string media_data_type = "jpg";
	}

	public class LivePhotoRequestExtra
	{
		// nothing to do
	}

	[System.Serializable]
	public class Parameter
	{
		public int makeupId;
		public int makeupAlpha;
		public int beautyAlpha;
		public string rsp_media_type;

		public Parameter(int id, int alpha1, int alpha2, string type)
		{
			makeupId		= id;
			makeupAlpha		= alpha1;
			beautyAlpha		= alpha2;
			rsp_media_type	= type;
		}
	}

	[System.Serializable]
	public class JSDNData
	{
		public Parameter parameter;
		public Extra extra;
		public MediaInfo[] media_info_list;
	}

	public class MediaInfo
	{
		public string media_data			= "";
		public MediaExtra media_extra		= new MediaExtra();
		public MediaProfiles media_profiles = new MediaProfiles();
	}
	
	public class MediaProfiles
	{
		public string media_data_type		= "jpg";
		public string media_data_describe	= "src";
	}

	[System.Serializable]
	public class Extra
	{
		// nothing to do
	}

	public class MediaExtra
	{
		// nothing to do
	}

	/**************************
	 * Response
	 * ************************/
	public class LivePhotoResponseSuccess
	{
		public LivePhotoResponseParam parameter;
		public LivePhotoResponseMediaInfo[] media_info_list;
	}
	public class LivePhotoResponseMediaInfo
	{
		public LivePhotoResponseMediaProfiles media_profiles = new LivePhotoResponseMediaProfiles();
		public string media_data;
	}

	public class LivePhotoResponseMediaProfiles
	{
		public string media_data_type;
	}

	public class LivePhotoResponseParam
	{
		// nothing to do		
	}
}
