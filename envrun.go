package main

import (
	"bufio"
	"fmt"
	"io"
	"log"
	"os"
	"os/exec"
	"path"
	"regexp"
	"sort"
	"syscall"
)

var databaseLineRegex = regexp.MustCompile(`^\s*(.+?)\s*=\s*'(.*?)'\s*$`)
var expandedVariableRegex = regexp.MustCompile(`{{\s*(.+?)\s*}}`)
var envRunCommandRegex = regexp.MustCompile(`@@envrun\[\s*(.+?)\s*]`)
var setVariableCommandRegex = regexp.MustCompile(`^set\s*name\s*=\s*'(.+?)'\s*value\s*=\s*'(.*?)'$`)
var resetVariableCommandRegex = regexp.MustCompile(`^reset\s*name\s*=\s*'(.*?)'$`)

func main() {

	// print usage information, if no parameters are specified
	args := os.Args[1:]
	if len(args) == 0 {
		printUsage()
		os.Exit(1)
	}

	// replace environment variables wrapped in double curly braces, e.g. {{name}}, in arguments
	for i, arg := range args {
		matches := expandedVariableRegex.FindAllStringSubmatch(arg, -1)
		for _, match := range matches {
			name := match[1]
			value := os.Getenv(name)
			args[i] = expandedVariableRegex.ReplaceAllString(arg, value)
		}
	}

	// determine the path of the envrun database file
	dbPath := os.Getenv("ENVRUN_DATABASE")
	if len(dbPath) == 0 {
		dir, err := os.Getwd()
		if err != nil {
			log.Fatalf("ERROR: %v\n", err)
		}
		dbPath = path.Join(dir, "envrun.db")
		dbPath = path.Clean(dbPath)
		os.Setenv("ENVRUN_DATABASE", dbPath)
		fmt.Printf("The ENVRUN_DATABASE environment variable is not set, using %s instead.\n", dbPath)
	}

	// read envrun database
	variables := readEnvrunDatabaseFile(dbPath)

	// run specified application and process stdout and stderr to
	// detect envrun variable setter patterns
	cmd := exec.Command(args[0], args[1:]...)
	stdout, _ := cmd.StdoutPipe()
	stderr, _ := cmd.StderrPipe()
	go processOutputStream(stdout, os.Stdout, variables)
	go processOutputStream(stderr, os.Stderr, variables)
	if err := cmd.Start(); err != nil {
		log.Fatalf("ERROR: %v\n", err)
	} else if err := cmd.Wait(); err != nil {
		if exiterr, ok := err.(*exec.ExitError); ok {
			if status, ok := exiterr.Sys().(syscall.WaitStatus); ok {
				exitcode := status.ExitStatus()
				os.Exit(exitcode)
			}
		} else {
			log.Fatalf("ERROR: %v\n", err)
		}
	}

	// write envrun database
	writeEnvrunDatabaseFile(dbPath, variables)
}

func readEnvrunDatabaseFile(path string) map[string]string {

	variables := make(map[string]string)

	// open file for reading
	file, err := os.Open(path)
	if os.IsNotExist(err) {
		return variables
	} else if err != nil {
		log.Fatalf("ERROR: %v\n", err)
	}
	defer file.Close()

	// read file
	scanner := bufio.NewScanner(file)
	for lineNo := 1; scanner.Scan(); lineNo++ {
		line := scanner.Text()
		match := databaseLineRegex.FindStringSubmatch(line)
		if len(match) == 0 {
			log.Fatalf("ERROR: Reading envrun database file failed (line: %d).", lineNo)
		}
		name := match[1]
		value := match[2]
		variables[name] = value
	}

	if err := scanner.Err(); err != nil {
		log.Fatalf("ERROR: %v\n", err)
	}

	return variables
}

func writeEnvrunDatabaseFile(path string, variables map[string]string) {

	// open file for writing
	file, err := os.OpenFile(path, os.O_RDWR|os.O_CREATE, 0644)
	if err != nil {
		log.Fatalf("ERROR: %v\n", err)
	}
	defer file.Close()

	// sort variable names in ascending order
	var names []string
	for name := range variables {
		names = append(names, name)
	}
	sort.Strings(names)

	// write file
	writer := bufio.NewWriter(file)
	for _, name := range names {
		line := fmt.Sprintf("%s = '%s'\n", name, variables[name])
		writer.WriteString(line)
	}
	writer.Flush()
}

func processOutputStream(
	input io.ReadCloser,
	output *os.File,
	variables map[string]string) {

	reader := io.TeeReader(input, output)
	scanner := bufio.NewScanner(reader)

	for scanner.Scan() {

		line := scanner.Text()

		// @@envrun[...]
		envrunTagMatches := envRunCommandRegex.FindAllStringSubmatch(line, -1)
	outer:
		for _, envrunTagMatch := range envrunTagMatches {

			// @@envrun[set name='...' value='...']
			innerEnvrunTagMatch := setVariableCommandRegex.FindStringSubmatch(envrunTagMatch[1])
			if innerEnvrunTagMatch != nil {
				name := innerEnvrunTagMatch[1]
				value := innerEnvrunTagMatch[2]
				variables[name] = value
				continue outer
			}

			// @@envrun[reset name='...']
			innerEnvrunTagMatch = resetVariableCommandRegex.FindStringSubmatch(envrunTagMatch[1])
			if innerEnvrunTagMatch != nil {
				name := innerEnvrunTagMatch[1]
				delete(variables, name)
				continue outer
			}
		}
	}
}

func printUsage() {
	fmt.Printf("  Griffin+ EnvRun v%s\n", version)
	fmt.Println("------------------------------------------------------------------------------------------------------------------------")
	fmt.Println("  Wraps the execution of other processes and scans their output (stdout/stderr)")
	fmt.Println("  for certain key expressions instructing EnvRun to maintain a set of")
	fmt.Println("  environment variables for following runs.")
	fmt.Println("------------------------------------------------------------------------------------------------------------------------")
	fmt.Println()
	fmt.Println("  USAGE:")
	fmt.Println()
	fmt.Println("  Step 1, optional)")
	fmt.Println("    Set ENVRUN_DATABASE environment variable to the path of the database file.")
	fmt.Println("    If not set, the database (envrun.db) is placed into the working directory.")
	fmt.Println()
	fmt.Println("  Step 2)")
	fmt.Println("    Start applications: EnvRun.exe <path> <arguments>")
	fmt.Println()
	fmt.Println("  The following expressions are recognized in the output streams:")
	fmt.Println("  - @@envrun[set name='<name>' value='<value>']")
	fmt.Println("  - @@envrun[reset name='<name>']")
	fmt.Println()
	fmt.Println("------------------------------------------------------------------------------------------------------------------------")
	fmt.Println("  Full Version:", fullVersion)
	fmt.Println("  Project: https://github.com/griffinplus/envrun")
	fmt.Println("  License: MIT License")
	fmt.Println("------------------------------------------------------------------------------------------------------------------------")
}
