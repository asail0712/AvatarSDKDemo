﻿using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XPlan.Utility
{ 
	public static class LocalPref
	{
		public static bool HasKeyInEditor(string key)
		{
#if UNITY_EDITOR
			return EditorPrefs.HasKey(key);
#else
			return false;
#endif //UNITY_EDITOR
		}

		public static bool HasKey(string key)
		{
			return PlayerPrefs.HasKey(key);
		}

		public static void DeleteKeyInEditor(string key)
		{
#if UNITY_EDITOR
			EditorPrefs.DeleteKey(key);
#endif //UNITY_EDITOR
		}

		public static void DeleteKey(string key)
		{
			PlayerPrefs.DeleteKey(key);
		}

		public static void SetValueInEditor<T>(string key, T value)
		{
	#if UNITY_EDITOR
			if (typeof(T) == typeof(int))
			{
				EditorPrefs.SetInt(key, Convert.ToInt32(value));
			}
			else if (typeof(T) == typeof(bool))
			{
				EditorPrefs.SetBool(key, Convert.ToBoolean(value));
			}
			else if (typeof(T) == typeof(float))
			{
				EditorPrefs.SetFloat(key, Convert.ToSingle(value));
			}
			else if (typeof(T) == typeof(string))
			{
				EditorPrefs.SetString(key, Convert.ToString(value));
			}
	#endif //UNITY_EDITOR
		}

		public static void SetValue<T>(string key, T value)
		{
			if(typeof(T) == typeof(int))
			{
				PlayerPrefs.SetInt(key, Convert.ToInt32(value));
			}
			else if (typeof(T) == typeof(float))
			{
				PlayerPrefs.SetFloat(key, Convert.ToSingle(value));
			}
			else if (typeof(T) == typeof(string))
			{
				PlayerPrefs.SetString(key, Convert.ToString(value));
			}
			else if (typeof(T) == typeof(bool))
			{
				PlayerPrefs.SetInt(key, Convert.ToBoolean(value)?1:0);
			}
		}

		public static T GetValueInEditor<T>(string key, T defaultValue = default(T))
		{
	#if UNITY_EDITOR
			if(!EditorPrefs.HasKey(key))
			{
				return defaultValue;
			}

			if (typeof(T) == typeof(int))
			{
				return (T)Convert.ChangeType(EditorPrefs.GetInt(key), typeof(T));
			}
			else if (typeof(T) == typeof(bool))
			{
				return (T)Convert.ChangeType(EditorPrefs.GetBool(key), typeof(T));
			}
			else if (typeof(T) == typeof(float))
			{
				return (T)Convert.ChangeType(EditorPrefs.GetFloat(key), typeof(T));
			}
			else if (typeof(T) == typeof(string))
			{
				return (T)Convert.ChangeType(EditorPrefs.GetString(key), typeof(T));
			}
	#endif //UNITY_EDITOR

			Debug.LogError("LocalPref型態不支援");

			return default(T);
		}

		public static T GetValue<T>(string key, T defaultValue = default(T))
		{
			if (!PlayerPrefs.HasKey(key))
			{
				return defaultValue;
			}

			if (typeof(T) == typeof(int))
			{
				return (T)Convert.ChangeType(PlayerPrefs.GetInt(key), typeof(T));
			}
			else if (typeof(T) == typeof(float))
			{
				return (T)Convert.ChangeType(PlayerPrefs.GetFloat(key), typeof(T));
			}
			else if (typeof(T) == typeof(string))
			{
				return (T)Convert.ChangeType(PlayerPrefs.GetString(key), typeof(T));
			}
			else if (typeof(T) == typeof(bool))
			{
				return (T)(object)(PlayerPrefs.GetInt(key) == 1);
			}

			Debug.LogError("LocalPref型態不支援");

			return default(T);
		}
	}
}