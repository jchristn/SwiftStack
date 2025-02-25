curl http://localhost:8000/
curl -X POST http://localhost:8000/loopback -d "Hello POST loopback endpoint"
curl "http://localhost:8000/search?q=hello&page=2"
curl http://localhost:8000/user
curl -X PUT http://localhost:8000/user/50 -d "{\"Email\":\"foo@bar.com\",\"Password\":\"password\"}"
curl http://localhost:8000/types/string
curl http://localhost:8000/types/number
curl http://localhost:8000/types/json
curl http://localhost:8000/types/null
curl http://localhost:8000/types/foo
