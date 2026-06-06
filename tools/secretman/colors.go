package main

const (
	Red    = "\033[31m"
	Green  = "\033[32m"
	Yellow = "\033[33m"
	Blue   = "\033[36m"
	Purple = "\033[35m"
	Grey   = "\033[90m"
	Reset  = "\033[0m"
)

func redStr(str string) string {
	return Red + str + Reset
}

func greenStr(str string) string {
	return Green + str + Reset
}

func yellowStr(str string) string {
	return Yellow + str + Reset
}

func blueStr(str string) string {
	return Blue + str + Reset
}

func purpleStr(str string) string {
	return Purple + str + Reset
}

func greyStr(str string) string {
	return Grey + str + Reset
}
