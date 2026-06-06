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

const rootPath = "../../"
const secretmanDirPath = rootPath + "infra/secretman/"
const secretsDirPath = secretmanDirPath + "secrets/"

var secretFiles = []SecretFile{
	{
		Encrypted: secretsDirPath + "root.env.crypt",
		Plaintext: rootPath + ".env",
	},
	{
		Encrypted: secretsDirPath + "front.env.crypt",
		Plaintext: rootPath + "frontend/.env",
	},
	{
		Encrypted: secretsDirPath + "auth.env.crypt",
		Plaintext: rootPath + "services/auth/.env",
	},
	{
		Encrypted: secretsDirPath + "chat.env.crypt",
		Plaintext: rootPath + "services/chat/.env",
	},
	{
		Encrypted: secretsDirPath + "gateway.env.crypt",
		Plaintext: rootPath + "services/gateway/.env",
	},
	{
		Encrypted: secretsDirPath + "guild.env.crypt",
		Plaintext: rootPath + "services/guild/.env",
	},
	{
		Encrypted: secretsDirPath + "notification.env.crypt",
		Plaintext: rootPath + "services/notification/.env",
	},
	{
		Encrypted: secretsDirPath + "user.env.crypt",
		Plaintext: rootPath + "services/user/.env",
	},
}

func encryptAllSecrets() error {

	if err := os.MkdirAll(secretsDirPath, 0755); err != nil {
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

		fmt.Println("➡️ Encrypting", secret.Plaintext[4:])

		cmd := exec.Command(
			toolsDir+"sops",
			"encrypt",
			"--filename-override", secret.Encrypted,
			"--input-type", "dotenv",
			"--output-type", "json",
			secret.Plaintext,
		)

		out, err := cmd.CombinedOutput()
		if err != nil {
			return fmt.Errorf("SOPS encrypt failed for %s: %w\n%s", secret.Plaintext, err, string(out))
		}

		if err := os.WriteFile(secret.Encrypted, out, 0644); err != nil {
			return fmt.Errorf("failed to write encrypted file %s: %w", secret.Encrypted, err)
		}

		fmt.Println("✅ Encrypted in", secret.Encrypted[4:])
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

		fmt.Println("➡️ Decrypting", secret.Encrypted[4:])

		cmd := exec.Command(
			toolsDir+"sops",
			"decrypt",
			"--input-type", "json",
			"--output-type", "dotenv",
			secret.Encrypted,
		)

		cmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)

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

		fmt.Println("✅ Decrypted in", secret.Plaintext[4:])
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
