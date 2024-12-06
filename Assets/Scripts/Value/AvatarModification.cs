using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ItSeez3D.AvatarSdk.Core;

using XPlan.Interface;

namespace XPlanUtility.AvatarSDK
{
    // 參考資料
    // https://api.avatarsdk.com/?_gl=1*161w6jd*_ga*MTQ5NzM3NDM0Mi4xNzExMzQxMDA0*_ga_RK28V9S3X1*MTcxMzE0NzAwOS42LjEuMTcxMzE0NzAzNS4zNC4wLjA.&_ga=2.99690206.1343456017.1713147009-1497374342.1711341004#id3

    public class ModificationsGroup
	{
		/*********************************
		 * 外型調整
		 * *******************************/		 
		// 脖子調細
		public bool bModifyNeck		= true;

		// 移除眼鏡
		public bool bRemoveGlass	= false;

		// 移除微笑
		public bool bRemoveSmile	= false;

		// 移除鬍子
		public bool bRemoveStubble	= false;

		/*********************************
		 * 光線顏色調整
		 * *******************************/
		// 打光強化
		public bool bEnhanceLight	= true;

		// 眼皮陰影
		public bool bEyelidShadow	= false;

		// 加強眼部亮度
		public bool bEyeGlare		= false;

		// 眼白顏色調整
		public Color eyeScleraColor = Color.white;

		// 髮色
		public Color hairColor		= new Color(1, 1, 1, 0);

		// 唇色
		public Color lipColor		= new Color(1, 1, 1, 0);

		// 齒色
		public Color teethColor			= new Color(1, 1, 1, 0);

		/*********************************
		 * 其他
		 * *******************************/
		// 與模型紋理看來略為卡通化
		//public bool bSlightlyCartoonishTexture	= true;
		public float cartoonishV1				= 0f;

		// 強化模型特色，有效值範圍為(0~無限大)，建議低於5
		public float caricatureAmount			= 1f;

		public TextureSize headTextureSize;

		public TextureSize hairTextureSize;

		public int hairFacesCount;
	}

	public class ModificationParam
	{
		public AvatarModificationsGroup avatarModificationsGroup;
		public ShapeModificationsGroup shapeModificationsGroup;

		public ModificationParam()
		{
			avatarModificationsGroup	= new AvatarModificationsGroup();
			shapeModificationsGroup		= new ShapeModificationsGroup();
		}
	}

	public class AvatarModification : ICommand, IValueReference<ModificationParam>
	{
		private ModificationsGroup modificationsGroup;

		/******************************
         * IValueReference
         * ****************************/
		public ModificationParam Value { get; set; }

		public AvatarModification(ModificationsGroup group)
		{
			this.modificationsGroup = group;
			Value					= new ModificationParam();
		}

		/******************************
         * 實作ICommand
         * ****************************/
		public void Execute()
		{
			Value.avatarModificationsGroup.allowModifyNeck	= GenerateProperty<bool>("allow_modify_neck", modificationsGroup.bModifyNeck);
			Value.avatarModificationsGroup.removeGlasses	= GenerateProperty<bool>("remove_glasses", modificationsGroup.bRemoveGlass);
			Value.avatarModificationsGroup.removeSmile		= GenerateProperty<bool>("remove_smile", modificationsGroup.bRemoveSmile);
#if AVATARSDK_CLOUD
			Value.avatarModificationsGroup.removeStubble	= GenerateProperty<bool>("remove_stubble", modificationsGroup.bRemoveStubble);
#endif // AVATARSDK_CLOUD
			bool bParametricEyesTexture = modificationsGroup.bEyelidShadow
										|| modificationsGroup.bEyeGlare
										|| modificationsGroup.eyeScleraColor.a != 0f;

			// 該參數影響上面三個功能
			Value.avatarModificationsGroup.parametricEyesTexture = GenerateProperty<bool>("parametric_eyes_texture", bParametricEyesTexture);

			Value.avatarModificationsGroup.enhanceLighting	= GenerateProperty<bool>("enhance_lighting", modificationsGroup.bEnhanceLight);
			Value.avatarModificationsGroup.addEyelidShadow	= GenerateProperty<bool>("add_eyelid_shadow", modificationsGroup.bEyelidShadow);
			Value.avatarModificationsGroup.addGlare			= GenerateProperty<bool>("add_glare", modificationsGroup.bEyeGlare);
			Value.avatarModificationsGroup.eyeScleraColor	= modificationsGroup.eyeScleraColor.a == 0f? null:GenerateProperty<Color>("eye_sclera_color", modificationsGroup.eyeScleraColor);
			Value.avatarModificationsGroup.hairColor		= modificationsGroup.hairColor.a == 0f ? null : GenerateProperty<Color>("hair_color", modificationsGroup.hairColor);
			Value.avatarModificationsGroup.lipsColor		= modificationsGroup.lipColor.a == 0f ? null : GenerateProperty<Color>("lips_color", modificationsGroup.lipColor);
			Value.avatarModificationsGroup.teethColor		= modificationsGroup.teethColor.a == 0f ? null : GenerateProperty<Color>("teeth_color", modificationsGroup.teethColor);

#if AVATARSDK_CLOUD
			Value.avatarModificationsGroup.bSlightlyCartoonishTexture	= GenerateProperty<bool>("slightly_cartoonish_texture", modificationsGroup.cartoonishV1 > 0);
#endif //AVATARSDK_CLOUD
			Value.avatarModificationsGroup.caricatureAmount				= GenerateProperty<float>("caricature_amount", modificationsGroup.caricatureAmount);
			Value.avatarModificationsGroup.textureSize					= GenerateProperty<TextureSize>("texture_size", modificationsGroup.headTextureSize);
			Value.avatarModificationsGroup.generatedHaircutTextureSize	= GenerateProperty<TextureSize>("generated_haircut_texture_size ", modificationsGroup.hairTextureSize);
			Value.avatarModificationsGroup.generatedHaircutFacesCount	= GenerateProperty<int>("generated_haircut_faces_count ", modificationsGroup.hairFacesCount);

			Value.shapeModificationsGroup.cartoonishV1.Value			= modificationsGroup.cartoonishV1;
		}

		/******************************
		 * 其他
		 * ****************************/
		private ComputationProperty<T> GenerateProperty<T>(string propertyName, T value)
		{
			ComputationProperty<T> newProperty	= new ComputationProperty<T>("plus", propertyName);
			newProperty.IsAvailable				= true;
			newProperty.Value					= value;

			return newProperty;
		}
	}
}