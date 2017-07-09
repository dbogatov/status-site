package main

import (
	"encoding/json"
	"fmt"
	"io"
	"io/ioutil"
	"log"
	"net"
	"net/http"
	"strconv"
	"strings"
	"time"
)

type response struct {
	URL     string
	Method  string
	Timeout int // milliseconds

	Latency       int               `json:",omitempty"` // milliseconds
	Headers       map[string]string `json:",omitempty"`
	ContentLength int               `json:",omitempty"`
	Status        string            `json:",omitempty"`

	Error string `json:",omitempty"`
}

func main() {

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {

		w.Header().Set("Content-Type", "application/json")

		// url := r.URL.Query().Get("url");
		url := "http://localhost:5555"
		method := r.URL.Query().Get("method")
		timeout, timeoutConversionErr := strconv.Atoi(r.URL.Query().Get("timeout"))

		response := response{
			URL:     url,
			Method:  method,
			Timeout: timeout,
		}

		if timeoutConversionErr != nil {
			sendError(
				&w,
				fmt.Errorf("%q is not a valid timeout value", r.URL.Query().Get("timeout")),
				response,
			)
			return
		}

		start := time.Now()

		var resp *http.Response
		var httpErr error
		client := http.Client{
			Timeout: time.Duration(timeout * int(time.Millisecond)),
		}

		switch strings.ToUpper(method) {
		case "HEAD":
			resp, httpErr = client.Head(url)
		case "GET":
			resp, httpErr = client.Get(url)
		default:
			sendError(&w, fmt.Errorf("Method %q is not supported", method), response)
			return
		}

		if httpErr != nil {
			errorMessage := httpErr.Error()

			if _, ok := httpErr.(*net.OpError); ok {
				errorMessage = fmt.Sprintf("Can't access %s", url)
			}

			if _, ok := httpErr.(net.Error); ok && httpErr.(net.Error).Timeout() {
				errorMessage = "Timeout"
			}

			sendError(
				&w,
				fmt.Errorf(errorMessage),
				response,
			)
			return
		}

		end := time.Since(start)

		defer resp.Body.Close()

		response.Latency = int(end.Nanoseconds() / 1000000)

		copied, _ := io.Copy(ioutil.Discard, resp.Body)
		response.ContentLength = int(copied)

		response.Status = resp.Status

		response.Headers = make(map[string]string)

		for k, v := range resp.Header {
			response.Headers[k] = fmt.Sprintf("%s", v)
		}

		data, _ := json.Marshal(response)

		w.Write(data)
	})

	log.Fatal(http.ListenAndServe(":8000", nil))
}

func sendError(w *http.ResponseWriter, error error, response response) {
	response.Error = error.Error()

	data, _ := json.Marshal(response)

	(*w).Write(data)
}
