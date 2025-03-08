# HVNCSharp

Creating just for fun, experimenting with CreateDesktop. The PoC is unfinished; it currently uses `CreateDesktop` from the Windows API to create a new desktop, switches to it, and starts `Explorer`. Running as Admin from Visual Studio or compiled results in a mostly blank screen.

The repo includes Server and Client projects, which don’t work yet. I started on them but hit issues with a blank screen and process creation. I might fix them later if time and motivation align.

I’m not planning to add features like mouse movement, but getting Explorer running and sending a screenshot would be a neat goal.

## Credits
- [CodeProject: Desktop Switching](https://www.codeproject.com/KB/cs/csdesktopswitching.aspx)  
  Inspired my approach to use `CreateProcess` from kernel32.dll with STARTUPINFO’s `lpDesktop` parameter for desktop-specific process creation, bypassing the .NET Process class for now.

- [GitHub: MalwareTech/HiddenDesktop](https://github.com/MalwareTech/HiddenDesktop)  
  Provided useful reference for hidden desktop concepts.

- [Stack Overflow: Grey screen when using CreateDesktop](https://stackoverflow.com/questions/72457608/grey-screen-when-using-createdesktop-no-taskbar-or-icon-showing)  
  Key insight: the blank screen was due to running as Administrator. Running in user context made it work—crucial for debugging my issue.

- [Microsoft Docs: CreateDesktopA](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createdesktopa)  
  Official documentation for the `CreateDesktop` API.
