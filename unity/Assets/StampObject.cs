using UnityEngine;
using UnityEngine.UI;

namespace chatapp
{
	/// <summary>
	/// スタンプオブジェクト
	/// </summary>
	public class StampObject : MonoBehaviour
	{
		[SerializeField] private Text _userName;
		[SerializeField] private Image _stampImage;

		/// <summary>
		/// データ設定
		/// </summary>
		/// <param name="userName">ユーザー名</param>
		/// <param name="stampNo">スタンプNo.</param>
		public void SetData(string userName, int stampNo)
		{
			_userName.text = userName;

			var stampSprite = GlobalObject.Instance.GetStampSprite(stampNo);
			if (stampSprite)
			{
				_stampImage.sprite = stampSprite;
			}
		}
	}
}