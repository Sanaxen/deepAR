:@echo off

call setup_ini.bat


set BATPATH=%~dp0
set BIN="%BATPATH%\bin"
set WRK="%BATPATH%\Work"

mkdir %WRK%

echo %R_INSTALL_PATH%> backend.txt
echo %PYTHON_VENV%> python_venv.txt
echo %PYTHON_VENV1%>> python_venv.txt
echo %PYTHON_VENV2%>> python_venv.txt
echo %PYTHON_VENV3%>> python_venv.txt
echo %PYTHON_VENV4%>> python_venv.txt
echo %PYTHON_VENV5%>> python_venv.txt

:pause
set LDM="%BIN%\deepARapp.exe"

if not "%2" == "" set WRK=%2

:start "DeepAR" "%LDM%" "%WRK%" "%1"

%LDM% "%WRK%" "%1"