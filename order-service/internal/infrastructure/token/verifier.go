package token

import (
	"crypto/rsa"
	"crypto/x509"
	"encoding/pem"
	"errors"
	"fmt"
	"os"

	golangjwt "github.com/golang-jwt/jwt/v5"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

func LoadPublicKey(path string) (*rsa.PublicKey, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read JWT public key %s: %w", path, err)
	}
	block, _ := pem.Decode(data)
	if block == nil {
		return nil, fmt.Errorf("JWT public key %s is not PEM", path)
	}

	if block.Type == "RSA PUBLIC KEY" {
		return x509.ParsePKCS1PublicKey(block.Bytes)
	}
	parsed, err := x509.ParsePKIXPublicKey(block.Bytes)
	if err != nil {
		return nil, fmt.Errorf("parse JWT public key %s: %w", path, err)
	}
	key, ok := parsed.(*rsa.PublicKey)
	if !ok {
		return nil, fmt.Errorf("JWT public key %s is not an RSA key", path)
	}
	return key, nil
}

type Verifier struct {
	key    *rsa.PublicKey
	issuer string
}

func NewVerifier(key *rsa.PublicKey, issuer string) *Verifier {
	return &Verifier{key: key, issuer: issuer}
}

type claims struct {
	Role  string `json:"role"`
	Email string `json:"email"`
	golangjwt.RegisteredClaims
}

func (v *Verifier) Verify(token string) (domain.Caller, error) {
	parsed := &claims{}
	_, err := golangjwt.ParseWithClaims(token, parsed,
		func(*golangjwt.Token) (any, error) { return v.key, nil },
		golangjwt.WithValidMethods([]string{"RS256"}),
		golangjwt.WithIssuer(v.issuer),
		golangjwt.WithExpirationRequired(),
	)
	if err != nil {
		return domain.Caller{}, err
	}

	if parsed.Subject == "" {
		return domain.Caller{}, errors.New("token has no sub")
	}
	role := domain.Role(parsed.Role)
	if role != domain.RoleUser && role != domain.RoleAdmin {
		return domain.Caller{}, fmt.Errorf("unknown role %q", parsed.Role)
	}
	return domain.Caller{UserID: parsed.Subject, Email: parsed.Email, Role: role}, nil
}
