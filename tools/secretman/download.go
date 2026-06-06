package main

import (
	"fmt"
	"io"
	"net/http"
	"os"
	"strings"

	"github.com/dustin/go-humanize"
)

type WriteCounter struct {
	Total    uint64
	FileSize uint64
}

func (wc *WriteCounter) Write(chunk []byte) (int, error) {

	n := len(chunk)
	wc.Total += uint64(n)
	wc.PrintProgress()
	return n, nil
}

func (wc WriteCounter) PrintProgress() {

	fmt.Printf("\r%s", strings.Repeat(" ", 50)) // clear line
	fmt.Printf("\r   Downloading... %s / %s", humanize.Bytes(wc.Total), humanize.Bytes(wc.FileSize))
}

func downloadFile(url string, filepath string, checksum string) error {

	tmpPath := filepath + ".tmp"

	fileSize, err := getRemoteFileSize(url)
	if err != nil {
		return err
	}

	// create a tmp file
	out, err := os.Create(tmpPath)
	if err != nil {
		return err
	}

	// get the data
	resp, err := http.Get(url)
	if err != nil {
		out.Close()
		os.Remove(tmpPath)
		return err
	}
	defer resp.Body.Close()

	// create our bytes counter and pass it to be used alongside our writer
	counter := &WriteCounter{0, fileSize}
	reader := io.TeeReader(resp.Body, counter)

	_, copyErr := io.Copy(out, reader)
	closeErr := out.Close()

	if copyErr != nil {
		os.Remove(tmpPath)
		return copyErr
	}
	if closeErr != nil {
		os.Remove(tmpPath)
		return closeErr
	}

	fmt.Printf("\r%s", strings.Repeat(" ", 50)) // clear line
	fmt.Printf("\r✅ Downloaded %s, all done.\n", humanize.Bytes(counter.Total))

	err = checkIntegrity(tmpPath, checksum)
	if err != nil {
		os.Remove(tmpPath)
		return err
	}

	// rename the tmp file back to the original file
	// after checking integrity
	err = os.Rename(filepath+".tmp", filepath)
	if err != nil {
		os.Remove(tmpPath)
		return err
	}

	return nil
}

func getRemoteFileSize(url string) (uint64, error) {

	resp, err := http.Head(url)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return 0, fmt.Errorf("HEAD request failed: %s", resp.Status)
	}

	size := resp.ContentLength
	if size < 0 {
		return 0, fmt.Errorf("server did not provide Content-Length")
	}

	return uint64(size), nil
}
