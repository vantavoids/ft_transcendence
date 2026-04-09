package utils

const (
	Red    = "\033[31m"
	Green  = "\033[32m"
	Yellow = "\033[33m"
	Blue   = "\033[36m"
	Reset  = "\033[0m"
)

func RedStr(str string) string {
	return Red + str + Reset
}

func GreenStr(str string) string {
	return Green + str + Reset
}

func YellowStr(str string) string {
	return Yellow + str + Reset
}

func BlueStr(str string) string {
	return Blue + str + Reset
}
