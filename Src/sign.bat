"C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe" sign /fd sha256 /a .\build\bin\Release\PicoGAUpdate.exe
"C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe" timestamp -t http://timestamp.verisign.com/scripts/timstamp.dll .\build\bin\Release\PicoGAUpdate.exe
copy $(ProjectDir)\Components\Scripts\* .\build\bin\Release\