﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using XPlan.Extensions;
using XPlan.Utility;

namespace XPlan.Audio
{
	public enum AudioChannel 
	{ 
		None,
		Channel_1,
		Channel_2,
		Channel_3,
		Channel_4,
		Channel_5,
		Channel_6,
		Channel_7,
		Channel_8,
	}

	[System.Serializable]
	public class SoundGroup
	{
		// 提供使用者清晰的名稱，不提供邏輯處理
		[SerializeField]
		public string groupName = "";

		[SerializeField]
		public List<SoundInfo> infoList;
	}


	[System.Serializable]
	public class SoundInfo
	{
		[SerializeField]
		public string clipName		= "";

		[SerializeField]
		public AudioClip clip		= null;

		[SerializeField]
		public float volume			= 1f;

		[SerializeField]
		public bool bLoop			= true;

		[SerializeField]
		public AudioChannel channel = AudioChannel.Channel_1;

		public SoundInfo()
		{
			volume	= 1f;
			bLoop	= true;
			channel = AudioChannel.Channel_1;
		}
	}

	public class AudioSystem : CreateSingleton<AudioSystem>
	{
		[Header("聲音總表")]
		[SerializeField][Tooltip("放置所有要撥放的聲音")]
		private List<SoundGroup> soundGroup;

		private Dictionary<AudioChannel, AudioSource> sourceMap = new Dictionary<AudioChannel, AudioSource>();
		private List<SoundInfo> soundBank;

		protected override void InitSingleton()
		{
			if(soundGroup == null)
			{
				return;
			}

			// 初始化soundBank
			soundBank = new List<SoundInfo>();
			foreach (SoundGroup group in soundGroup)
			{
				soundBank.AddRange(group.infoList);
			}

			// 記錄所有使用到的channel
			List<AudioChannel> channelList = new List<AudioChannel>();

			foreach (SoundInfo info in soundBank)
			{
				if (!channelList.Contains(info.channel))
				{
					channelList.Add(info.channel);
				}
			}

			// 依照Channel的數量建立對應數量的AudioSource
			for (int i = 0; i < channelList.Count; ++i)
			{
				AudioSource source = gameObject.AddComponent<AudioSource>();

				sourceMap.Add(channelList[i], source);
			}
		}

		/************************************
		 * Play Sound
		 * 播放聲音可以透過clip name或是 clip index
		 * **********************************/
		public void PlaySound(string clipName, float fadeInTime = 1f, float delayTime = 0f)
		{
			int idx = soundBank.FindIndex((E04) =>
			{
				return E04.clipName == clipName;
			});

			PlaySound(idx, fadeInTime, delayTime);
		}

		public void PlaySound(int clipIdx, float fadeInTime = 1f, float delayTime = 0f)
		{
			StartCoroutine(DelayToPlay(clipIdx, fadeInTime, delayTime));
		}

		private IEnumerator DelayToPlay(int clipIdx, float fadeInTime, float delayTime)
		{
			if (delayTime > 0)
			{
				yield return new WaitForSeconds(delayTime);
			}
			
			// 參數意義分別為 撥放的audio source 撥放的曲目 fadein時間 fadeout時間
			yield return FadeInOutSound(clipIdx, fadeInTime);
		}

		/************************************
		 * Stop Sound
		 * 停止聲音可以透過clip name或是 clip index
		 * **********************************/
		public void StopSound(string clipName, float fadeOutTime = 1f)
		{
			int idx = soundBank.FindIndex((E04) =>
			{
				return E04.clipName == clipName;
			});

			StopSound(idx, fadeOutTime);
		}

		public void StopSound(int clipIdx, float fadeOutTime = 1f)
		{
			AudioSource audioSource = GetSourceByClipIndex(clipIdx);

			if (audioSource == null)
			{
				return;
			}

			if (fadeOutTime > 0f && audioSource.isPlaying)
			{
				StartCoroutine(FadeOutSound(audioSource, fadeOutTime));
			}
			else
			{
				// 如果不需要淡出，则直接停止播放
				audioSource.Stop();
			}
		}

		/************************************
		 * Pause Sound
		 * **********************************/
		public void PauseSound(string clipName)
		{
			int idx = soundBank.FindIndex((E04) =>
			{
				return E04.clipName == clipName;
			});

			PauseSound(idx);
		}

		public void PauseSound(int clipIdx)
		{
			AudioSource audioSource = GetSourceByClipIndex(clipIdx);

			if(audioSource == null)
			{
				return;
			}

			audioSource.Pause();
		}
		/************************************
		 * Resume Sound
		 * **********************************/
		public void ResumeSound(string clipName)
		{
			int idx = soundBank.FindIndex((E04) =>
			{
				return E04.clipName == clipName;
			});

			ResumeSound(idx);
		}

		public void ResumeSound(int clipIdx)
		{
			AudioSource audioSource = GetSourceByClipIndex(clipIdx);

			if (audioSource == null)
			{
				return;
			}

			audioSource.UnPause();
		}

		/************************************
		 * is playing
		 * **********************************/
		public bool IsPlaying(string clipName)
		{
			int idx = soundBank.FindIndex((E04) =>
			{
				return E04.clipName == clipName;
			});

			return IsPlaying(idx);
		}

		public bool IsPlaying(int clipIdx)
		{
			AudioSource audioSource = GetSourceByClipIndex(clipIdx);

			if(audioSource == null)
			{
				// audioSource不存在
				return false;
			}

			SoundInfo info = GetSoundByIdx(clipIdx);

			if(info == null)
			{
				return false;
			}

			if (audioSource.clip != info.clip)
			{
				// 當前 clip不為指定的 clip
				return false;
			}

			// 判斷audioSource是否有play
			return audioSource.isPlaying;
		}

		/************************************
		 * Other
		 * **********************************/

		private AudioSource GetSourceByClipIndex(int clipIdx)
		{
			if (!soundBank.IsValidIndex<SoundInfo>(clipIdx))
			{
				Debug.LogWarning($"soundBank沒有這個Idx {clipIdx}");

				return null;
			}

			AudioChannel channel = soundBank[clipIdx].channel;

			return GetSourceByChannel(channel);
		}

		private AudioSource GetSourceByChannel(AudioChannel channel)
		{
			if (!sourceMap.ContainsKey(channel))
			{
				Debug.LogError($"使用不存在的 {channel}");

				return null;
			}

			AudioSource audioSource = sourceMap[channel];

			return audioSource;
		}

		private SoundInfo GetSoundByIdx(int clipIdx)
		{
			if (!soundBank.IsValidIndex<SoundInfo>(clipIdx))
			{
				Debug.LogError($"soundBank沒有這個Idx {clipIdx}");

				return null;
			}

			SoundInfo info = soundBank[clipIdx];

			return info;
		}

		private bool IsLoopByIdx(int clipIdx)
		{
			if (!soundBank.IsValidIndex<SoundInfo>(clipIdx))
			{
				Debug.LogError($"soundBank沒有這個Idx {clipIdx}");

				return false;
			}

			bool bLoop = soundBank[clipIdx].bLoop;

			return bLoop;
		}

		/************************************
		* 實際播放聲音的流程
		* **********************************/
		private IEnumerator FadeInOutSound(int clipIdx = -1, float fadeInTime = 1f)
		{
			// 在同一個Channel做 Fade in / out的處理

			AudioSource audioSource = GetSourceByClipIndex(clipIdx);

			if(audioSource == null)
			{
				yield break;
			}

			// 設定source是否為Loop
			audioSource.loop = IsLoopByIdx(clipIdx);

			// 使用這次撥放聲音的Fade In時間除以2 當作前一個聲音的Fade Out時間			
			yield return FadeOutSound(audioSource, fadeInTime / 2f);

			// 检查是否指定了新的音频剪辑
			if (clipIdx == -1)
			{
				yield break;
			}

			SoundInfo info = GetSoundByIdx(clipIdx);

			if (info == null)
			{
				yield break;
			}

			// 更換撥放音樂
			audioSource.clip	= info.clip;
			float volume		= info.volume;

			// fade in
			yield return FadeInSound(audioSource, fadeInTime / 2f, volume);
		}

		private IEnumerator FadeOutSound(AudioSource audioSource, float fadeOutTime)
		{
			if(!audioSource.isPlaying)
			{
				yield break;
			}

			if (fadeOutTime == 0f)
			{
				audioSource.Stop();
				yield break;
			}

			float startVolume	= audioSource.volume;
			float startTime		= Time.time;

			while (Time.time < startTime + fadeOutTime)
			{
				audioSource.volume = Mathf.Lerp(startVolume, 0f, (Time.time - startTime) / fadeOutTime);
				yield return null;
			}

			audioSource.volume = 0f;
			audioSource.Stop();
		}

		private IEnumerator FadeInSound(AudioSource audioSource, float fadeInTime, float volume)
		{
			if (fadeInTime == 0f)
			{
				// 如果不需要淡入，则直接播放
				audioSource.Play();
				yield break;
			}

			audioSource.volume = 0f;
			audioSource.Play();

			float targetVolume	= volume;
			float startTime		= Time.time;

			while (Time.time < startTime + fadeInTime)
			{
				audioSource.volume = Mathf.Lerp(0f, targetVolume, (Time.time - startTime) / fadeInTime);
				yield return null;
			}

			audioSource.volume = targetVolume;
		}
	}
}
