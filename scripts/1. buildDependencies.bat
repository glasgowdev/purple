set scriptDir=%~dp0

set vs15="C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"
set vs17="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"

set msbuild=%vs15%

if exist %vs17% (
    set msbuild=%vs17%
)

%msbuild% "%scriptDir%..\src\Cryptonight\Cryptonight.vcxproj" /t:Clean,Build /p:Platform=Win32 /p:Configuration=Release /v:quiet
%msbuild% "%scriptDir%..\src\Cryptonight\Cryptonight.vcxproj" /t:Clean,Build /p:Platform=x64 /p:Configuration=Release /v:quiet

echo f | xcopy "%scriptDir%..\src\Cryptonight\bin\Debug\Win32\CryptoNight.dll"  "%scriptDir%..\src\Dependencies\CryptoNight\CryptoNight_x86.dll" /Y
echo f | xcopy "%scriptDir%..\src\Cryptonight\bin\Debug\x64\CryptoNight.dll"  "%scriptDir%..\src\Dependencies\CryptoNight\CryptoNight_x64.dll" /Y

pause
