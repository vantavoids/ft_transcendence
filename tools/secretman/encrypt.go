package main

import (
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
)

func encryptSecrets() error {

	if err := os.MkdirAll(secretsDirPath, 0755); err != nil {
		return err
	}

	targets, err := askForTargets(blueStr("encrypt"))
	if err != nil {
		return err
	}

	fmt.Println()
	paths, err := ensureToolsPaths(false)
	if err != nil {
		return err
	}
	fmt.Println()

	for index, secret := range secretFiles {
		if !targets[index] {
			continue
		}

		if !fileExists(secret.Plaintext) {
			fmt.Println("⚠️ Skipping missing env file:", displayPath(secret.Plaintext))
			continue
		}

		encrypted, err := encryptOneSecret(secret, paths)
		if err != nil {
			return err
		}

		if encrypted {
			fmt.Println("✅ Encrypted in", displayPath(secret.Encrypted))
		}
	}

	return nil
}

func encryptOneSecret(secret secretFile, paths *toolPaths) (bool, error) {

	if fileExists(secret.Encrypted) {
		diffMap, err := checkEncryptedFileForDiff(secret, paths)
		if err != nil {
			return false, err
		}

		if len(diffMap) == 0 {
			fmt.Println("➡️ No diff, skipping env file:", displayPath(secret.Plaintext))
			return false, nil
		}

		fmt.Println("➡️ Encrypting", displayPath(secret.Plaintext))

		if err := applyEncryptedDiff(secret, paths, diffMap); err != nil {
			return false, err
		}

		return true, nil
	}

	fmt.Println("➡️ Encrypting", displayPath(secret.Plaintext))

	if err := encryptPlainEnv(secret, paths); err != nil {
		return false, err
	}

	return true, nil
}

func encryptPlainEnv(secret secretFile, paths *toolPaths) error {

	cmd := exec.Command(
		paths.SOPS,
		"encrypt",
		"--filename-override", secret.Encrypted,
		"--input-type", "dotenv",
		"--output-type", "json",
		secret.Plaintext,
	)

	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("❌ SOPS encrypt failed for %s: %w\n%s",
			secret.Plaintext, err, string(out))
	}

	if err := os.WriteFile(secret.Encrypted, out, 0644); err != nil {
		return fmt.Errorf("❌ failed to write encrypted file %s: %w", secret.Encrypted, err)
	}

	return nil
}

func applyEncryptedDiff(secret secretFile, paths *toolPaths, diffMap map[string]string) error {

	for key, value := range diffMap {
		if value == "" {
			if err := unsetEncryptedKey(secret, paths, key); err != nil {
				return err
			}
			continue
		}

		if err := setEncryptedKey(secret, paths, key, value); err != nil {
			return err
		}
	}

	return nil
}

func unsetEncryptedKey(secret secretFile, paths *toolPaths, key string) error {

	cmd := exec.Command(
		paths.SOPS,
		"unset",
		"--input-type", "json",
		"--output-type", "json",
		secret.Encrypted,
		fmt.Sprintf(`["%s"]`, key),
	)

	cmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)

	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("❌ SOPS unset failed for %s: %w\n%s",
			secret.Encrypted, err, string(out))
	}

	return nil
}

func setEncryptedKey(secret secretFile, paths *toolPaths, key string, value string) error {

	encodedValue, err := json.Marshal(value)
	if err != nil {
		return fmt.Errorf("❌ failed to encode value for key %s: %w", key, err)
	}

	cmd := exec.Command(
		paths.SOPS,
		"set",
		"--input-type", "json",
		"--output-type", "json",
		secret.Encrypted,
		fmt.Sprintf(`["%s"]`, key),
		string(encodedValue),
	)

	cmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)

	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("❌ SOPS set failed for %s: %w\n%s",
			secret.Encrypted, err, string(out))
	}

	return nil
}
