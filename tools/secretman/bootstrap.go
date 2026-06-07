package main

import (
	"fmt"
)

func bootstrap() error {

	fmt.Printf("➡️ Starting %s:\n\n", greenStr("setup"))

	// check OS
	if err := checkOS(userOS, userArch); err != nil {
		return err
	}

	// check for SOPS and AGE, otherwise install
	if _, err := ensureToolsPaths(true); err != nil {
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
