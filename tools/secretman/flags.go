package main

import (
	"flag"
	"fmt"
	"os"
)

func flagSetup(setup *bool, refresh *bool, decrypt *bool, encrypt *bool) error {

	flagUsage()

	flag.BoolVar(setup, "setup", false, "run setup")
	flag.BoolVar(setup, "s", false, "")

	flag.BoolVar(refresh, "refresh", false, "refresh all secret files")
	flag.BoolVar(refresh, "r", false, "")

	flag.BoolVar(decrypt, "decrypt", false, "decrypt env files")
	flag.BoolVar(decrypt, "d", false, "")

	flag.BoolVar(encrypt, "encrypt", false, "encrypt env files")
	flag.BoolVar(encrypt, "e", false, "")

	flag.Parse()

	count := 0
	for _, enabled := range []bool{*setup, *refresh, *decrypt, *encrypt} {
		if enabled {
			count++
		}
	}

	if count > 1 {
		return fmt.Errorf("❌ Use only one action flag at a time")
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
  -d, --decrypt		decrypt env files
  -e, --encrypt		encrypt env files
  -h, --help		show help
`)
	}
}
