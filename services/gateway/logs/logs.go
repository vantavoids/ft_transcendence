// Package logs gives functions to organize and print colorful logs
package logs

import (
	"log"

	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

func Debug(source string, msg string) {

	log.Printf("%s - %s: %s", utils.GreyStr("DEBUG"), utils.GreyStr(source), msg)
}

func Info(source string, msg string) {

	log.Printf("%s - %s: %s", utils.GreenStr("INFO"), utils.GreyStr(source), msg)
}

func Warning(source string, msg string) {

	log.Printf("%s - %s: %s", utils.YellowStr("WARN"), utils.YellowStr(source), msg)
}

func Error(source string, msg string) {

	log.Printf("%s - %s: %s", utils.RedStr("ERROR"), utils.RedStr(source), msg)
}
