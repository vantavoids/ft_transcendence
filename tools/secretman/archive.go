package main

import (
	"archive/tar"
	"compress/gzip"
	"fmt"
	"io"
	"os"
	"path/filepath"
)

func extractTarGz(tarGzPath string, target string) error {

	tarPath, err := decompressGz(tarGzPath)
	if err != nil {
		return err
	}

	err = extractTar(tarPath, target)
	if err != nil {
		return err
	}

	return nil
}

func decompressGz(filePath string) (string, error) {

	file, err := os.Open(filePath)
	if err != nil {
		return "", err
	}
	defer file.Close()

	newPath := filePath[:len(filePath)-3]
	tmpPath := newPath + ".tmp"
	out, err := os.Create(tmpPath)
	if err != nil {
		return "", err
	}

	zr, err := gzip.NewReader(file)
	if err != nil {
		out.Close()
		return "", err
	}

	_, copyErr := io.Copy(out, zr)
	outCloseErr := out.Close()
	zrCloseErr := zr.Close()

	if copyErr != nil {
		return "", copyErr
	}
	if outCloseErr != nil {
		return "", outCloseErr
	}
	if zrCloseErr != nil {
		return "", zrCloseErr
	}

	err = os.Rename(tmpPath, newPath)
	if err != nil {
		return "", err
	}

	err = os.Remove(filePath)
	if err != nil {
		return "", err
	}

	return newPath, nil
}

func extractTar(filePath string, target string) error {

	file, err := os.Open(filePath)
	if err != nil {
		return err
	}
	defer file.Close()

	binPath := toolsDir + filepath.Base(target)
	tmpPath := binPath + ".tmp"

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

		if hdr.Name == target {
			out, err := os.Create(tmpPath)
			if err != nil {
				return err
			}

			_, copyErr := io.Copy(out, tr)
			closeErr := out.Close()

			if copyErr != nil {
				os.Remove(tmpPath)
				return copyErr
			}
			if closeErr != nil {
				os.Remove(tmpPath)
				return closeErr
			}

			err = os.Rename(tmpPath, binPath)
			if err != nil {
				return err
			}

			err = os.Remove(filePath)
			if err != nil {
				return err
			}

			return nil
		}
	}

	return fmt.Errorf("❌ %s binary not found in archive", target)
}
