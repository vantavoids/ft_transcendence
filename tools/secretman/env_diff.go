package main

import (
	"bufio"
	"bytes"
	"fmt"
	"maps"
	"os"
	"os/exec"
	"strings"
)

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

func makeMapFromDiff(oldMap map[string]string, newMap map[string]string) map[string]string {

	diffMap := maps.Clone(newMap)

	for oldKey, oldValue := range oldMap {
		newValue, exists := newMap[oldKey]
		if !exists {
			// removed key
			diffMap[oldKey] = ""
			continue
		}

		if newValue == oldValue {
			// unchanged key
			delete(diffMap, oldKey)
		}
	}

	return diffMap
}
