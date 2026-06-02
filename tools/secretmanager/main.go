package main

import (
	"log"
	"os"
)

func main() {

	var setup bool
	var decryptAll bool
	var encryptAll bool

	flagSetup(&setup, &decryptAll, &encryptAll)

	if setup {
		err := bootstrap()
		if err != nil {
			log.Fatal(err)
		}
	} else if decryptAll {
		if err := decryptAllSecrets(); err != nil {
			log.Fatal(err)
		}
	} else if encryptAll {
		if err := encryptAllSecrets(); err != nil {
			log.Fatal(err)
		}
	} else { // TODO setup followed by prompt
		os.Exit(0)
	}
}
