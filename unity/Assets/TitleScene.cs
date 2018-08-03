using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace chatapp
{
	/// <summary>
	/// タイトルシーン
	/// </summary>
	public class TitleScene : MonoBehaviour
	{
		[SerializeField] private Text _errorText;
		[SerializeField] private Text _inputText;
		[SerializeField] private Text _serverUrl;
		[SerializeField] private Text _serverPort;
		[SerializeField] private Button _loginButton;

		/// <summary>
		/// Update
		/// </summary>
		void Update()
		{
			if (_loginButton.interactable) return;

			var messageList = GlobalObject.Instance.GetReceivedMessageList();
			foreach (var message in messageList)
			{
				switch (message.Key)
				{
					case "join":
						SceneManager.LoadScene("Chat");
						return;
					case "error":
					{
						var errMsg = Util.DecodeBase64(message.Params[0]);
						StartCoroutine(showErrorText(errMsg));
						GlobalObject.Instance.Leave();
						_loginButton.interactable = true;
						return;
					}
					defualt:
						break;
				}
			}
		}

		/// <summary>
		/// ユーザー名が入力された
		/// </summary>
		public void OnNameInput(string text)
		{
			if (text.IndexOf("\n") >= 0)
			{
				// 改行文字が含まれていたら送信
				OnClickLogin();
			}
		}

		/// <summary>
		/// ログインボタンが押された
		/// </summary>
		public void OnClickLogin()
		{
			var userName = _inputText.text;
			userName = userName.Replace(" ", "").Replace("　", "").Replace("\n", "");
			if (string.IsNullOrEmpty(userName))
			{
				StartCoroutine(showErrorText("名前を入力してください"));
			}
			else if (userName.Length > 10)
			{
				StartCoroutine(showErrorText("10文字以内でお願いします"));
			}
			else
			{
				int port = string.IsNullOrEmpty(_serverPort.text) ? 0 : int.Parse(_serverPort.text);
				GlobalObject.Instance.Join(userName, _serverUrl.text, port);
				_errorText.gameObject.SetActive(false);
				_loginButton.interactable = false;
			}
		}

		/// <summary>
		/// 一定時間エラー表示を行う
		/// </summary>
		/// <param name="text">表示するテキスト</param>
		/// <returns>IEnumerator</returns>
		IEnumerator showErrorText(string text)
		{
			_errorText.text = text;
			_errorText.gameObject.SetActive(true);
			yield return new WaitForSeconds(3);
			_errorText.gameObject.SetActive(false);
		}
	}
}