package main

import (
	"flag"
	"fmt"
	"os"
)

func flagSetup(setup *bool, refresh *bool, decryptAll *bool, encryptAll *bool) error {

	flagUsage()

	flag.BoolVar(setup, "setup", false, "run setup")
	flag.BoolVar(setup, "s", false, "")

	flag.BoolVar(refresh, "refresh", false, "refresh all secret files")
	flag.BoolVar(refresh, "r", false, "")

	flag.BoolVar(decryptAll, "decrypt-all", false, "decrypt all env files")
	flag.BoolVar(decryptAll, "d", false, "")

	flag.BoolVar(encryptAll, "encrypt-all", false, "encrypt all env files")
	flag.BoolVar(encryptAll, "e", false, "")

	flag.Parse()

	count := 0
	for _, enabled := range []bool{*setup, *refresh, *decryptAll, *encryptAll} {
		if enabled {
			count++
		}
	}

	if count > 1 {
		return fmt.Errorf("❌ Error: use only one action flag at a time")
	}
	return nil
}

func flagUsage() {
	flag.Usage = func() {
		fmt.Fprintf(os.Stderr, `Usage:
  secretman [flags]

Flags:
  -s, --setup		run setup
  -r, --refresh		refresh all secret files
  -d, --decrypt-all	decrypt all env files
  -e, --encrypt-all	encrypt all env files
  -h, --help		show help
`)
	}
}
