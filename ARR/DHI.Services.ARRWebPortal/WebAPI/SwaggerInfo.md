## Security
To ensure that only authenticated users can access the Web API, some requests are protected using [Basic Authentication](https://en.wikipedia.org/wiki/Basic_access_authentication). Basic Authentication is using a Base64 encoding of username and password. As the Base64 encoding is reversible, clients should preferably use HTTPS (instead of HTTP) when connecting to Web API. 

For all protected actions, the client must compute a Base64 encoding of __username:password__ and include the value in an `Authorization` header in the request.

Below, an example of a request for adding a new account (user) is shown:

```http
POST http://localhost:17510/api/account http/1.1
Content-Type: application/json
Authorization: Basic YWRtaW46VGhpc0lzTm90VGhlUmVhbFBhc3N3b3JkIQ==

{
  "Password": "password",
  "Id": "john.doe",
  "Name": "John Doe",
  "Email": "john.doe@acme.com",
  "Roles": "User"
}
```
If the requesting user is not properly authenticated, the server will return a response with HTTP status code `401 (Unauthorized)`.
Access to the web API is guarded by role-based security. Some services (e.g. adding or deleting accounts or connections) require administrator privileges.

## Connections
A connection holds information about specific provider types to be used for a specific type of service – e.g. a MIKE res1d repository to be used in a time series service. Furthermore, the connection defines connection information such as a file path or a connection string to a particular database and possible other necessary information for establishing the connection.