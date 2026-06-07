package main

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
)

func decryptSecrets() error {

	targets, err := askForTargets(redStr("decrypt"))
	if err != nil {
		return err
	}
	fmt.Println()

	overwrite := false

	for index, secret := range secretFiles {
		if targets[index] && fileExists(secret.Plaintext) {
			overwrite, err = askForConfirmation("⚠️ Overwrite existing .env files")
			if err != nil {
				return err
			}
			fmt.Println()
			break
		}
	}

	paths, err := ensureToolsPaths(false)
	if err != nil {
		return err
	}
	fmt.Println()

	for index, secret := range secretFiles {

		if !targets[index] {
			continue
		}

		if !fileExists(secret.Encrypted) {
			fmt.Println("⚠️ Skipping missing encrypted file:", secret.Encrypted)
			continue
		}

		if fileExists(secret.Plaintext) && !overwrite {
			fmt.Println("➡️ Skipping existing .env file:", displayPath(secret.Plaintext))
			continue
		}

		fmt.Println("➡️ Decrypting", displayPath(secret.Encrypted))

		cmd := exec.Command(
			paths.SOPS,
			"decrypt",
			"--input-type", "json",
			"--output-type", "dotenv",
			secret.Encrypted,
		)

		cmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)

		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("❌ SOPS decrypt failed for %s: %w\n%s", secret.Encrypted, err, string(out))
		}

		if err := os.MkdirAll(filepath.Dir(secret.Plaintext), 0755); err != nil {
			return fmt.Errorf("❌ failed to create directory for %s: %w", secret.Plaintext, err)
		}

		if err := os.WriteFile(secret.Plaintext, out, 0600); err != nil {
			return fmt.Errorf("❌ failed to write plaintext file %s: %w", secret.Plaintext, err)
		}

		fmt.Println("✅ Decrypted in", displayPath(secret.Plaintext))
	}

	return nil
}
