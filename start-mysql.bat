@echo off
echo Starting MySQL Server on port 3306 using datadir d:\mysql-data...
"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqld.exe" --datadir="d:\mysql-data" --port=3306 --console
pause
