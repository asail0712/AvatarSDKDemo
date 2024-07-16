/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using ItSeez3D.AvatarSdk.Core.Communication;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.Core.Editor
{
	public class ApiUrlSettingsWindow : EditorWindow
	{
		private string apiUrl = string.Empty;

		private bool isRequestInProgress = false;

		private string notificationMessage = string.Empty;
		private bool showNotification = false;

		GUIStyle textStyle;
		GUIStyle titleStyle;
		GUIStyle richTextStyle;
		readonly string linkColor = "#3366BB";

		[MenuItem("Window/itSeez3D Avatar SDK/Api Url Settings")]
		static void Init()
		{
			var window = (ApiUrlSettingsWindow)EditorWindow.GetWindow(typeof(ApiUrlSettingsWindow));
			window.InitUI();
			window.titleContent.text = "Api Url Settings";
			window.minSize = new Vector2(450, 230);
			window.maxSize = window.minSize;
			window.apiUrl = NetworkUtils.ApiUrl;
			window.Show();
		}

		void InitUI()
		{
			titleStyle = new GUIStyle(EditorStyles.boldLabel);
			titleStyle.alignment = TextAnchor.MiddleCenter;

			textStyle = new GUIStyle(EditorStyles.label);
			textStyle.wordWrap = true;

			richTextStyle = new GUIStyle(EditorStyles.label);
			richTextStyle.richText = true;
			richTextStyle.wordWrap = true;
		}

		void OnGUI()
		{
			//GUILayout.Label("API URL Settings", titleStyle);

			GUILayout.Space(5);
			GUILayout.Label("<color=red>We don't recommend changing API URL without the necessity!</color>", richTextStyle);

			GUILayout.Space(5);
			GUILayout.Label("If you have problems with connection by default API URL due to firewall restrictions, you can try backup URL.", textStyle);

			GUILayout.Space(10);
			GUILayout.Label("Default API URL", titleStyle);
			if (GUILayout.Button(string.Format("<size=16><color={0}><b>{1}</b></color></size>", linkColor, NetworkUtils.DefaultApiUrl), richTextStyle))
			{
				apiUrl = NetworkUtils.DefaultApiUrl;
			}

			GUILayout.Space(10);
			GUILayout.Label("Backup API URL", titleStyle);
			if (GUILayout.Button(string.Format("<size=16><color={0}><b>{1}</b></color></size>", linkColor, NetworkUtils.BackupApiUrl), richTextStyle))
			{
				apiUrl = NetworkUtils.BackupApiUrl;
			}

			GUILayout.Space(10);
			GUI.enabled = !isRequestInProgress;
			apiUrl = EditorGUILayout.TextField("API URL", apiUrl);
			GUILayout.Space(4);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Save API URL", GUILayout.Width(150), GUILayout.Height(25)))
			{
				if (string.IsNullOrEmpty(apiUrl))
					NetworkUtils.StoreApiUrl(NetworkUtils.DefaultApiUrl);
				else
					NetworkUtils.StoreApiUrl(apiUrl);
				apiUrl = NetworkUtils.ApiUrl;
				ShowNotification(new GUIContent(string.Format("API URL is saved: {0}", apiUrl)));
			}
			GUI.enabled = !isRequestInProgress && !string.IsNullOrEmpty(apiUrl);
			if (GUILayout.Button("Test connection", GUILayout.Width(150), GUILayout.Height(25)))
			{
				Coroutines.EditorRunner.instance.Run(TestConnection());
			}
			GUI.enabled = true;
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if (showNotification)
			{
				ShowNotification(new GUIContent(notificationMessage));
				showNotification = false;
			}
		}

		private IEnumerator TestConnection()
		{
			Debug.Log("Sending request to the server to test connection.");
			isRequestInProgress = true;
			ConnectionBase connection = new ConnectionBase();

			var credentials = AuthUtils.LoadCredentials();
			var request = connection.GenerateAuthRequest(credentials);

			request.SendWebRequest();

			while (!request.isDone)
				yield return null;

			Debug.LogFormat("Server response: {0}", request.downloadHandler.text);
			AccessData accessData = JsonUtility.FromJson<AccessData>(request.downloadHandler.text);

			bool isError = NetworkUtils.IsWebRequestFailed(request);
			if (isError || string.IsNullOrEmpty(accessData.access_token))
			{
				Debug.LogErrorFormat("Connection error: {0}", request.error);
				notificationMessage = "Unable to get access to the cloud API";
			}
			else if (string.IsNullOrEmpty(accessData.access_token))
			{
				Debug.LogErrorFormat("Credentials are invalid: {0}", request.downloadHandler.text);
				notificationMessage = "Credentials are invalid!";
			}
			else
			{
				Debug.Log("Successful authentication!");
				notificationMessage = "Successful authentication!";
			}
			showNotification = true;
			isRequestInProgress = false;
			Repaint();
		}
	}
}
