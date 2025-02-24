curl http://localhost:8000/
curl -X POST http://localhost:8000/loopback -d "Hello POST loopback endpoint"
curl http://localhost:8000/user
curl -X PUT http://localhost:8000/user/1 -d "{\"Email\":\"foo@bar.com\",\"Password\":\"foobar\"}"
