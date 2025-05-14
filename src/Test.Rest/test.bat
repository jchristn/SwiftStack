@echo off
echo.
echo Unauthenticated routes
echo ----------------------
echo.
echo Calling GET / (should succeed)
curl -v http://localhost:8000/
echo.
echo Calling POST /loopback (should succeed)
curl -v -X POST http://localhost:8000/loopback -d "Hello POST loopback endpoint"
echo.
echo Calling POST /search (should succeed)
curl -v "http://localhost:8000/search?q=hello&page=2"
echo.
echo Calling GET /user (should succeed)
curl -v http://localhost:8000/user
echo.
echo Calling PUT /user/50 (should succeed)
curl -v -X PUT http://localhost:8000/user/50 -d "{\"Email\":\"foo@bar.com\",\"Password\":\"password\"}"
echo.
echo Calling GET /types/string
curl -v http://localhost:8000/types/string
echo.
echo Calling GET /types/number
curl -v http://localhost:8000/types/number
echo.
echo Calling GET /types/json
curl -v http://localhost:8000/types/json
echo.
echo Calling GET /types/null
curl -v http://localhost:8000/types/null
echo.
echo Calling GET /types/foo
curl -v http://localhost:8000/types/foo
echo.
echo Calling GET /types/events/5
curl -v http://localhost:8000/events/5
echo.
echo Calling GET /exception/400
curl -v -i http://localhost:8000/exception/400
echo.
echo Calling GET /exception/401
curl -v -i http://localhost:8000/exception/401
echo.
echo Calling GET /exception/404
curl -v -i http://localhost:8000/exception/404
echo.
echo Calling GET /exception/409
curl -v -i http://localhost:8000/exception/409
echo.
echo Calling GET /exception/500
curl -v -i http://localhost:8000/exception/500
echo.

echo.
echo Authenticated routes
echo --------------------
echo.
echo Calling GET /authenticated (valid password)
curl -v -H "Authorization: Bearer password" http://localhost:8000/authenticated
echo.
echo Calling GET /authenticated (invalid password)
curl -v -H "Authorization: Bearer badpassword" http://localhost:8000/authenticated
echo.

echo on
