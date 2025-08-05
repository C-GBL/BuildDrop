# BuildDrop – Visual Studio Build Sender & Receiver
[Watch the video](https://raw.githubusercontent.com/C-GBL/BuildDrop/refs/heads/main/showcase.mp4)
Minimal utilities to push freshly built binaries from a development PC to another machine over LAN immediately after a Visual Studio build.
For use with prototyping Windows applications on devices within your local network easily, without the use of File Shares.

## Components

* **BuildReceiver** (target PC): WinForms app that listens on a TCP port and writes incoming files to a chosen folder.
* **BuildSender** (dev PC): WinForms app with a silent command-line mode for post-build automation.

## Requirements

* Windows 10/11
* .NET Framework 4.7.2 on both machines
* Network reachability between PCs and an open inbound TCP port on the receiver (default 9000)

## Quick Start

1. **Receiver**: Run `BuildReceiver.exe`, choose destination folder, set port, click **Start Listening**.
2. **Sender**: Place `BuildSender.exe` somewhere in your solution, e.g. `.\Tools\BuildSender.exe`.
3. **Visual Studio Post-build** (Project → Properties → Build Events):

   ```cmd
   "$(SolutionDir)Tools\BuildSender.exe" "$(TargetPath)" 10.10.1.76 9000
   ```

   Replace IP/port as needed.

## How It Works

The sender opens a TCP connection, sends filename length, filename, and file size, then streams the file. The receiver writes to `<destination>\<filename>`.

## Command-Line Usage

```
BuildSender.exe <filePath> <host> <port>
```

Example:

```
BuildSender.exe "C:\repo\MyApp\bin\Release\MyApp.exe" 10.10.1.76 9000
```

Supplying all three arguments runs in silent mode and exits with code 0 on success.

## MSBuild Alternative

Add to the project file to keep the rule in source control:

```xml
<Target Name="AfterBuild" Condition="'$(Configuration)' == 'Debug'">
  <Exec Command='"$(SolutionDir)Tools\BuildSender.exe" "$(TargetPath)" 10.10.1.76 9000' />
</Target>
```

## Troubleshooting

* **Exit code 9009**: path to `BuildSender.exe` is wrong. Verify the quoted path and that the file exists.
* **Exit code 3**: path or connection issue. Confirm `$(TargetPath)` exists, receiver is listening, and firewall allows the port.
* **Connection refused/timeouts**: receiver not running or firewall blocking. Start the receiver and open the port.

To avoid failing the build on transfer errors:

```cmd
"$(SolutionDir)Tools\BuildSender.exe" "$(TargetPath)" 10.10.1.76 9000 || exit 0
```

## Extending

* **Send a folder**: enumerate `bin\<config>` and send multiple files.
* **Zip then send**: compress build output, send one archive, extract on receiver.
* **Versioned drops**: include subfolders in the transmitted filename (e.g., `MyApp\1.2.3\MyApp.exe`) and create directories before writing.
* **Authentication/TLS**: add a pre-shared token to the handshake; wrap streams in `SslStream`.
* **Service mode**: host the receiver as a Windows Service or scheduled task for always-on listening.

## Project Layout

```
/BuildSender
  SenderForm.cs
  SenderForm.Designer.cs
  Program.cs
/BuildReceiver
  ReceiverForm.cs
  ReceiverForm.Designer.cs
  Program.cs
/Tools
  BuildSender.exe
README.md
```
