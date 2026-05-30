package main

type toolAsset struct {
	URL    string
	SHA256 string
}

func installSOPS(userArch string, path string) error {

	sopsAssets := map[string]toolAsset{
		"arm64": {
			URL:    "https://github.com/getsops/sops/releases/download/v3.13.1/sops-v3.13.1.linux.arm64",
			SHA256: "19576fb1734dbf8fb77eda0cf0f3a2218f99bf4d33b814318e5e10d6babb9820",
		},
		"amd64": {
			URL:    "https://github.com/getsops/sops/releases/download/v3.13.1/sops-v3.13.1.linux.amd64",
			SHA256: "620a9d7e3352ababeca6908cea24a6e8b14ce89a448ddbd3f94f1ef3398f470a",
		},
	}

	url := sopsAssets[userArch].URL
	checksum := sopsAssets[userArch].SHA256

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

func installAGE(userArch string, path string) error {

	ageAssets := map[string]toolAsset{
		"arm64": {
			URL:    "https://github.com/FiloSottile/age/releases/download/v1.3.1/age-v1.3.1-linux-arm64.tar.gz",
			SHA256: "c6878a324421b69e3e20b00ba17c04bc5c6dab0030cfe55bf8f68fa8d9e9093a",
		},
		"amd64": {
			URL:    "https://github.com/FiloSottile/age/releases/download/v1.3.1/age-v1.3.1-linux-amd64.tar.gz",
			SHA256: "bdc69c09cbdd6cf8b1f333d372a1f58247b3a33146406333e30c0f26e8f51377",
		},
	}

	url := ageAssets[userArch].URL
	checksum := ageAssets[userArch].SHA256

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
