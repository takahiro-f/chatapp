package main

import (
	"fmt"
	"net"
	"strconv"
	"strings"
	"sync"
	"time"
)

// 定数
const (
	ServerPort        = ":50000"
	MessageBufferSize = 2048
	DeadlineTime      = 10 * time.Second
)

// ユーザー情報
type User struct {
	// コネクション情報
	conn net.Conn
	// ユーザーID
	id int
	// ユーザー名
	name string
}

// グローバル変数アクセス同期用
var mutex sync.Mutex

// ユーザーID割り当て用
var incrementUserId = 0

// ユーザーリスト
var users = make(map[int]*User)

/**
 * メイン関数
 */
func main() {
	tcpAddr, err := net.ResolveTCPAddr("tcp", ServerPort)
	Assert(err)
	tcpListener, err := net.ListenTCP("tcp", tcpAddr)
	Assert(err)

	fmt.Println("[info] listen start.")
	for {
		conn, err := tcpListener.Accept()
		if err != nil {
			continue
		}

		go handleClient(conn)
	}
}

/**
 * 接続してきたクライアントのハンドリング
 */
func handleClient(conn net.Conn) {
	defer conn.Close()
	fmt.Println("[info] handleClient start.")

	// メッセージを受けるためのバッファを用意
	messageBuf := make([]byte, MessageBufferSize)

	// ユーザー登録
	mutex.Lock()
	incrementUserId++
	user := new(User)
	user.conn = conn
	user.id = incrementUserId
	user.name = ""
	users[user.id] = user
	mutex.Unlock()
	defer onLeave(user)

	// メッセージを監視
	for {
		conn.SetReadDeadline(time.Now().Add(DeadlineTime))
		messageLen, err := conn.Read(messageBuf)
		if err != nil {
			if IsNetworkTimeout(err) {
				continue
			}
			return
		}

		message := string(messageBuf[:messageLen])
		fmt.Println("[recv] " + message)
		onReceiveMessage(user, message)
	}
}

/**
 * ユーザー退室時の処理
 */
func onLeave(user *User) {
	mutex.Lock()
	defer mutex.Unlock()

	fmt.Printf("[info] leave user (id:%d name:%s)\n", user.id, user.name)

	// リストから削除
	delete(users, user.id)

	// 残ってるユーザーに退室通知
	EmitMessageAll(fmt.Sprintf("leave:%s", EncodeBase64(user.name)))

	// ユーザーリストの更新通知
	broadcastUserList()
}

/**
 * ユーザーリストを全員に送信
 */
func broadcastUserList() {
	var userNames string
	var first = true
	for _, user := range users {
		if !first {
			userNames += ","
		}
		first = false
		userNames += EncodeBase64(user.name)
	}
	EmitMessageAll(fmt.Sprintf("users:%s", userNames))
}

/**
 * クライアントからメッセージを受信した時の処理
 */
func onReceiveMessage(user *User, message string) {
	messages := strings.Split(message, ":")
	var params []string
	if len(messages) > 1 {
		params = strings.Split(messages[1], ",")
	}

	mutex.Lock()
	defer mutex.Unlock()

	switch messages[0] {
	case "join":
		onJoin(user, params)
	case "chat":
		onChat(user, params)
	case "stamp":
		onStamp(user, params)
	default:
		fmt.Printf("[info] 不明なメッセージを受信しました。(%s)\n", message)
		onError(user, MyError{ErrorCodeBadRequest})
	}
}

/**
 * 参加メッセージを受信した時の処理
 */
func onJoin(user *User, params []string) {
	if params == nil || len(params) != 1 {
		onError(user, MyError{ErrorCodeBadRequest})
		return
	}

	// 既に名前設定済みかチェック
	if user.name != "" {
		fmt.Printf("[info] username setted already. (%s)\n", params[0])
		onError(user, MyError{ErrorCodeForbidden})
		return
	}

	// 1つ目のパラメータがユーザー名
	userName, err := DecodeBase64(params[0])
	if err != nil || len(userName) < 1 {
		fmt.Printf("[info] username invalid. (%s)\n", params[0])
		onError(user, MyError{ErrorCodeBadRequest})
		return
	}

	// 同名のユーザーチェック
	for _, u := range users {
		if u.name == userName {
			fmt.Printf("[info] username duplicate. (%s)\n", params[0])
			onError(user, MyError{ErrorCodeDuplicateUserName})
			return
		}
	}

	// ユーザー名決定
	user.name = userName

	// 全ユーザーに参加通知
	EmitMessageAll(fmt.Sprintf("join:%s", EncodeBase64(user.name)))
	broadcastUserList()

	fmt.Printf("[info] join user (id:%d name:%s)\n", user.id, user.name)
}

/**
 * チャットメッセージを受信した時の処理
 */
func onChat(user *User, params []string) {
	if params == nil || len(params) != 1 {
		onError(user, MyError{ErrorCodeBadRequest})
		return
	}

	content, err := DecodeBase64(params[0])
	if err != nil || len(content) < 1 {
		fmt.Printf("[info] chat invalid. (%s)\n", params[0])
		onError(user, MyError{ErrorCodeBadRequest})
		return
	}

	// 全ユーザーにチャット送信
	EmitMessageAll(fmt.Sprintf("chat:%s,%s", EncodeBase64(user.name), EncodeBase64(content)))
}

/**
 * スタンプメッセージを受信した時の処理
 */
func onStamp(user *User, params []string) {
	if params == nil || len(params) != 1 {
		onError(user, MyError{ErrorCodeBadRequest})
		return
	}

	stampNo, err := strconv.Atoi(params[0])
	if err != nil {
		fmt.Printf("[info] stamp invalid. (%s)\n", params[0])
		onError(user, MyError{ErrorCodeBadRequest})
		return
	}

	// 全ユーザーにスタンプ送信
	EmitMessageAll(fmt.Sprintf("stamp:%s,%d", EncodeBase64(user.name), stampNo))
}

/**
 * エラー通知
 */
func onError(user *User, err error) {
	EmitMessage(user.conn, "error:"+EncodeBase64(err.Error()))
}
