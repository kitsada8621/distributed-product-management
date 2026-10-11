# JWT signing keys (development)

Auth Service signs access tokens with RS256. The private key PEM is read from the path in
`Jwt:PrivateKeyPath` (environment variable `Jwt__PrivateKeyPath`). The `.pem` files are git-ignored.

Generate a dev key pair from the `auth-service` folder:

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out keys/jwt-private.pem
openssl rsa -in keys/jwt-private.pem -pubout -out keys/jwt-public.pem
```

- `jwt-private.pem`: only Auth Service reads it.
- `jwt-public.pem`: mounted into Catalog and Order later so they can verify tokens.

With `docker run` mount the folder read-only at `/keys` and set `Jwt__PrivateKeyPath=/keys/jwt-private.pem`. `docker-compose.yml` instead passes `jwt-private.pem` as a Compose secret at `/run/secrets/jwt_private_key`.
