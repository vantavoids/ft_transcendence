package main

import (
	"runtime"
)

const toolsDir = ".tools/secretman/"

func run() error {

	// check OS
	err := checkOs()
	if err != nil {
		return err
	}

	// create a cache dir .tools for secretman if missing
	err = ensureToolsCache()
	if err != nil {
		return err
	}

	// check for SOPS and install if missing
	userArch := runtime.GOARCH
	err = ensureSOPS(userArch, toolsDir+"sops")
	if err != nil {
		return err
	}

	// check for AGE and install if missing
	err = ensureAGE(userArch, toolsDir+"age.tar.gz")
	if err != nil {
		return err
	}

	// check for git-hooks and install if missing

	return nil
}
