using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace chatapp
{
    /// <summary>
    /// チャットシーン (メインシーン)
    /// </summary>
    public class ChatScene : MonoBehaviour
    {
        [SerializeField] private ScrollRect _chatScrollRect;
        [SerializeField] private ContentSizeFitter _chatContentSizeFilter;
        [SerializeField] private InputField _chatMessage;
        [SerializeField] private GameObject _stampWindow;
        [SerializeField] private GameObject _errorDialog;
        [SerializeField] private Text _errorMessage;
        [SerializeField] private GameObject _userNameTemplate;
        [SerializeField] private GameObject _myChatTemplate;
        [SerializeField] private GameObject _otherChatTemplate;
        [SerializeField] private GameObject _joinTemplate;
        [SerializeField] private GameObject _leaveTemplate;
        [SerializeField] private GameObject _myStampTemplate;
        [SerializeField] private GameObject _otherStampTemplate;

        private Dictionary<string, Text> _userNames = new Dictionary<string, Text>();
        private int _readLength = 0;

        /// <summary>
        /// Update
        /// </summary>
        void Update()
        {
            var messageList = GlobalObject.Instance.GetReceivedMessageList();
            for (int i = _readLength; i < messageList.Count; i++)
            {
                var message = messageList[i];
                switch (message.Key)
                {
                    case "users":
                        onReceiveUsers(message.Params);
                        break;
                    case "join":
                    {
                        var userName = Util.DecodeBase64(message.Params[0]);
                        onReceiveMessage(_joinTemplate, "", userName + " さんがログインしました。");
                        break;
                    }
                    case "leave":
                    {
                        var userName = Util.DecodeBase64(message.Params[0]);
                        onReceiveMessage(_leaveTemplate, "", userName + " さんがログアウトしました。");
                        break;
                    }
                    case "chat":
                    {
                        var userName = Util.DecodeBase64(message.Params[0]);
                        var msg = Util.DecodeBase64(message.Params[1]);
                        var templateObj = userName == GlobalObject.Instance.MyUserName
                            ? _myChatTemplate
                            : _otherChatTemplate;
                        onReceiveMessage(templateObj, userName, msg);
                    }
                        break;
                    case "stamp":
                    {
                        var userName = Util.DecodeBase64(message.Params[0]);
                        var stampNo = int.Parse(message.Params[1]);
                        var templateObj = userName == GlobalObject.Instance.MyUserName
                            ? _myStampTemplate
                            : _otherStampTemplate;
                        onReceiveStamp(templateObj, userName, stampNo);
                    }
                        break;
                    case "error":
                        showErrorDialog(Util.DecodeBase64(message.Params[0]));
                        break;
                    defualt:
                        showErrorDialog("不明なエラーが発生しました。");
                        break;
                }
            }

            _readLength = messageList.Count;
        }

        /// <summary>
        /// チャットに文字が入力された
        /// </summary>
        /// <param name="text">入力文字</param>
        public void OnChatInput(string text)
        {
            if (text.IndexOf("\n") >= 0)
            {
                // 改行文字が含まれていたら送信
                OnClickSubmit();
            }
        }

        /// <summary>
        /// 送信ボタンが押された
        /// </summary>
        public void OnClickSubmit()
        {
            var text = _chatMessage.text;
            if (string.IsNullOrEmpty(text)) return;

            text = text.Trim();
            text = text.Replace("\n", "");
            if (text.Length == 0) return;

            GlobalObject.Instance.SendChat(text);

            _chatMessage.text = "";
            _chatMessage.ActivateInputField();
        }

        /// <summary>
        /// スタンプウィンドウを開く
        /// </summary>
        public void OpenStampWindow()
        {
            _stampWindow.SetActive(true);
        }

        /// <summary>
        /// スタンプウィンドウを閉じる
        /// </summary>
        public void CloseStampWindow()
        {
            _stampWindow.SetActive(false);
        }

        /// <summary>
        /// スタンプがクリックされた
        /// </summary>
        /// <param name="stampNo"></param>
        public void OnClickStamp(int stampNo)
        {
            GlobalObject.Instance.SendStamp(stampNo);
            CloseStampWindow();
        }

        /// <summary>
        /// ログアウト
        /// </summary>
        public void Logout()
        {
            GlobalObject.Instance.Leave();
            SceneManager.LoadScene("Title");
        }

        /// <summary>
        /// ユーザー名一覧受信
        /// </summary>
        /// <param name="p">受信パラメータ</param>
        void onReceiveUsers(string[] p)
        {
            var userNames = new List<string>();
            foreach (var name in p)
            {
                userNames.Add(Util.DecodeBase64(name));
            }
            
            // 新規ユーザーがいたら追加する
            foreach (var userName in userNames)
            {
                if (_userNames.ContainsKey(userName)) continue;

                GameObject newObj = Instantiate(_userNameTemplate);
                newObj.transform.SetParent(_userNameTemplate.transform.parent);
                newObj.SetActive(true);

                var textObj = newObj.GetComponent<Text>();
                textObj.text = userName;
                _userNames.Add(userName, textObj);
            }

            // いなくなったユーザーがいたら削除する
            var leaveUserNames = new List<string>();
            foreach (var userName in _userNames.Keys)
            {
                if (!userNames.Contains(userName))
                {
                    leaveUserNames.Add(userName);
                }
            }

            foreach (var leaveUserName in leaveUserNames)
            {
                GameObject.Destroy(_userNames[leaveUserName].gameObject);
                _userNames.Remove(leaveUserName);
            }
        }

        /// <summary>
        /// メッセージ受信
        /// </summary>
        /// <param name="templateObj">テンプレートオブジェクト</param>
        /// <param name="userName">ユーザー名</param>
        /// <param name="message">メッセージ</param>
        void onReceiveMessage(GameObject templateObj, string userName, string message)
        {
            GameObject newObj = Instantiate(templateObj);
            newObj.transform.SetParent(templateObj.transform.parent);
            newObj.SetActive(true);
            
            var chatObj = newObj.GetComponent<ChatObject>();            
            chatObj.SetData(userName, message);

            StartCoroutine(autoScroll());
        }

        /// <summary>
        /// スタンプ受信
        /// </summary>
        /// <param name="templateObj">テンプレートオブジェクト</param>
        /// <param name="userName">ユーザー名</param>
        /// <param name="stampNo">スタンプNo</param>
        void onReceiveStamp(GameObject templateObj, string userName, int stampNo)
        {
            GameObject newObj = Instantiate(templateObj);
            newObj.transform.SetParent(templateObj.transform.parent);
            newObj.SetActive(true);
            
            var chatObj = newObj.GetComponent<StampObject>();            
            chatObj.SetData(userName, stampNo);
            
            StartCoroutine(autoScroll());
        }

        /// <summary>
        /// 自動スクロール
        /// </summary>
        /// <returns>IEnumerator</returns>
        private IEnumerator autoScroll()
        {
            if (_chatScrollRect.verticalNormalizedPosition >= 0.1f) yield break;

            _chatContentSizeFilter.SetLayoutVertical();
            
            yield return null;

            while (_chatScrollRect.verticalNormalizedPosition >= 0.1f)
            {
                _chatScrollRect.verticalNormalizedPosition -= 0.1f;
                yield return null;
            }

            _chatScrollRect.verticalNormalizedPosition = 0;
        }

        /// <summary>
        /// エラーダイアログ表示
        /// </summary>
        void showErrorDialog(string text)
        {
            _errorDialog.SetActive(true);
            _errorMessage.text = text;
        }
    }
}