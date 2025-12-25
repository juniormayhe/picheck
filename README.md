# PiCheck

A lightweight Windows system tray application that monitors SSH connectivity to remote hosts (originally designed for Raspberry Pi devices).

## Features

- **System Tray Integration**: Runs quietly in the background with custom icon indicators
- **Professional Icons**: Custom .ico files for clear visual status communication
- **SSH Connectivity Monitoring**: Uses standard SSH commands to test connectivity
- **Windows Startup Support**: Option to start automatically with Windows
- **Automatic Checks**: Checks connectivity every hour automatically
- **Manual Testing**: Force immediate connectivity check via right-click menu
- **Configurable Target**: Easy configuration of SSH target host
- **Status Notifications**: Balloon notifications when connectivity status changes
- **Single Instance**: Prevents multiple instances from running simultaneously

## System Requirements

- Windows with .NET support
- SSH client (OpenSSH) available in system PATH
- SSH key-based authentication configured for the target host

## Installation

1. Clone this repository
2. Build the solution using Visual Studio or `dotnet build`
3. Run the executable from the output directory

## Usage

1. Launch PiCheck - it will minimize to the system tray
2. The tray icon indicates connectivity status:
   - **Connecting Icon** (`picheck-connecting.ico`): Starting up, checking, or reconfiguring
   - **Online Icon** (`picheck.ico`): Target host is online and accessible via SSH
   - **Offline Icon** (`picheck-offline.ico`): Target host is offline or unreachable

3. Right-click the tray icon for options:
   - **Force Check Now**: Immediately test connectivity
   - **Configure**: Change SSH target and startup settings
   - **Start with Windows**: Toggle automatic startup (checkmark when enabled)
   - **Exit**: Close the application

4. Double-click the tray icon to open configuration

## Configuration

- **SSH Target**: Default target is `junior@100.117.1.121`
- **Windows Startup**: Option to start PiCheck automatically with Windows
- **Settings Access**: Configuration dialog available via right-click menu or double-click tray icon
- **Persistent Settings**: All settings are automatically saved between sessions

## Technical Details

- **SSH Testing**: Uses SSH with batch mode and timeout for reliable connectivity testing
- **Check Interval**: Automatically checks connectivity every hour (3600000 milliseconds)
- **SSH Command**: `ssh -o ConnectTimeout=10 -o BatchMode=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=QUIET [target] exit`
- **Startup Management**: Uses Windows Registry to manage startup settings
- **Icon System**: Embedded .ico resources with filesystem fallback for reliability
- **Single Instance**: Named mutex prevents multiple instances from running

## Notes

- Requires SSH key-based authentication (no password prompts in batch mode)
- The application runs completely in the background - no main window
- Hover over the tray icon to see detailed status and next check time