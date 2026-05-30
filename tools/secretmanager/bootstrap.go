package main

import (
	"fmt"
	"runtime"
)

const toolsDir = ".tools/"

func bootstrap() error {

	// check OS
	if err := checkOs(); err != nil {
		return err
	}

	// create a cache dir .tools for secretman if missing
	if err := ensureToolsCache(); err != nil {
		return err
	}

	// check for SOPS and install if missing
	userArch := runtime.GOARCH
	if err := ensureSOPS(userArch, toolsDir+"sops"); err != nil {
		return err
	}

	// check for AGE and install if missing
	if err := ensureAGE(userArch, toolsDir+"age.tar.gz"); err != nil {
		return err
	}

	// check for AGE secrets else generate one
	if err := ensureAGESecret(); err != nil {
		return err
	}

	// check for git-hooks and install if missing

	fmt.Printf("\n✅ Secretman setup finished successfully.\n")

	return nil
}

// func encryptRoot() error {
//
// 	cmd := exec.Command(".tools/secretman/sops", "--encrypt", "../../.env", ">", "../../secrets/root.env.crypt")
// 	err := cmd.Run()
// 	if err != nil {
// 		log.Fatal(err)
// 	}
// 	return nil
// }
