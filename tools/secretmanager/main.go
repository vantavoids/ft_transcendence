package main

import (
	"log"
)

func main() {

	err := bootstrap()
	if err != nil {
		log.Fatal(err)
	}

	// err = run()
	// if err != nil {
	// 	log.Fatal(err)
	// }
}
