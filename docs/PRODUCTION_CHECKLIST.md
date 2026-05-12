# Production Checklist

This project now has stronger paging, safer role-based redirects, authorization policies, health checks, antiforgery validation, and auth rate limiting. Before a real production release, finish the items below.

## Secrets and configuration

- Move database, Google auth, and Paystack secrets out of committed configuration and into environment variables or a secret store.
- Rotate any secrets that have already been checked into source control.
- Add separate production configuration values for cookie domain, HTTPS, callback URLs, and payment/webhook endpoints.

## Security

- Add password reset, email verification, and account recovery flows.
- Add audit logging for login attempts, admin changes, payment verification, and order status changes.
- Review all customer-facing POST actions for CSRF, replay, and ownership protections.
- Add webhook signature validation for payment callbacks if the payment provider supports it.

## Scalability and reliability

- Introduce a distributed cache and session store for multi-instance hosting.
- Move email sending, payment reconciliation, and other long-running work to background jobs.
- Add centralized log aggregation, alerts, uptime monitoring, and request tracing.
- Run database indexing review for the most common item, order, and customer queries.

## User experience

- Add empty states, inline validation messages, and clearer success/error messaging across admin and checkout flows.
- Add customer-facing order history filtering and search.
- Review mobile layouts for admin tables and forms with realistic production data volumes.

## Quality gates

- Expand the regression harness in `tests/EMS.Tests` into broader coverage for checkout, payment verification, and order lifecycle updates.
- Triage and reduce the current nullable warnings until the solution builds cleanly without warning noise.
