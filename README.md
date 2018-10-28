# Ebnf.Compiler
Converts Extended Backus-Naur Form statements to C#, and then compiles them into a DLL which can be referenced in applications.

*Note: This project was created for educational purposes only. For a much more flexible and user friendly solution, I recommend looking at the [Antlr](http://www.antlr.org/) project.* 

## Running the Compiler

This is a command line application. Whilst the recommended usage is by being ran by an automated tool, with arguments supplied when the process is created, it is possible to run the program within a standard console window and enter the inputs manually.

If you choose to supply the arguments when launching the program, the order of arguments is as follows:
- **Input file path:** The path of the file containing the EBNF statements
- **Output file path:** The path of the output DLL file
- **Default namespace:** The namespace that generated classes should be placed within

If you do not supply any arguments, you will be required to manually enter these inputs via the standard input.

## Library usage

Within the generated library, you will find a class called `Validation`. This will contain varous functions for detecting matches to the given rules, for example, you could see a method defintion like this:
```C#
/// <summary>
/// WholeNumber=Digit,{Digit}
/// </summary>
/// <param name="input">The input string to validate.</param>
/// <param name="remainder">The part of the string that did not match the given rule set; empty if full match.</param>
/// <param name="parseTree">The parse tree produced from the input.</param>
/// <param name="level">The internal level of recursion; starts at 0.</param>
public static bool IsWholeNumber(string input, out string remainder, out Node parseTree, int level = 0)
{
    // ...
}
```
The summary will contain the EBNF rule corresponding to the method definition.

The parameters are used as follows:
- **input:** The string to search for a match in. Note that it will search from the start of the string, so the use of substring will be required if you wish to search from a specified character within the string.
- **remainder:** The returned value contains the part of the string that could not be matched to anything. If a match is found to the token and the function returns true, you will also need to check that this value has a length of 0 to verify that it the string was a complete match.
- **parseTree:** This will be a detailed tree of `Node` objects, indicating all the token matches that were found.
- **level:** This parameter is for internal use only and you do not need to worry about it.

In addition, a class called `Node` and an enum called `NodeTypes` will be generated. Both of these have automatically generated XML documentation which I feel amply describes them.

## EBNF Syntax

My implementation of EBNF is not necessarily standard, so please read this section in order to avoid any compilation errors!

### Comments
All comments must start with `(*` and end with `*)`:
```
(* My comment *)
```
Comments can be placed anywhere within the document and are stripped during compilation.

### Literals
This implementation of EBNF does not support Regex. Therefore, all literals must be string values, surrounded by quotation marks:
```
"String"
```

### Equality
EBNF statements must have the form:
```
TokenType = Conditions;
```

The simplest case, comparing against a string literal, is shown below:
```
Number 0 = "0";
```

Note that token names may contain spaces, although these spaces will be removed during compiling.
Please be aware also that matches to literals are case sensitive.

### Or
The pipe symbol (`|`) can be used for allowing a match to any one of several options:
```
Variable = "x" | "y"; (* Matches "x" or "y" *)
```

### Calling rules
You can call other rules by referring to them by name:
```
Operator = Plus | Minus; (* Matches "+" or "-" *)
Plus = "+";
Minus = "-";
```

Note that this can be used for recursion, but make sure to include a base case so that the recursion is not infinite.

### Concatenation
The `,` symbol is used for concatenating multiple rules:
```
Greeting = Hello, ", World!"; (* Matches "Hello, World!" *)
Hello = "Hello";
```

### Optional rules
For rules that may be ommitted, square brackets (`[` and `]`) may be used:
```
Decimal = Whole Number, [".", Whole Number];
```

### Repetition
If a token may appear 0 or more times, curly braces (`{` and `}`) may be used:
```
Whole Number = Digit, {Digit}; (* Matches 1 or more digits *)
Digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
```

## Sample file for testing
This is a sample EBNF file I have produced for testing the capability of the program:

```
(* Sample EBNF statements for testing *)

Signed Number = [Sign], Positive Number; 
Positive Number = Whole Number, [".", Whole Number];
Whole Number = Digit, {Digit};
Digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
Sign = "+" | "-";
```
