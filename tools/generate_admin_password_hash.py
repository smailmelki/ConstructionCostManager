#!/usr/bin/env python3
"""Generate the PBKDF2 format accepted by AdminAuthenticationService."""

import base64
import getpass
import hashlib
import secrets

ITERATIONS = 210_000
password = getpass.getpass("Admin password: ")
confirmation = getpass.getpass("Repeat password: ")
if not password or password != confirmation:
    raise SystemExit("Passwords are empty or do not match.")

salt = secrets.token_bytes(16)
digest = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, ITERATIONS, 32)
encoded = "$".join(
    [
        "v1",
        str(ITERATIONS),
        base64.b64encode(salt).decode("ascii"),
        base64.b64encode(digest).decode("ascii"),
    ]
)
print("Admin:PasswordHash=" + encoded)
