package main

import (
	"fmt"
	"os"
)

func fileExists(filepath string) bool {

	_, err := os.Stat(filepath)

	if err != nil && os.IsNotExist(err) {
		return false
	}
	return true
}

func changePerm(path string, perm os.FileMode) error {

	err := os.Chmod(path, perm)
	if err != nil {
		fmt.Println("❌ File permissions update failed.")
		return err
	}

	fmt.Println("✅ File permissions updated.")
	return nil
}
