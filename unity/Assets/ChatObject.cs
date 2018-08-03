using UnityEngine;
using UnityEngine.UI;

namespace chatapp
{
    /// <summary>
    /// チャットオブジェクト
    /// </summary>
    public class ChatObject : MonoBehaviour
    {
        [SerializeField] private Text _userName;
        [SerializeField] private Text _message;

        /// <summary>
        /// データ設定
        /// </summary>
        /// <param name="userName">ユーザー名</param>
        /// <param name="chatMessage">チャットメッセージ</param>
        public void SetData(string userName, string message)
        {
            if (_userName != null)
            {
                _userName.text = userName;
            }

            _message.text = message;
        }
    }
}