package main

import (
	"fmt"
	"net"
)

// エラーコード
type ErrorCode int

const (
	// 不明なエラー、その他のエラー
	ErrorCodeUnknown ErrorCode = iota
	// リクエストが不正
	ErrorCodeBadRequest
	// 禁止されたリクエスト
	ErrorCodeForbidden
	// 同名のユーザーがいた
	ErrorCodeDuplicateUserName
)

// エラー構造体
type MyError struct {
	Code ErrorCode
}

// MyError構造体にerrorインタフェースのError関数を実装
func (err MyError) Error() string {
	msg := ""
	switch err.Code {
	case ErrorCodeUnknown:
		msg = "エラーが発生しました。"
	case ErrorCodeBadRequest:
		msg = "エラーが発生しました。"
	case ErrorCodeForbidden:
		msg = "エラーが発生しました。"
	case ErrorCodeDuplicateUserName:
		msg = "その名前は既に使われています。"
	}
	return fmt.Sprintf("%s (エラーコード:%d)", msg, err.Code)
}

/**
 * 引数errがMyError構造体で、かつ引数codeと同じエラーコードを持っているか調べる
 */
func EqualErrorCode(err error, code ErrorCode) bool {
	myError, ok := err.(*MyError)
	if !ok {
		return false
	}

	return myError.Code == code
}

/**
 * エラーがネットワークタイムアウトによるものなのか返す
 */
func IsNetworkTimeout(err error) bool {
	netError, ok := err.(net.Error)
	if !ok {
		return false
	}

	return netError.Timeout()
}
