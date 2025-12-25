# PiCheck

A lightweight Windows system tray application that monitors SSH connectivity to remote hosts (originally designed for Raspberry Pi devices).

## Features

- **System Tray Integration**: Runs quietly in the background with a colored indicator
- **Visual Status**: Green dot for online, red dot for offline, gray dot during startup
- **SSH Connectivity Monitoring**: Uses standard SSH commands to test connectivity
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
2. The tray icon color indicates connectivity status:
   - **Gray**: Starting up or checking
   - **Green**: Target host is online and accessible via SSH
   - **Red**: Target host is offline or unreachable
   - **Orange**: Checking new configuration

3. Right-click the tray icon for options:
   - **Force Check Now**: Immediately test connectivity
   - **Configure**: Change the SSH target host
   - **Exit**: Close the application

4. Double-click the tray icon to open configuration

## Configuration

- Default target: `junior@100.117.1.121`
- Target can be changed via the configuration dialog
- Settings are automatically saved between sessions

## Technical Details

- Uses SSH with batch mode and timeout for reliable connectivity testing
- Checks connectivity every hour (3600000 milliseconds)
- SSH command: `ssh -o ConnectTimeout=10 -o BatchMode=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=QUIET [target] exit`
- Single instance enforcement using named mutex

## Notes

- Requires SSH key-based authentication (no password prompts in batch mode)
- The application runs completely in the background - no main window
- Hover over the tray icon to see detailed status and next check time