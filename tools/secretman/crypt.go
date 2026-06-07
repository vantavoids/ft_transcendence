package main

import (
	"bufio"
	"bytes"
	"encoding/json"
	"fmt"
	"maps"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

type secretFile struct {
	Encrypted string
	Plaintext string
}

const rootPath = "../../"
const secretmanDirPath = rootPath + "infra/secretman/"
const secretsDirPath = secretmanDirPath + "secrets/"

var secretFiles = []secretFile{
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
			fmt.Println("⚠️ Skipping missing env file:", secret.Plaintext[4:])
			continue
		}

		diffMap, err := checkEncryptedFileForDiff(secret, paths)
		if err != nil {
			return err
		}
		if len(diffMap) == 0 {
			fmt.Println("➡️ No diff, skipping env file:", secret.Plaintext[4:])
			continue
		}

		fmt.Println("➡️ Encrypting", secret.Plaintext[4:])

		for key, value := range diffMap {
			if value == "" {
				removeCmd := exec.Command(
					paths.SOPS,
					"unset",
					"--input-type", "json",
					"--output-type", "json",
					secret.Encrypted,
					fmt.Sprintf(`["%s"]`, key),
				)
				removeCmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)
				out, err := removeCmd.CombinedOutput()
				if err != nil {
					return fmt.Errorf("❌ SOPS unset failed for %s: %w\n%s", secret.Encrypted, err, string(out))
				}
			} else {
				encodedValue, err := json.Marshal(value)
				if err != nil {
					return fmt.Errorf("❌ failed to encode value for key %s: %w", key, err)
				}

				updateCmd := exec.Command(
					paths.SOPS,
					"set",
					"--input-type", "json",
					"--output-type", "json",
					secret.Encrypted,
					fmt.Sprintf(`["%s"]`, key),
					string(encodedValue),
				)
				updateCmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)
				out, err := updateCmd.CombinedOutput()
				if err != nil {
					return fmt.Errorf("❌ SOPS set failed for %s: %w\n%s", secret.Encrypted, err, string(out))
				}
			}
		}

		fmt.Println("✅ Encrypted in", secret.Encrypted[4:])
	}

	return nil
}

func checkEncryptedFileForDiff(secret secretFile, paths *toolPaths) (map[string]string, error) {

	decryptCmd := exec.Command(
		paths.SOPS,
		"decrypt",
		"--input-type", "json",
		"--output-type", "dotenv",
		secret.Encrypted,
	)

	decryptCmd.Env = append(os.Environ(), "SOPS_AGE_KEY_FILE="+keyFilePath)

	oldEnv, err := decryptCmd.CombinedOutput()
	if err != nil {
		return nil, fmt.Errorf("❌ SOPS decrypt failed for %s: %w\n%s", secret.Encrypted, err, string(oldEnv))
	}

	newEnv, err := os.ReadFile(secret.Plaintext)
	if err != nil {
		return nil, err
	}

	diffMap := sliceDiffToMap(oldEnv, newEnv)

	return diffMap, nil
}

func sliceDiffToMap(oldEnv []byte, newEnv []byte) map[string]string {

	oldMap := sliceToMap(oldEnv)
	newMap := sliceToMap(newEnv)

	if maps.Equal(oldMap, newMap) {
		return nil
	}

	diffMap := makeMapFromDiff(oldMap, newMap)

	return diffMap
}

func sliceToMap(envSlice []byte) map[string]string {

	retMap := make(map[string]string)

	scanner := bufio.NewScanner(bytes.NewReader(envSlice))

	for scanner.Scan() {
		line := scanner.Text()

		var key, value string
		if i := strings.Index(line, "="); i > 0 {
			key, value = line[:i], line[i+1:]
		} else {
			continue
		}

		retMap[key] = value
	}

	return retMap
}

func makeMapFromDiff(oldMap map[string]string,
	newMap map[string]string) map[string]string {

	diffMap := maps.Clone(newMap)

	for oldKey, oldValue := range oldMap {
		newValue := newMap[oldKey]

		if newValue == oldValue {
			// unchanged value
			delete(diffMap, oldKey)
		} else {
			// changed value
			if newValue == "" && oldValue != "" {
				// removed value
				diffMap[oldKey] = ""
			}
			continue
		}
	}

	return diffMap
}

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
			fmt.Println("➡️ Skipping existing .env file:", secret.Plaintext[4:])
			continue
		}

		fmt.Println("➡️ Decrypting", secret.Encrypted[4:])

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

		fmt.Println("✅ Decrypted in", secret.Plaintext[4:])
	}

	return nil
}

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
