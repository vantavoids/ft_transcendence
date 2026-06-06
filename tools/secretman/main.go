package main

import (
	"fmt"
	"os"
)

func main() {

	var setup bool
	var refresh bool
	var decrypt bool
	var encrypt bool

	err := flagSetup(&setup, &refresh, &decrypt, &encrypt)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	if setup {
		err := bootstrap()
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	} else if refresh {
		if err := refreshSecrets(); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	} else if decrypt {
		if err := decryptSecrets(); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	} else if encrypt {
		if err := encryptSecrets(); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	}
}
