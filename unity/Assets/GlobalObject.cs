using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;

namespace chatapp
{
	/// <summary>
	/// サーバーから受け取ったメッセージデータ
	/// </summary>
	public class MessageData
	{
		/// <summary>
		/// キー名
		/// </summary>
		public string Key;

		/// <summary>
		/// パラメータ配列
		/// </summary>
		public string[] Params;
	}

	/// <summary>
	/// シーンが変わっても消えないオブジェクト
	/// </summary>
	public class GlobalObject : MonoBehaviour
	{
		/// <summary>
		/// インスタンスアクセッサ
		/// </summary>
		public static GlobalObject Instance { get; private set; }

		/// <summary>
		/// Awake
		/// </summary>
		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}

			_messageBuffer = new byte[2048];
		}

		/// <summary>
		/// OnDestroy
		/// </summary>
		void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}

			Leave();
		}

		/// <summary>
		/// デフォルトサーバーURL
		/// </summary>
		private const string DefaultServerUrl = "127.0.0.1";
		
		/// <summary>
		/// デフォルトサーバーポート
		/// </summary>
		private const int DefaultServerPort = 50000;

		/// <summary>
		/// 自分のユーザー名
		/// </summary>
		public string MyUserName { get; set; }

		/// <summary>
		/// サーバーURL
		/// </summary>
		private string _serverUrl;

		/// <summary>
		/// サーバーポート番号
		/// </summary>
		private int _serverPort;

		/// <summary>
		/// NetworkStream
		/// </summary>
		private NetworkStream _stream;

		/// <summary>
		/// 受け取ったメッセージを格納するバッファ
		/// </summary>
		private byte[] _messageBuffer;

		/// <summary>
		/// 受信済みメッセージデータのリスト
		/// </summary>
		private readonly List<MessageData> _receivedMessageList = new List<MessageData>();
		
		/// <summary>
		/// スタンプ画像
		/// </summary>
		[SerializeField] private Sprite[] _stampSprites;

		/// <summary>
		/// スタンプNo.に対応するスプライト取得
		/// </summary>
		/// <param name="stampNo">スタンプNo.</param>
		/// <returns>スタンプスプライト</returns>
		public Sprite GetStampSprite(int stampNo)
		{
			var stampIndex = stampNo - 1;
			if (stampIndex >= 0 && stampIndex < _stampSprites.Length)
			{
				return _stampSprites[stampIndex];
			}

			return null;
		}

		/// <summary>
		/// チャットルームに参加
		/// </summary>
		/// <param name="userName">ユーザー名</param>
		/// <param name="serverUrl">サーバーURL</param>
		/// <param name="serverPort">サーバーポート</param>
		public void Join(string userName, string serverUrl, int serverPort)
		{
			MyUserName = userName;
			_serverUrl = string.IsNullOrEmpty(serverUrl) ? DefaultServerUrl : serverUrl;
			_serverPort = serverPort <= 0 ? DefaultServerPort : serverPort;

			emit("join:" + Util.EncodeBase64(userName));
		}

		/// <summary>
		/// チャットルームから退室
		/// </summary>
		public void Leave()
		{
			if (_stream != null)
			{
				_stream.Close();
				_stream = null;
			}

			lock (((ICollection) _receivedMessageList).SyncRoot)
			{
				_receivedMessageList.Clear();
			}
		}

		/// <summary>
		/// チャット送信
		/// </summary>
		/// <param name="text">送信テキスト</param>
		public void SendChat(string text)
		{
			emit("chat:" + Util.EncodeBase64(text));
		}

		/// <summary>
		/// スタンプ送信
		/// </summary>
		/// <param name="stampNo">スタンプ番号</param>
		public void SendStamp(int stampNo)
		{
			emit("stamp:" + stampNo.ToString());
		}

		/// <summary>
		/// 受信済みメッセージリスト取得
		/// </summary>
		/// <returns>受け取ったメッセージデータのリスト</returns>
		public List<MessageData> GetReceivedMessageList()
		{
			List<MessageData> copy;
			lock (((ICollection) _receivedMessageList).SyncRoot)
			{
				copy = new List<MessageData>(_receivedMessageList);
			}

			return copy;
		}

		/// <summary>
		/// NetworkStreamの取得
		/// </summary>
		/// <returns>NetworkStream</returns>
		private NetworkStream getNetworkStream()
		{
			if (_stream != null && _stream.CanRead)
			{
				return _stream;
			}

			Leave();

			// TcpClientを作成し、サーバーと接続する
			Debug.Log("Try connect " + _serverUrl + ":" + _serverPort.ToString());
			TcpClient tcp = new TcpClient(_serverUrl, _serverPort);
			_stream = tcp.GetStream();
			StartCoroutine(readMessage(_stream));
			return _stream;
		}

		/// <summary>
		/// メッセージ監視コルーチン
		/// </summary>
		/// <param name="stream">NetworkStream</param>
		/// <returns>IEnumerator</returns>
		private IEnumerator readMessage(NetworkStream stream)
		{
			while (stream.CanRead)
			{
				stream.BeginRead(_messageBuffer, 0, _messageBuffer.Length, new AsyncCallback(onReceiveMessage), null);
				yield return null;
			}
		}

		/// <summary>
		/// メッセージ受信コールバック
		/// </summary>
		/// <param name="ar"></param>
		private void onReceiveMessage(IAsyncResult ar)
		{
			if (_stream == null || !_stream.CanRead)
			{
				return;
			}

			var bytes = _stream.EndRead(ar);
			var messages = Encoding.UTF8.GetString(_messageBuffer, 0, bytes).Split('\n');
			foreach (var message in messages)
			{
				if (message.Length <= 0) continue;
				var keyLen = message.IndexOf(":");
				if (keyLen <= 0) continue;
				var data = new MessageData();
				data.Key = message.Substring(0, keyLen);
				data.Params = message.Substring(keyLen + 1).Split(',');
				lock (((ICollection)_receivedMessageList).SyncRoot)
				{
					_receivedMessageList.Add(data);
				}
			}
		}

		/// <summary>
		/// サーバーにメッセージ送信
		/// </summary>
		/// <param name="message">送信するメッセージ</param>
		private void emit(string message)
		{
			var sendBytes = Encoding.UTF8.GetBytes(message);
			getNetworkStream().Write(sendBytes, 0, sendBytes.Length);
		}
	}
}