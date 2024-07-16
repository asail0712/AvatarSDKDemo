/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ItSeez3D.AvatarSdkSamples.Core
{
	/// <summary>
	/// Helper class to deal with head blendshape animations.
	/// </summary>
	public class AnimationManager : MonoBehaviour
	{
		#region UI

		public Text currentAnimationText;

		public RuntimeAnimatorController animatorController;

		public string[] animations;

		#endregion

		// animations-related data
		private Animator animator = null;
		private int currentAnimationIdx = 0;

		public void CreateAnimator (GameObject obj)
		{
			ChangeCurrentAnimation(0);

			animator = obj.AddComponent<Animator> ();
			animator.applyRootMotion = true;
			animator.runtimeAnimatorController = animatorController;
		}

		public void DestroyAnimator ()
		{
			animator = null;
		}

		private void ChangeCurrentAnimation (int delta)
		{
			var newIdx = currentAnimationIdx + delta;
			if (newIdx < 0)
				newIdx = animations.Length - 1;
			if (newIdx >= animations.Length)
				newIdx = 0;

			currentAnimationIdx = newIdx;
			currentAnimationText.text = animations [currentAnimationIdx].Replace ('_', ' ');

			PlayCurrentAnimation ();
		}

		public void OnPrevAnimation ()
		{
			ChangeCurrentAnimation (-1);
		}

		public void OnNextAnimation ()
		{
			ChangeCurrentAnimation (+1);
		}

		public void PlayCurrentAnimation ()
		{
			if (animator != null)
				animator.Play (animations [currentAnimationIdx]);
		}
	}
}

