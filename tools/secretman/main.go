package main

import (
	"fmt"
	"os"
)

func main() {

	var setup, refresh, decrypt, encrypt bool

	err := flagSetup(&setup, &refresh, &decrypt, &encrypt)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	var cmd func() error

	switch {
	case setup:
		cmd = bootstrap
	case refresh:
		cmd = refreshSecrets
	case encrypt:
		cmd = encryptSecrets
	case decrypt:
		cmd = decryptSecrets
	default:
		fmt.Fprintln(os.Stderr, "❌ Error: expected at least one flag, use -h for help")
		os.Exit(1)
	}

	if err := cmd(); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
