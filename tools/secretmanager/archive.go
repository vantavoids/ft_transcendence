package main

import (
	"archive/tar"
	"compress/gzip"
	"io"
	"os"
)

func extractTarGz(tarGzPath string) error {

	tarPath, err := decompressGz(tarGzPath)
	if err != nil {
		return err
	}

	err = extractTar(tarPath)
	if err != nil {
		return err
	}

	return nil
}

func decompressGz(filepath string) (string, error) {

	file, err := os.Open(filepath)
	if err != nil {
		return "", err
	}
	defer file.Close()

	newPath := filepath[:len(filepath)-3]
	tmpPath := newPath + ".tmp"
	out, err := os.Create(tmpPath)
	if err != nil {
		return "", err
	}

	zr, err := gzip.NewReader(file)
	if err != nil {
		return "", err
	}

	_, err = io.Copy(out, zr)
	if err != nil {
		return "", err
	}

	err = os.Rename(tmpPath, newPath)
	if err != nil {
		return "", err
	}

	err = os.Remove(filepath)
	if err != nil {
		return "", err
	}

	return newPath, nil
}

func extractTar(filepath string) error {

	file, err := os.Open(filepath)
	if err != nil {
		return err
	}
	defer file.Close()

	ageDir := "age/"
	ageBin := "age-keygen"
	binPath := toolsDir + ageBin
	out, err := os.Create(binPath + ".tmp")
	if err != nil {
		return err
	}

	// Open and iterate through the files in the archive.
	tr := tar.NewReader(file)
	for {
		hdr, err := tr.Next()
		if err == io.EOF {
			break // End of archive
		}
		if err != nil {
			return err
		}

		if hdr.Name == ageDir+ageBin {

			if _, err := io.Copy(out, tr); err != nil {
				return err
			}

			err = os.Rename(binPath+".tmp", binPath)
			if err != nil {
				return err
			}

			err = os.Remove(filepath)
			if err != nil {
				return err
			}

			break
		}
	}

	return nil
}
