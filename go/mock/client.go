package main

import (
	"encoding/base64"
	"fmt"
	"net"
	"os"
	"strconv"
	"time"
)

func main() {
	if len(os.Args) != 3 {
		return
	}
	userName := os.Args[1]
	port := os.Args[2]

	serverIP := "127.0.0.1"           //サーバ側のIP
	serverPort := "50000"             //サーバ側のポート番号
	myIP := "127.0.0.1"               //クライアント側のIP
	myPort, err := strconv.Atoi(port) //クライアント側のポート番号
	Assert(err)

	tcpAddr, err := net.ResolveTCPAddr("tcp", serverIP+":"+serverPort)
	Assert(err)
	myAddr := new(net.TCPAddr)
	myAddr.IP = net.ParseIP(myIP)
	myAddr.Port = myPort
	conn, err := net.DialTCP("tcp", myAddr, tcpAddr)
	Assert(err)
	defer conn.Close()

	conn.SetWriteDeadline(time.Now().Add(10 * time.Second))
	conn.Write([]byte("join:" + base64.StdEncoding.EncodeToString([]byte(userName))))

	readBuf := make([]byte, 1024)
	count := 1
	for {
		conn.SetReadDeadline(time.Now().Add(10 * time.Second))
		readlen, err := conn.Read(readBuf)

		if err == nil {
			fmt.Println("[recv] " + string(readBuf[:readlen]))
		}

		sendMsg := fmt.Sprintf("hello%d", count)
		conn.SetWriteDeadline(time.Now().Add(10 * time.Second))

		conn.Write([]byte("chat:" + base64.StdEncoding.EncodeToString([]byte(sendMsg))))
		count++
		time.Sleep(1000000000)
	}
}

func Assert(err error) {
	if err != nil {
		fmt.Fprintf(os.Stderr, "[error] %s", err.Error())
		os.Exit(1)
	}
}
