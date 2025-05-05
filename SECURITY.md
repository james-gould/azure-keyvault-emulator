# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 2.2.0   | :white_check_mark: |
| 2.1.0   | :x:                |
| 1.0.6   | :x:                |
| < 1.0.6   | :x:              |

## Reporting a Vulnerability

Please raise an issue if you discover a vulnerability, optionally including the OWASP identifier, and a description of the impact.

If you wish to privately disclose the vulnerability you can also [email it privately here](hello@jamesgould.dev).

## Usage

Please be aware that the emulator is **not** a suitable place to store production secrets, and should ideally be used with `ContainerLifetime.Session` to purge all persisted secrets on destroy.
