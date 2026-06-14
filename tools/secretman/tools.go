package main

import (
	"bufio"
	"errors"
	"fmt"
	"io"
	"os"
	"os/exec"
	"runtime"
	"strings"
)

const userOS = runtime.GOOS
const userArch = runtime.GOARCH

const toolsDir = ".tools/"

type toolPaths struct {
	SOPS string
	AGE  string
}

var cachedToolPaths *toolPaths

func ensureToolsPaths(setup bool) (*toolPaths, error) {

	// create a cache dir .tools for secretman if missing
	err := os.MkdirAll(toolsDir, 0755)
	if err != nil && !os.IsExist(err) {
		return nil, err
	}

	if cachedToolPaths != nil {
		return cachedToolPaths, nil
	}

	paths, err := resolveToolPaths(setup)
	if err != nil {
		return nil, err
	}

	cachedToolPaths = &paths
	return cachedToolPaths, nil
}

func resolveToolPaths(setup bool) (toolPaths, error) {

	var paths toolPaths

	SOPSPath, err := resolveSOPSPath()
	if err != nil {
		return paths, err
	}
	if setup {
		AGEPath, err := resolveAGEPath()
		if err != nil {
			return paths, err
		}
		paths.AGE = AGEPath
	}

	paths.SOPS = SOPSPath

	return paths, nil
}

func resolveSOPSPath() (string, error) {

	globalPath, err := exec.LookPath("sops")
	if err == nil {
		fmt.Println("✅ SOPS binary found in global PATH.")
		return globalPath, nil
	}

	localPath := toolsDir + "sops"

	if fileExists(localPath) {
		fmt.Println("✅ SOPS binary found in secretman tools.")
		return localPath, nil
	}

	fmt.Println("⚠️ SOPS binary not found, downloading it.")
	if err := installSOPS(userOS, userArch, localPath); err != nil {
		return "", err
	}

	return localPath, nil
}

func resolveAGEPath() (string, error) {

	fmt.Println()

	globalPath, err := exec.LookPath("age-keygen")
	if err == nil {
		fmt.Println("✅ AGE binary found in global PATH.")
		return globalPath, nil
	}

	localPath := toolsDir + "age-keygen"

	if fileExists(localPath) {
		fmt.Println("✅ AGE binary found in secretman tools.")
		return localPath, nil
	}

	fmt.Println("⚠️ AGE binary not found, downloading it.")
	if err := installAGE(userOS, userArch, toolsDir+"age.tar.gz"); err != nil {
		return "", err
	}

	return localPath, nil
}

type toolAsset struct {
	URL    string
	SHA256 string
}

func installSOPS(userOS string, userArch string, path string) error {

	sopsAssets := map[string]toolAsset{
		"linux-arm64": {
			URL:    "https://github.com/getsops/sops/releases/download/v3.13.1/sops-v3.13.1.linux.arm64",
			SHA256: "19576fb1734dbf8fb77eda0cf0f3a2218f99bf4d33b814318e5e10d6babb9820",
		},
		"linux-amd64": {
			URL:    "https://github.com/getsops/sops/releases/download/v3.13.1/sops-v3.13.1.linux.amd64",
			SHA256: "620a9d7e3352ababeca6908cea24a6e8b14ce89a448ddbd3f94f1ef3398f470a",
		},
		"darwin-arm64": {
			URL:    "https://github.com/getsops/sops/releases/download/v3.13.1/sops-v3.13.1.darwin.arm64",
			SHA256: "a2c0dd37eb031068af6ef213b78cfa67b7f1afd76c2e5cc404257f42bbc8367d",
		},
		"darwin-amd64": {
			URL:    "https://github.com/getsops/sops/releases/download/v3.13.1/sops-v3.13.1.darwin.amd64",
			SHA256: "dad79d1b1dea767ca38ffaa50e10330a3e807dd13c853ef9c880567acef4f1ef",
		},
	}

	key := userOS + "-" + userArch

	url := sopsAssets[key].URL
	checksum := sopsAssets[key].SHA256

	err := downloadFile(url, path, checksum)
	if err != nil {
		return err
	}

	err = changePerm(path, 0755)
	if err != nil {
		return err
	}

	return nil
}

func installAGE(userOS string, userArch string, archive string) error {

	ageAssets := map[string]toolAsset{
		"linux-arm64": {
			URL:    "https://github.com/FiloSottile/age/releases/download/v1.3.1/age-v1.3.1-linux-arm64.tar.gz",
			SHA256: "c6878a324421b69e3e20b00ba17c04bc5c6dab0030cfe55bf8f68fa8d9e9093a",
		},
		"linux-amd64": {
			URL:    "https://github.com/FiloSottile/age/releases/download/v1.3.1/age-v1.3.1-linux-amd64.tar.gz",
			SHA256: "bdc69c09cbdd6cf8b1f333d372a1f58247b3a33146406333e30c0f26e8f51377",
		},
		"darwin-arm64": {
			URL:    "https://github.com/FiloSottile/age/releases/download/v1.3.1/age-v1.3.1-darwin-arm64.tar.gz",
			SHA256: "01120ea2cbf0463d4c6bd767f99f3271bbed1cdc8a9aa718a76ba1fe4f01998b",
		},
		"darwin-amd64": {
			URL:    "https://github.com/FiloSottile/age/releases/download/v1.3.1/age-v1.3.1-darwin-amd64.tar.gz",
			SHA256: "2b233301ad21ab7b1eabd9ae1198a164005fa4928fcdd745d47c39f8593209d7",
		},
	}

	key := userOS + "-" + userArch

	url := ageAssets[key].URL
	checksum := ageAssets[key].SHA256

	err := downloadFile(url, archive, checksum)
	if err != nil {
		return err
	}

	err = extractTarGz(archive, "age/age-keygen")
	if err != nil {
		return err
	}

	err = changePerm(toolsDir+"age-keygen", 0755)
	if err != nil {
		return err
	}

	return nil
}

const gitPath string = rootPath + ".git/"

func ensureBetterLeaks() error {

	// check that .git/ exists
	if !fileExists(gitPath) {
		err := fmt.Errorf("❌ Missing .git in root directory, cannot add BetterLeaks hook")
		return err
	}

	// check that betterleaks is installed or install it
	binPath, err := resolveBetterLeaksToolPath()
	if err != nil {
		return err
	}

	// create .git/hooks/betterleaks & .git/hooks/pre-commit if missing
	hooksDirPath := gitPath + "hooks/"
	if err := ensureBetterLeaksHook(hooksDirPath, binPath); err != nil {
		return err
	}
	if err := ensurePreCommitHook(hooksDirPath); err != nil {
		return err
	}

	return nil
}

func resolveBetterLeaksToolPath() (string, error) {

	fmt.Println()

	globalPath, err := exec.LookPath("betterleaks")
	if err == nil {
		fmt.Println("✅ BetterLeaks binary found in global PATH.")
		return globalPath, nil
	}

	localPath := toolsDir + "betterleaks"
	if fileExists(localPath) {
		fmt.Println("✅ BetterLeaks binary found in secretman tools.")
		return localPath, nil
	}

	fmt.Println("⚠️ BetterLeaks binary not found, downloading it.")
	if err := installBetterLeaks(userOS, userArch, toolsDir+"betterleaks.tar.gz"); err != nil {
		return "", err
	}

	return localPath, nil
}

func installBetterLeaks(userOS string, userArch string, archive string) error {

	ageAssets := map[string]toolAsset{
		"linux-arm64": {
			URL:    "https://github.com/betterleaks/betterleaks/releases/download/v1.5.0/betterleaks_1.5.0_linux_arm64.tar.gz",
			SHA256: "f4e89eccde1cdf0cf048748876757e56705d21f122fa4284a4b84803da288608",
		},
		"linux-amd64": {
			URL:    "https://github.com/betterleaks/betterleaks/releases/download/v1.5.0/betterleaks_1.5.0_linux_x64.tar.gz",
			SHA256: "b883e8c61a3a14c90ff46a08c203cf88fe340ed88251d4c049db5530ec0ac54b",
		},
		"darwin-arm64": {
			URL:    "https://github.com/betterleaks/betterleaks/releases/download/v1.5.0/betterleaks_1.5.0_darwin_arm64.tar.gz",
			SHA256: "a341e534f152bd10fa8309d74e2ab7eadb634f16ddeb7c3f43ca54f8a016905b",
		},
		"darwin-amd64": {
			URL:    "https://github.com/betterleaks/betterleaks/releases/download/v1.5.0/betterleaks_1.5.0_darwin_x64.tar.gz",
			SHA256: "bbbbf362ddd0a0c5d37633707206be92501ba5f3291ea45af0c6ab3980ec693d",
		},
	}

	key := userOS + "-" + userArch

	url := ageAssets[key].URL
	checksum := ageAssets[key].SHA256

	err := downloadFile(url, archive, checksum)
	if err != nil {
		return err
	}

	target := "betterleaks"

	err = extractTarGz(archive, target)
	if err != nil {
		return err
	}

	err = changePerm(toolsDir+target, 0755)
	if err != nil {
		return err
	}

	return nil
}

func ensureBetterLeaksHook(hooksDirPath string, binPath string) error {

	if err := os.MkdirAll(hooksDirPath, 0755); err != nil {
		return err
	}

	betterHookPath := hooksDirPath + "betterleaks"

	betterHookFile, err := os.OpenFile(betterHookPath, os.O_CREATE|os.O_EXCL|os.O_WRONLY, 0755)
	if err != nil {
		if errors.Is(err, os.ErrExist) {
			fmt.Println("✅ BetterLeaks hook found in .git directory.")
			return nil
		}
		return err
	}
	defer betterHookFile.Close()

	fmt.Println("⚠️ BetterLeaks hook not found in .git directory, adding it.")

	hookContent := "#!/bin/sh\n\nexec \"" + binPath + "\" git . --pre-commit --staged -v\n"

	if _, err := io.Copy(betterHookFile, strings.NewReader(hookContent)); err != nil {
		return err
	}

	return nil
}

func ensurePreCommitHook(hooksDirPath string) error {

	preCommitHookPath := hooksDirPath + "pre-commit"

	if !fileExists(preCommitHookPath) {
		shebang := []byte("#!/bin/sh\n")
		if err := os.WriteFile(preCommitHookPath, shebang, 0755); err != nil {
			return err
		}
	}

	preCommitHookFile, err := os.OpenFile(preCommitHookPath, os.O_CREATE|os.O_RDWR, 0755)
	if err != nil {
		return err
	}
	defer preCommitHookFile.Close()

	scanner := bufio.NewScanner(preCommitHookFile)

	for scanner.Scan() {
		line := scanner.Text()
		if strings.Contains(line, "# secretman betterleaks hook") || strings.Contains(line, ".git/hooks/betterleaks") {
			fmt.Println("✅ BetterLeaks found in pre-commit hook.")
			return nil
		}
	}
	if err := scanner.Err(); err != nil {
		return err
	}

	fmt.Println("⚠️ BetterLeaks not found in pre-commit hook, adding it.")

	hookContent := `
# secretman betterleaks hook

repo_root="$(git rev-parse --show-toplevel)" || exit 1
cd "$repo_root" || exit 1

if [ -x ".git/hooks/betterleaks" ]; then
	.git/hooks/betterleaks || exit $?
fi
`

	if _, err := io.Copy(preCommitHookFile, strings.NewReader(hookContent)); err != nil {
		return err
	}

	return nil
}
