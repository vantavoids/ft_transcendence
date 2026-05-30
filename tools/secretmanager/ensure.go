package main

import (
	"fmt"
	"os"
	"runtime"
)

func checkOs() error {

	userOs := runtime.GOOS

	if userOs != "linux" {
		fmt.Printf("❌ Operating system: %v.\n", userOs)
		return fmt.Errorf("you need to be on Linux in order to use secretman, exiting.")
	} else {
		fmt.Printf("✅ Operating system: %v.\n", userOs)
	}
	return nil
}

func ensureToolsCache() error {

	err := os.MkdirAll(toolsDir, 0755)
	if err != nil && !os.IsExist(err) {
		return err
	} else if os.IsExist(err) {
		fmt.Println("✅ Tools cache directory found.")
	} else {
		fmt.Println("✅ Created tools cache directory.")
	}
	return nil
}

func ensureSOPS(userArch string, path string) error {

	fmt.Println()
	download := true

	if fileExists(path) {
		fmt.Println("✅ SOPS binary found.")
		download = askForConfirmation("➡️ Do you want to overwrite SOPS")
	} else {
		fmt.Println("⚠️ SOPS binary not found, downloading it.")
	}

	if download {
		err := installSOPS(userArch, path)
		if err != nil {
			return err
		}
	}

	return nil
}

func ensureAGE(userArch string, path string) error {

	fmt.Println()
	download := true

	if fileExists(toolsDir + "age-keygen") {
		fmt.Println("✅ AGE binary found.")
		download = askForConfirmation("➡️ Do you want to overwrite AGE")
	} else {
		fmt.Println("⚠️ AGE binary not found, downloading it.")
	}

	if download {
		err := installAGE(userArch, path)
		if err != nil {
			return err
		}
	}

	return nil
}
