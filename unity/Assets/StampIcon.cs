using UnityEngine;
using UnityEngine.UI;

namespace chatapp
{
	/// <summary>
	/// スタンプアイコンクラス
	/// </summary>
	public class StampIcon : MonoBehaviour
	{
		/// <summary>
		/// スタンプNo.
		/// </summary>
		[SerializeField] private int _stampNo;

		/// <summary>
		/// チャットシーン
		/// </summary>
		[SerializeField] private ChatScene _chatScene;

		/// <summary>
		/// Awake
		/// </summary>
		void Awake()
		{
			var image = GetComponent<Image>();
			var stampSprite = GlobalObject.Instance.GetStampSprite(_stampNo);
			
			if (image && stampSprite)
			{
				image.sprite = stampSprite;
			}
		}

		/// <summary>
		/// スタンプがクリックされた
		/// </summary>
		public void OnClickStamp()
		{
			_chatScene.OnClickStamp(_stampNo);
		}
	}
}