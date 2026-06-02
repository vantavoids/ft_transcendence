package main

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
)

type SecretFile struct {
	Encrypted string
	Plaintext string
}

var secretFiles = []SecretFile{
	{
		Encrypted: "secrets/root.env.crypt",
		Plaintext: "../../.env",
	},
	{
		Encrypted: "secrets/front.env.crypt",
		Plaintext: "../../frontend/.env",
	},
	{
		Encrypted: "secrets/auth.env.crypt",
		Plaintext: "../../services/auth/.env",
	},
	{
		Encrypted: "secrets/chat.env.crypt",
		Plaintext: "../../services/chat/.env",
	},
	{
		Encrypted: "secrets/gateway.env.crypt",
		Plaintext: "../../services/gateway/.env",
	},
	{
		Encrypted: "secrets/guild.env.crypt",
		Plaintext: "../../services/guild/.env",
	},
	{
		Encrypted: "secrets/notification.env.crypt",
		Plaintext: "../../services/notification/.env",
	},
	{
		Encrypted: "secrets/user.env.crypt",
		Plaintext: "../../services/user/.env",
	},
}

func encryptAllSecrets() error {

	if err := os.MkdirAll("secrets", 0755); err != nil {
		return err
	}

	targets, err := askForTargets()
	if err != nil {
		return err
	}
	fmt.Println()

	for index, secret := range secretFiles {

		if !targets[index] {
			continue
		}

		fmt.Println("➡️ Encrypting", secret.Plaintext)

		cmd := exec.Command(
			toolsDir+"sops",
			"encrypt",
			"--filename-override", secret.Encrypted,
			"--input-type", "dotenv",
			"--output-type", "dotenv",
			secret.Plaintext,
		)

		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("SOPS encrypt failed for %s: %w\n%s", secret.Plaintext, err, string(out))
		}

		if err := os.WriteFile(secret.Encrypted, out, 0644); err != nil {
			return fmt.Errorf("failed to write encrypted file %s: %w", secret.Encrypted, err)
		}

		fmt.Println("✅ Encrypted in", secret.Encrypted)
	}

	return nil
}

func decryptAllSecrets() error {

	overwrite, err := askForConfirmation("⚠️ Overwrite existing .env files")
	if err != nil {
		return err
	}
	fmt.Println()

	for _, secret := range secretFiles {
		if fileExists(secret.Plaintext) && !overwrite {
			fmt.Println("➡️ Skipping existing .env file:", secret.Plaintext)
			continue
		}

		if !fileExists(secret.Encrypted) {
			fmt.Println("⚠️ Skipping missing encrypted file:", secret.Encrypted)
			continue
		}

		fmt.Println("➡️ Decrypting", secret.Encrypted)

		cmd := exec.Command(
			toolsDir+"sops",
			"decrypt",
			"--input-type", "dotenv",
			"--output-type", "dotenv",
			secret.Encrypted,
		)

		cmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keysFilePath)

		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("SOPS decrypt failed for %s: %w\n%s", secret.Encrypted, err, string(out))
		}

		if err := os.MkdirAll(filepath.Dir(secret.Plaintext), 0755); err != nil {
			return fmt.Errorf("failed to create directory for %s: %w", secret.Plaintext, err)
		}

		if err := os.WriteFile(secret.Plaintext, out, 0600); err != nil {
			return fmt.Errorf("failed to write plaintext file %s: %w", secret.Plaintext, err)
		}

		fmt.Println("✅ Decrypted in", secret.Plaintext)
	}

	return nil
}

func refreshAllSecrets() error {

	for _, secret := range secretFiles {
		if !fileExists(secret.Encrypted) {
			fmt.Println("⚠️ Skipping missing encrypted file:", secret.Encrypted)
			continue
		}

		fmt.Println("➡️ Refreshing", secret.Encrypted)

		cmd := exec.Command(
			toolsDir+"sops",
			"updatekeys",
			"--input-type", "dotenv",
			secret.Encrypted,
		)

		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("SOPS refresh failed for %s: %w\n%s", secret.Encrypted, err, string(out))
		}

		fmt.Println("✅ Refreshed", secret.Encrypted)
	}
	return nil
}
