package main

import (
	"fmt"
	"os"
)

func main() {

	var setup bool
	var refresh bool
	var decryptAll bool
	var encryptAll bool

	err := flagSetup(&setup, &refresh, &decryptAll, &encryptAll)
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
		if err := refreshAllSecrets(); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	} else if decryptAll {
		if err := decryptAllSecrets(); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	} else if encryptAll {
		if err := encryptAllSecrets(); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	}
}
