using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ItSeez3D.AvatarSdk.Core;

using XPlan;

namespace Granden.AvatarSDK
{
	public class AvatarSceneSystem : SystemBase
    {
		[Header("功能")]
		[SerializeField][Tooltip("添加美顏")]
		private bool bNeedToBeauty = true;

		/*********************************
		 * 外型調整
		 * *******************************/
		[Header("外型")]
		[SerializeField][Tooltip("脖子調細")]
		private bool bAddHair = true;

		[SerializeField][Tooltip("脖子調細")]
		private bool bModifyNeck = true;

		[SerializeField][Tooltip("移除眼鏡")]
		private bool bRemoveGlass = false;

		[SerializeField][Tooltip("移除微笑")]
		private bool bRemoveSmile = false;

#if AVATARSDK_CLOUD
		[SerializeField][Tooltip("移除鬍子")]
		private bool bRemoveStubble = false;
#endif

		/*********************************
		 * 光線顏色調整
		 * *******************************/
		[Header("光線顏色")]
		[SerializeField][Tooltip("打光強化")]
		private bool bEnhanceLight = true;

		[SerializeField][Tooltip("眼皮陰影")]
		private bool bEyelidShadow = false;

		[SerializeField][Tooltip("加強眼部亮度")]
		private bool bEyeGlare = false;

		[SerializeField][Tooltip("眼白顏色調整")]
		private Color eyeScleraColor = Color.white;

		[SerializeField][Tooltip("髮色")]
		private Color hairColor = new Color(1, 1, 1, 0);

		[SerializeField][Tooltip("唇色")]
		private Color lipColor = new Color(1, 1, 1, 0);

		[SerializeField][Tooltip("齒色")]
		private Color teethColor = new Color(1, 1, 1, 0);

		/*********************************
		 * 紋理材質
		 * *******************************/
		[Header("紋理材質")]
		[SerializeField][Tooltip("頭部模型texture size")]
		private int headTextureWidth = 2048;

		[SerializeField][Tooltip("頭部模型texture size")]
		private int headTextureHight = 2048;

		[SerializeField][Tooltip("頭髮模型texture size")]
		private int hairTextureWidth = 1024;

		[SerializeField][Tooltip("頭髮模型texture size")]
		private int hairTextureHight = 1024;

		[SerializeField][Tooltip("頭髮模型面數")]
		private int hairFacesCount = 256;

		/*********************************
		 * 其他
		 * *******************************/
		[Header("其他")]
		[SerializeField][Tooltip("是否加上表情")]
		private bool bAddMotion = false;

		[SerializeField][Tooltip("允許頭髮變色")]
		private bool bHairRecolor = false;

		[SerializeField][Tooltip("允許皮膚變色")]
		private bool bSkinRecolor = false;

		[SerializeField][Tooltip("卡通化效果(0~1)")]
		private float cartoonishV1 = 0f;

		[SerializeField][Tooltip("強化模型特色，有效值範圍為(0~無限大)，建議低於5")]
		private float caricatureAmount = 1f;

		protected override void OnInitialGameObject()
		{

		}

		protected override void OnInitialHandler()
		{
			AvatarParam avatarParam = new AvatarParam()
			{ 
				bAddHair		= bAddHair,
				bAddMotion		= bAddMotion,
				bHairRecolor	= bHairRecolor,
				bSkinRecolor	= bSkinRecolor,
			};

			AvatarModification avatarModification = new AvatarModification(new ModificationsGroup()
			{
				/*********************************
				 * 外型調整
				 * *******************************/
				// 脖子調細
				bModifyNeck		= bModifyNeck,
				// 移除眼鏡
				bRemoveGlass	= bRemoveGlass,
				// 移除微笑
				bRemoveSmile	= bRemoveSmile,
#if AVATARSDK_CLOUD
				// 移除鬍子
				bRemoveStubble	= bRemoveStubble,
#endif //AVATARSDK_CLOUD

				/*********************************
				 * 光線顏色調整
				 * *******************************/
				// 打光強化
				bEnhanceLight	= bEnhanceLight,
				// 眼皮陰影
				bEyelidShadow	= bEyelidShadow,
				// 加強眼部亮度
				bEyeGlare		= bEyeGlare,
				// 眼白顏色調整
				eyeScleraColor  = eyeScleraColor,
				// 髮色
				hairColor		= hairColor,
				// 唇色
				lipColor		= lipColor,
				// 齒色
				teethColor		= teethColor,

				/*********************************
				 * 其他
				 * *******************************/
				// 與模型紋理看來略為卡通化
				cartoonishV1		= cartoonishV1,
				// 強化模型特色，有效值範圍為(0~無限大)，建議低於5
				caricatureAmount	= caricatureAmount,
				headTextureSize		= new TextureSize() { width = headTextureWidth, height = headTextureHight },
				hairTextureSize		= new TextureSize() { width = hairTextureWidth, height = hairTextureHight },
				hairFacesCount		= hairFacesCount
			});

			RegisterHandler(new StatusNotifyHandler());			
			RegisterHandler(new BeautyPicHandler(bNeedToBeauty));
			RegisterHandler(new AvatarSDKHandler(null, avatarParam, avatarModification, avatarModification));
			RegisterHandler(new HeadGeneratorHandler());
		}
	}
}
