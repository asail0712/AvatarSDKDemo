/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using ItSeez3D.AvatarSdk.Core;
using System;
using System.Runtime.InteropServices;

namespace ItSeez3D.AvatarSdk.LocalCompute
{
	public static class PipelineTypeExtensionsLocalCompute
	{
		public static AvatarSdkPipelineSubtype ToSdkPipelineSubType(this PipelineType type)
		{
			switch (type)
			{
				case PipelineType.FACE: throw new Exception("Unsupported pipeline type: " + type.ToString());
				case PipelineType.HEAD_1_2: throw new Exception("Unsupported pipeline type: " + type.ToString());
				case PipelineType.HEAD_2_0_BUST_MOBILE: return AvatarSdkPipelineSubtype.BUST_MOBILE;
				case PipelineType.HEAD_2_0_HEAD_MOBILE: return AvatarSdkPipelineSubtype.HEAD_MOBILE;
				default: throw new Exception("Unknown pipeline type");
			}
		}
	}

	delegate void ReportProgress(float progressFraction);

	public enum AvatarSdkPipelineSubtype
	{
		HEAD_MOBILE = 0,
		BUST_MOBILE = 1,
	}

	public enum AvatarSdkMeshFormat
	{
		AVATAR_SDK_MESH_FORMAT_PLY,
		AVATAR_SDK_MESH_FORMAT_OBJ
	};

	class NativeMethods
	{
		#region Dll interface

		[DllImport(DllHelper.dll)]
		private static extern int initAvatarSdk(string programName, string resourcesPath);

		[DllImport(DllHelper.dll)]
		private static extern int releaseAvatarSdk();

		[DllImport(DllHelper.dll)]
		private unsafe static extern void getLastError(byte* buffer, int size);

		[DllImport(DllHelper.dll)]
		private static extern int initializeAvatarFromRawData(IntPtr rawPhotoBytesRGBA, int w, int h, string avatarDirectory);

		[DllImport(DllHelper.dll)]
		private static extern int generateAvatarForUnity(AvatarSdkPipelineSubtype avatarParams, string inputImagePath, string outputDirPath,
			string resourcesJsonFilePath, ReportProgress reportProgressFunc);

		[DllImport(DllHelper.dll)]
		private static extern int generateUmaAvatarForUnity(string inputImagePath, string outputDirPath, string resourcesJsonFilePath, ReportProgress reportProgressFunc);

		[DllImport(DllHelper.dll)]
		private static extern int generateAdditionalHaircutsFromJson(string meshFilePath, string outputDir, string resourcesJsonWithHaircuts, AvatarSdkMeshFormat format, bool saveAsPointCloud);

		[DllImport(DllHelper.dll)]
		private static extern int generateLODMesh(AvatarSdkPipelineSubtype pipelineType, int levelOfDetails, string meshFilePath, string outMeshFilePath, string blendshapesDir, string outBlendshapesDir);

		[DllImport(DllHelper.dll)]
		private static extern int extractHaircutFromResources(string haircutId, string haircutsDirectory, string resourcesDirectory);

		[DllImport(DllHelper.dll)]
		private static extern int extractHaircutPreviewFromResources(string haircutId, string haircutsDirectory, string resourcesDirectory);

		[DllImport(DllHelper.dll)]
		public static extern void setLoggingFile(string logFile);

		[DllImport(DllHelper.dll)]
		public static extern void setAdvancedLogs([MarshalAs(UnmanagedType.U1)] bool isAdvanced);

		[DllImport(DllHelper.dll)]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool isHardwareSupported();

		[DllImport(DllHelper.dll)]
		private static extern void setApiUrl(string apiUrl);

		#endregion

		#region public methods
		public static int InitAvatarSdk(string programName, string resourcesPath = "")
		{
			return initAvatarSdk(programName, resourcesPath);
		}

		public static void ReleaseAvatarSdk()
		{
			releaseAvatarSdk();
		}

		public unsafe static void GetLastError(byte* buffer, int size)
		{
			getLastError(buffer, size);
		}

		public static int InitializeAvatarFromRawData(IntPtr rawPhotoBytesRGBA, int w, int h, string avatarDirectory)
		{
			return initializeAvatarFromRawData(rawPhotoBytesRGBA, w, h, avatarDirectory);
		}

		public static int GenerateAvatar(AvatarSdkPipelineSubtype subtype, string inputImagePath, string outputDirPath, string resourcesJson, ReportProgress reportProgressFunc)
		{
			return generateAvatarForUnity(subtype, inputImagePath, outputDirPath, resourcesJson, reportProgressFunc);
		}

		public static int GenerateUmaAvatar(string inputImagePath, string outputDirPath, string resourcesJsonFilePath, ReportProgress reportProgressFunc)
		{
			return generateUmaAvatarForUnity(inputImagePath, outputDirPath, resourcesJsonFilePath, reportProgressFunc);
		}

		public static int GenerateHaircuts(string meshFilePath, string outputDir, string resourcesJsonWithHaircuts)
		{
			return generateAdditionalHaircutsFromJson(meshFilePath, outputDir, resourcesJsonWithHaircuts, AvatarSdkMeshFormat.AVATAR_SDK_MESH_FORMAT_PLY, true);
		}

		public static int GenerateLODMesh(AvatarSdkPipelineSubtype pipelineSubtype, int levelOfDetails, string meshFilePath, string outMeshFilePath, string blendshapesDir, string outBlendshapesDir)
		{
			return generateLODMesh(pipelineSubtype, levelOfDetails, meshFilePath, outMeshFilePath, blendshapesDir, outBlendshapesDir);
		}

		public static int ExtractHaircutFromResources(string haircutId, string haircutsDirectory, string resourcesDirectory)
		{
			return extractHaircutFromResources(haircutId, haircutsDirectory, resourcesDirectory);
		}

		public static int ExtractHaircutPreviewFromResources(string haircutId, string haircutsDirectory, string resourcesDirectory)
		{
			return extractHaircutPreviewFromResources(haircutId, haircutsDirectory, resourcesDirectory);
		}

		public static bool IsHardwareSupported()
		{
			return isHardwareSupported();
		}

		public static void SetApiUrl(string apiUrl)
		{
			setApiUrl(apiUrl);
		}
		#endregion
	}
}
