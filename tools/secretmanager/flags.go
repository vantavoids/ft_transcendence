package main

import (
	"flag"
	"fmt"
	"os"
)

func flagSetup(setup *bool, decryptAll *bool, encryptAll *bool) {

	flagUsage()

	flag.BoolVar(setup, "setup", false, "run setup")
	flag.BoolVar(setup, "s", false, "")

	flag.BoolVar(decryptAll, "decrypt-all", false, "decrypt all env files")
	flag.BoolVar(decryptAll, "d", false, "")

	flag.BoolVar(encryptAll, "encrypt-all", false, "encrypt all env files")
	flag.BoolVar(encryptAll, "e", false, "")

	flag.Parse()

	count := 0
	for _, enabled := range []bool{*setup, *decryptAll, *encryptAll} {
		if enabled {
			count++
		}
	}

	if count > 1 {
		fmt.Fprintln(os.Stderr, "❌ Error: use only one action flag at a time")
		os.Exit(1)
	}
}

func flagUsage() {
	flag.Usage = func() {
		fmt.Fprintf(os.Stderr, `Usage:
  secretman [flags]

Flags:
  -s, --setup         run setup
  -d, --decrypt-all   decrypt all env files
  -e, --encrypt-all   encrypt all env files
  -h, --help          show help
`)
	}
}
