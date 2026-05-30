package main

import (
	"crypto/sha256"
	"fmt"
	"io"
	"os"
)

func checkIntegrity(filepath string, checksum string) error {

	expected, err := hashFileSHA256(filepath)
	if err != nil {
		return err
	}

	if expected != checksum {
		return fmt.Errorf("failed checksum for: %s", filepath)
	}

	fmt.Println("🟢 File integrity checked.")
	return nil
}

func hashFileSHA256(filepath string) (string, error) {

	// use os.Open for read-only access
	file, err := os.Open(filepath)
	if err != nil {
		return "", fmt.Errorf("failed to open file: %w", err)
	}
	// ensure the file is closed even if errors occur later
	// capture the close error only if no other error occurred
	defer func() {
		if cerr := file.Close(); cerr != nil && err == nil {
			err = fmt.Errorf("failed to close file: %w", cerr)
		}
	}()

	// create a new SHA256 hash interface.
	hash := sha256.New()

	// io.Copy efficiently copies data from the file to the hash function
	if _, err = io.Copy(hash, file); err != nil {
		return "", fmt.Errorf("failed to copy file content to hash: %w", err)
	}

	// get the resulting hash sum as a byte slice and format it as hex
	// hash.Sum(nil) appends the hash to a nil slice
	hashInBytes := hash.Sum(nil)
	hashString := fmt.Sprintf("%x", hashInBytes)

	return hashString, err // err will be nil unless file.Close() failed
}
