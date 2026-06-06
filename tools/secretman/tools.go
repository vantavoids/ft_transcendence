package main

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

	currentKey := userOS + "-" + userArch

	url := sopsAssets[currentKey].URL
	checksum := sopsAssets[currentKey].SHA256

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

func installAGE(userOS string, userArch string, path string) error {

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

	currentKey := userOS + "-" + userArch

	url := ageAssets[currentKey].URL
	checksum := ageAssets[currentKey].SHA256

	err := downloadFile(url, path, checksum)
	if err != nil {
		return err
	}

	err = extractTarGz(path)
	if err != nil {
		return err
	}

	err = changePerm(toolsDir+"age-keygen", 0755)
	if err != nil {
		return err
	}

	return nil
}
