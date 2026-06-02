package main

import (
	"bufio"
	"fmt"
	"os"
	"strconv"
	"strings"
)

func askForConfirmation(question string) (bool, error) {

	reader := bufio.NewReader(os.Stdin)

	for {
		fmt.Printf("%s [y/n]: ", question)

		response, err := reader.ReadString('\n')
		if err != nil {
			return false, fmt.Errorf("❌ Invalid entry: %v", err)
		}

		response = strings.ToLower(strings.TrimSpace(response))

		if response == "y" || response == "yes" {
			return true, nil
		} else if response == "n" || response == "no" {
			return false, nil
		}
	}
}

func askForTargets() (map[int]bool, error) {

	reader := bufio.NewReader(os.Stdin)

	fmt.Printf("➡️ Which .env file do you want to encrypt:\n")
	fmt.Printf("1. root\n2. frontend\n3. auth\n")
	fmt.Printf("4. chat\n5. gateway\n6. guild\n")
	fmt.Printf("7. notification\n8. user\n")
	fmt.Printf("\n➡️ Type index numbers separated by space: ")

	response, err := reader.ReadString('\n')
	if err != nil {
		return nil, fmt.Errorf("❌ Invalid index: %v", err)
	}

	list := strings.Fields(response)
	if len(list) == 0 {
		return nil, fmt.Errorf("❌ No index picked")
	}

	indexMap, err := makeValidMapFromSlice(list, len(secretFiles))
	if err != nil {
		return nil, err
	}

	return indexMap, nil
}

func makeValidMapFromSlice(indexSlice []string, maxIndex int) (map[int]bool, error) {

	indexMap := make(map[int]bool)

	for _, val := range indexSlice {
		index, err := strconv.Atoi(val)
		if err != nil {
			return nil, fmt.Errorf("❌ Invalid index: %v", err)
		}
		if index < 1 || index > maxIndex {
			return nil, fmt.Errorf("❌ Invalid index: %v", val)
		}
		indexMap[index-1] = true
	}
	return indexMap, nil
}
