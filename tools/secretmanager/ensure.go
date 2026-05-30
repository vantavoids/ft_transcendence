package main

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"runtime"
	"strings"
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

	fmt.Println()
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
		if err := installSOPS(userArch, path); err != nil {
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
		if err := installAGE(userArch, path); err != nil {
			return err
		}
	}

	return nil
}

const secretDirPath = "../../secrets/"
const secretFilePath = "../../secrets/age.key"

func ensureAGESecret() error {

	fmt.Println()

	// check if the secrets dir is present else make it
	err := os.Mkdir(secretDirPath, 0755)
	if err != nil && !os.IsExist(err) {
		return err
	}

	// check if age.key is inside it else generate it
	// and display a warning
	if fileExists(secretFilePath) {
		fmt.Println("✅ AGE key found.")
		return nil
	} else {
		fmt.Println("⚠️ AGE key not found, generating a new one inside the secrets directory.")
		if err := generateAGEKey(secretDirPath); err != nil {
			return err
		}

		fmt.Println("✅ The new public key has been added to the list of trusted keys inside .sops.yaml.")
		fmt.Printf("\n⚠️ Open a PR containing only the updated .sops.yaml file.\n\n")
		fmt.Println("A trusted developer must then refresh the encrypted env files with your public key.")
		fmt.Println("You will not be able to decrypt env files until that PR is merged and secrets are updated.")
	}

	return nil
}

func generateAGEKey(secretDirPath string) error {

	cmd := exec.Command(toolsDir+"age-keygen", "-o", secretFilePath)
	err := cmd.Run()
	if err != nil {
		return err
	}

	publicKey, err := fetchPublicKey(secretFilePath)
	if err != nil {
		return err
	}

	fmt.Printf("\nNew public key: %s\n\n", publicKey)

	if err := addKeyToYaml(publicKey); err != nil {
		return nil
	}

	return nil
}

func fetchPublicKey(secretFilePath string) (string, error) {

	file, err := os.Open(secretFilePath)
	if err != nil {
		return "", err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)

	lineNum := 0
	for scanner.Scan() {
		lineNum++

		if lineNum == 2 {
			line := scanner.Text()

			const prefix = "# public key: "
			if !strings.HasPrefix(line, prefix) {
				return "", fmt.Errorf("invalid public key line")
			}

			publicKey := strings.TrimSpace(strings.TrimPrefix(line, prefix))
			return publicKey, nil
		}
	}

	if err := scanner.Err(); err != nil {
		return "", err
	}

	return "", fmt.Errorf("second line not found")
}

func addKeyToYaml(publicKey string) error {

	line := "          - " + publicKey + "\n"

	out, err := os.OpenFile(".sops.yaml", os.O_APPEND|os.O_WRONLY, 0644)
	if err != nil {
		return err
	}
	defer out.Close()

	_, err = out.WriteString(line)
	if err != nil {
		return err
	}
	return nil
}
