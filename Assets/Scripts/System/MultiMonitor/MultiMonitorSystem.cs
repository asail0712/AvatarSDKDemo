using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using XPlan;

namespace Granden.MultiDisplay
{
	// 參考文件
	// https://docs.unity3d.com/cn/current/Manual/MultiDisplay.html
	// https://blog.csdn.net/weixin_33912246/article/details/93446238?utm_medium=distribute.pc_relevant.none-task-blog-2~default~baidujs_baidulandingword~default-4-93446238-blog-117528525.235^v43^pc_blog_bottom_relevance_base9&spm=1001.2101.3001.4242.3&utm_relevant_index=7

	public class MultiMonitorSystem : SystemBase
    {

		protected override void OnInitialGameObject()
		{

		}

		protected override void OnInitialHandler()
		{
			RegisterHandler(new ModifyDisplayOrderHandler());
		}
	}
}
