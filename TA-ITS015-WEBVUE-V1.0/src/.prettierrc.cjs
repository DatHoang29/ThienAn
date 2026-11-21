module.exports = {
	// Maximum number of characters per line
	printWidth: 200,
	// Number of spaces per indentation level
	tabWidth: 2,
	// Use tabs instead of spaces for indentation
	useTabs: true,
	// Print semicolons at the end of statements
	semi: true,
	// Use single quotes instead of double quotes
	singleQuote: true,
	// Change when to quote object properties. Possible values: "<as-needed|consistent|preserve>"
	quoteProps: 'as-needed',
	// Use single quotes instead of double quotes in JSX
	jsxSingleQuote: false,
	// Print trailing commas wherever possible in multi-line. (e.g., single-line arrays will never have trailing commas.) Possible values: "<none|es5|all>", default is none
	trailingComma: 'es5',
	// Print spaces between brackets in object literals
	bracketSpacing: true,
	// Put the closing bracket of JSX tags on the same line
	jsxBracketSameLine: false,
	// Include parentheses around a sole arrow function parameter always: (x) => x \ avoid: x => x
	arrowParens: 'always',
	// These two options can be used to format code starting and ending with a given character offset (inclusive and exclusive respectively)
	rangeStart: 0,
	rangeEnd: Infinity,
	// Specify the parser to use, no need to write @prettier at the beginning of the file
	requirePragma: false,
	// No need to automatically insert @prettier at the beginning of the file
	insertPragma: false,
	// Use the default wrap standard always\never\preserve
	proseWrap: 'preserve',
	// Specify the global whitespace sensitivity for HTML files css\strict\ignore
	htmlWhitespaceSensitivity: 'css',
	// Indentation for script and style tags in Vue files
	vueIndentScriptAndStyle: false,
	// Line endings use lf. Possible values: "<auto|lf|crlf|cr>"
	endOfLine: 'lf',
};
