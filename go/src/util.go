package main

import (
	"encoding/base64"
	"fmt"
	"net"
	"os"
	"time"
)

/**
 * メッセージ送信
 */
func emit(conn net.Conn, message string) {
	conn.SetWriteDeadline(time.Now().Add(DeadlineTime))
	conn.Write([]byte(message))
}

/**
 * メッセージを個別送信
 */
func EmitMessage(conn net.Conn, message string) {
	fmt.Println("[send] " + message)
	emit(conn, message+"\n")
}

/**
 * メッセージを全員に送信
 */
func EmitMessageAll(message string) {
	fmt.Println("[send] " + message)
	for _, user := range users {
		emit(user.conn, message+"\n")
	}
}

/**
 * Base64文字列にエンコード
 */
func EncodeBase64(str string) string {
	return base64.StdEncoding.EncodeToString([]byte(str))
}

/**
 * Base64文字列をデコード
 */
func DecodeBase64(str string) (string, error) {
	ret, err := base64.StdEncoding.DecodeString(str)
	if err != nil {
		return "", err
	}
	return string(ret), nil
}

/**
 * エラーがあったら停止
 */
func Assert(err error) {
	if err != nil {
		fmt.Fprintf(os.Stderr, "[error] %s", err.Error())
		os.Exit(1)
	}
}
