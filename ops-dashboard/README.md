# SecurePay // Ops Control

Standalone, read-only demo. From the repository root, run
`py -m http.server 8080 --bind 127.0.0.1 --directory ops-dashboard`
and open `http://127.0.0.1:8080/`. No npm install or AWS credentials needed.

All telemetry and event data is simulated. The demo refreshes every five seconds
and the stream can be paused independently. Use **Simulate disconnect** and **Retry**
to exercise the degraded state. Charts also support keyboard inspection.

`src/services/opsTelemetryService.js` defines the adapter contract using JSDoc.
Before deployment, replace the simulator with an authenticated server-side
adapter, use allowlisted and redacted event fields, and enforce operator
authorization on the server. Incident dispatch and CloudWatch deep links
are intentionally disabled in the demo. The event stream is not the stored
transaction lifecycle audit trail.

## Hardened local preview

From the repository root:

```powershell
docker build -t securepay-ops:hardened ./ops-dashboard
docker run --rm -p 127.0.0.1:8080:8080 securepay-ops:hardened
```

The image runs unprivileged and sends CSP, frame denial, nosniff, and referrer
headers. Set HSTS at the production HTTPS termination point after HTTPS is
verified. The local preview is HTTP only.

The inspector recursively masks known sensitive field names; credentials
are fully masked and well-formed card numbers show at most four digits.
This does not detect free-text secrets or protect the network payload.
Production must send only allowlisted, server-redacted event fields.
The viewer badge is simulated, not authorization. Incident dispatch stays
disabled until the server authenticates and authorizes the operation and
records an audit event. Do not promise CloudTrail coverage without testing it.

## Deployment security checklist (not provisioned by this demo)

- Use GitHub OIDC with short-lived AWS credentials. Restrict role trust to an
  approved repository and deployment environment; scope deployment IAM
  permissions and grant `id-token: write` only to the deployment job.
- Bootstrap private, versioned S3 state storage with SSE-KMS and locking
  compatible with the pinned Terraform version. Keep state out of Git.
- Pin and scan the container image, then sign its digest and enforce signature
  verification before deployment.
- This frontend has no npm dependencies or lockfile to audit. Add a
  lockfile-based dependency audit when introducing a package manager.
