
# FileFam TODO2

- collections of associated files.
- files have arbitrary tags, id (optional), info.
- search by tag(s), id, etc.
- dbl click a file to open it (by association or maybe TODO1 user supplied cmd).


// TODO2 Open txt file as ntr? custom aliases and/or pgm associations?
// subl -n -w %1 --command "set_file_type {\"syntax\": \"Packages/JavaScript/JSON.sublime-syntax\"}"
// When I try this, I see that it sets the syntax of the tab that was active before I executed the command.
// This tells us, that the command supplied on the command line is executed before the file is loaded, likely because ST does this asynchronously.
// For me, it's possible to get it working by using a separate invocation:
// subl C:\test\README && subl --command "set_file_type { \"syntax\": \"Packages/JavaScript/JSON.sublime-syntax\" }"
// Note that I'm not using -w, as this would wait until the file is closed before executing the command.
// Also, you can set the syntax of a new file directly using the new_file command:
// subl --command "new_file { \"syntax\": \"Packages/JavaScript/JSON.sublime-syntax\" }"
// Obviously, if you want it in a new window, you can keep the -n argument. And if you want Sublime Text not to return control to the shell until you close the file, then you can keep the -w too, but from what I can see, that only works if you are opening a file, not when creating a new one. And if you use -w, you won't be able to change the syntax from the command line. You may be better off using a plugin like ApplySyntax or writing a small Python script yourself to set the file type when a file is opened with the path C:\test\README etc.

// Tags and Id are case insensitive (not and converted to lower case). Because.

# UI

## Mouse

## Context Menu

## Tools

