package main

import (
	"fmt"
	"os"
	"os/exec"
)

func refreshSecrets() error {

	fmt.Printf("➡️ Starting secrets %s\n\n", purpleStr("refresh"))

	paths, err := ensureToolsPaths(false)
	if err != nil {
		return err
	}

	fmt.Println()

	for _, secret := range secretFiles {
		if !fileExists(secret.Encrypted) {
			fmt.Println("⚠️ Skipping missing encrypted file:", secret.Encrypted)
			continue
		}

		fmt.Println("➡️ Refreshing", secret.Encrypted)

		cmd := exec.Command(
			paths.SOPS,
			"updatekeys",
			"-y",
			"--input-type", "json",
			secret.Encrypted,
		)

		cmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)

		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("❌ SOPS refresh failed for %s: %w\n%s", secret.Encrypted, err, string(out))
		}

		fmt.Println("✅ Refreshed", secret.Encrypted)
	}
	return nil
}
