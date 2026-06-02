package main

import (
	"fmt"
	"runtime"
)

const toolsDir = ".tools/"

func bootstrap() error {

	userOS := runtime.GOOS
	userArch := runtime.GOARCH

	// check OS
	if err := checkOs(userOS, userArch); err != nil {
		return err
	}

	// create a cache dir .tools for secretman if missing
	if err := ensureToolsCache(); err != nil {
		return err
	}

	// check for SOPS and install if missing
	if err := ensureSOPS(userOS, userArch, toolsDir+"sops"); err != nil {
		return err
	}

	// check for AGE and install if missing
	if err := ensureAGE(userOS, userArch, toolsDir+"age.tar.gz"); err != nil {
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
