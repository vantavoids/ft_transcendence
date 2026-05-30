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
	} else if decryptAll { // TODO decrypt all secrets
		os.Exit(0)
	} else if encryptAll { // TODO encrypt all secrets
		os.Exit(0)
	} else { // TODO setup followed by prompt
		os.Exit(0)
	}

}
