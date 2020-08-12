set dir=%~dp0
call setup_ini.bat

set python_venv=%PYTHON_VENV%
:pause


set CUDA_VISIBLE_DEVICES=0
PATH=%python_venv%;%python_venv%\%PYTHON_VENV1%;%python_venv%\%PYTHON_VENV2%;%python_venv%\%PYTHON_VENV3%;%python_venv%\%PYTHON_VENV4%;%python_venv%\%PYTHON_VENV5%;%PATH%

copy setup_ini.bat deepAR_application /v /y

mkdir deepAR_application\bin
mkdir deepAR_application\wrk
mkdir deepAR_application\script

copy WindowsFormsApp1\WindowsFormsApp1\bin\Debug\*.exe deepAR_application\bin /v /y
copy WindowsFormsApp1\WindowsFormsApp1\bin\script\*.* deepAR_application\script\*.* /v /y
mkdir WindowsFormsApp1\WindowsFormsApp1\bin\wrk


call setup_ini.bat


echo %R_INSTALL_PATH%> WindowsFormsApp1\WindowsFormsApp1\bin\backend.txt
echo %PYTHON_VENV%> WindowsFormsApp1\WindowsFormsApp1\bin\python_venv.txt
echo %PYTHON_VENV1%>> WindowsFormsApp1\WindowsFormsApp1\bin\python_venv.txt
echo %PYTHON_VENV2%>> WindowsFormsApp1\WindowsFormsApp1\bin\python_venv.txt
echo %PYTHON_VENV3%>> WindowsFormsApp1\WindowsFormsApp1\bin\python_venv.txt
echo %PYTHON_VENV4%>> WindowsFormsApp1\WindowsFormsApp1\bin\python_venv.txt
echo %PYTHON_VENV5%>> WindowsFormsApp1\WindowsFormsApp1\bin\python_venv.txt

echo %R_INSTALL_PATH%> deepAR_application\backend.txt
echo %PYTHON_VENV%> deepAR_application\python_venv.txt
echo %PYTHON_VENV1%>> deepAR_application\python_venv.txt
echo %PYTHON_VENV2%>> deepAR_application\python_venv.txt
echo %PYTHON_VENV3%>> deepAR_application\python_venv.txt
echo %PYTHON_VENV4%>> deepAR_application\python_venv.txt
echo %PYTHON_VENV5%>> deepAR_application\python_venv.txt
