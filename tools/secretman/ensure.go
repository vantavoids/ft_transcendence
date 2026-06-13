package main

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"os/user"
	"strings"
)

func checkOS(userOS string, userArch string) error {

	if userOS != "linux" && userOS != "darwin" {
		fmt.Printf("❌ Operating system: %v\n", userOS)
		return fmt.Errorf("your system must be Linux or Darwin (MacOS) in order to use secretman, exiting")
	} else {
		fmt.Printf("✅ Operating system: %v\n", userOS)
	}

	if userArch != "amd64" && userArch != "arm64" {
		fmt.Printf("❌ System architecture: %v\n", userArch)
		return fmt.Errorf("your system must be amd64 or arm64 in order to use secretman, exiting")
	} else {
		fmt.Printf("✅ System architecture: %v\n", userArch)
	}
	fmt.Println()

	return nil
}

const keysDirPath = secretmanDirPath + ".keys/"
const keyFilePath = secretmanDirPath + ".keys/age.key"

func ensureAGESecret() error {

	fmt.Println()

	// check if the keys dir is present else make it
	err := os.Mkdir(keysDirPath, 0755)
	if err != nil && !os.IsExist(err) {
		return err
	}

	// check if age.key is inside it else generate it
	// and display a warning
	if fileExists(keyFilePath) {
		fmt.Println("✅ AGE key found.")
		publicKey, err := isInSopsYaml()
		if err != nil {
			return err
		}
		if publicKey != "" {
			fmt.Println("\n⚠️ AGE key not found in .sops.yaml, adding it.")

			if err := addKeyToSopsYaml(publicKey); err != nil {
				return err
			}
			displayKeyWarning()
		}
	} else {
		fmt.Println("⚠️ AGE key not found, generating a new one inside the keys directory.")
		if err := generateAGEKey(); err != nil {
			return err
		}

		fmt.Println("✅ The new public key has been added to the list of trusted keys inside .sops.yaml.")
		displayKeyWarning()
	}

	return nil
}

func displayKeyWarning() {

	fmt.Printf("\n⚠️ Open a PR containing only the updated .sops.yaml file.\n\n")
	fmt.Println("A trusted developer must then refresh the encrypted env files with your public key.")
	fmt.Println("You will not be able to decrypt env files until that PR is merged and secrets are updated.")
}

const sopsYamlPath = ".sops.yaml"

func isInSopsYaml() (string, error) {

	publicKey, err := fetchPublicKey()
	if err != nil {
		return "", err
	}

	file, err := os.Open(sopsYamlPath)
	if err != nil {
		return "", err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)

	for scanner.Scan() {
		if strings.Contains(scanner.Text(), publicKey) {
			return "", nil
		}
	}

	if err := scanner.Err(); err != nil {
		return "", err
	}

	return publicKey, nil
}

func generateAGEKey() error {

	paths, err := ensureToolsPaths(true)
	if err != nil {
		return err
	}

	cmd := exec.Command(paths.AGE, "-o", keyFilePath)
	err = cmd.Run()
	if err != nil {
		return err
	}

	publicKey, err := fetchPublicKey()
	if err != nil {
		return err
	}

	fmt.Printf("\nNew public key: %s\n\n", publicKey)

	if err := addKeyToSopsYaml(publicKey); err != nil {
		return err
	}

	return nil
}

func fetchPublicKey() (string, error) {

	file, err := os.Open(keyFilePath)
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

func addKeyToSopsYaml(publicKey string) error {

	user, err := user.Current()
	if err != nil {
		return err
	}
	hostname, err := os.Hostname()
	if err != nil {
		return err
	}
	author := "          # " + user.Name + "@" + hostname + "\n"
	line := "          - " + publicKey + "\n"

	out, err := os.OpenFile(sopsYamlPath, os.O_APPEND|os.O_WRONLY, 0644)
	if err != nil {
		return err
	}
	defer out.Close()

	_, err = out.WriteString(author)
	if err != nil {
		return err
	}
	_, err = out.WriteString(line)
	if err != nil {
		return err
	}
	return nil
}
