[Setup]
;-- Main Setup Information
 AppName                          = Katzebase Server
 AppVerName                       = Katzebase Server 0.15.0
 AppCopyright                     = Copyright © 2024 NetworkDLS.
 DefaultDirName                   = {commonpf}\NetworkDLS\Katzebase\Server
 DefaultGroupName                 = NetworkDLS\Katzebase
 UninstallDisplayIcon             = {app}\NTDLS.Katzebase.Server.exe
 SetupIconFile                    = "..\@Art\Icon.ico"
 PrivilegesRequired               = admin
 Uninstallable                    = Yes
 MinVersion                       = 0.0,7.0
 Compression                      = bZIP/9
 ChangesAssociations              = Yes
 OutputBaseFilename               = Katzebase.Server
 ArchitecturesInstallIn64BitMode  = x64compatible
 AppPublisher                     = NetworkDLS
 AppPublisherURL                  = http://www.NetworkDLS.com/
 AppUpdatesURL                    = http://www.NetworkDLS.com/
 AppVersion                       = 0.15.0

[Files]
 Source: "..\NTDLS.Katzebase.Server\bin\Release\net8.0\runtimes\*.*";  DestDir: "{app}\runtimes";              Flags: IgnoreVersion recursesubdirs;
 Source: "..\NTDLS.Katzebase.Server\bin\Release\net8.0\*.exe";         DestDir: "{app}";                       Flags: IgnoreVersion;
 Source: "..\NTDLS.Katzebase.Server\bin\Release\net8.0\*.dll";         DestDir: "{app}";                       Flags: IgnoreVersion;
 Source: "..\NTDLS.Katzebase.Server\bin\Release\net8.0\*.json";        DestDir: "{app}";                       Flags: IgnoreVersion;
 Source: "Settings\*.*";                                                DestDir: "{app}";                       Flags: IgnoreVersion;

[Registry]
 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\Server";                                                             Flags: uninsdeletekey noerror;
 Root: HKLM; Subkey: "Software\NetworkDLS\Katzebase\Server\"; ValueType: string; ValueName: "Path";   ValueData:"{app}"; Flags: CreateValueIfDoesntExist;

[Run]
 Filename: "{app}\NTDLS.Katzebase.Server.exe"; Parameters: "install"; Flags: runhidden; StatusMsg: "Installing services..."
 Filename: "{app}\NTDLS.Katzebase.Server.exe"; Parameters: "start"; Flags: runhidden; StatusMsg: "Starting services..."

[UninstallRun]
 Filename: "{app}\NTDLS.Katzebase.Server.exe"; Parameters: "uninstall"; Flags: runhidden; StatusMsg: "Removing services..."; RunOnceId: "ServiceRemoval"
 
