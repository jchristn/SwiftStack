@echo off
echo.
echo Unauthenticated routes
echo ----------------------
curl http://localhost:8000/
echo.
curl -X POST http://localhost:8000/loopback -d "Hello POST loopback endpoint"
echo.
curl "http://localhost:8000/search?q=hello&page=2"
echo.
curl http://localhost:8000/user
echo.
curl -X PUT http://localhost:8000/user/50 -d "{\"Email\":\"foo@bar.com\",\"Password\":\"password\"}"
echo.
curl http://localhost:8000/types/string
echo.
curl http://localhost:8000/types/number
echo.
curl http://localhost:8000/types/json
echo.
curl http://localhost:8000/types/null
echo.
curl http://localhost:8000/types/foo
echo.
curl http://localhost:8000/events/5
echo.
curl -i http://localhost:8000/exception/400
echo.
curl -i http://localhost:8000/exception/401
echo.
curl -i http://localhost:8000/exception/404
echo.
curl -i http://localhost:8000/exception/409
echo.
curl -i http://localhost:8000/exception/500
echo.

echo.
echo Authenticated routes
echo --------------------
curl -H "Authorization: Bearer password" http://localhost:8000/authenticated
echo.
curl -H "Authorization: Bearer badpassword" http://localhost:8000/authenticated
echo.

echo on
