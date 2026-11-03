# Email

Account email (sign-up codes, recovery links, staff invitations) goes out as **`app@baptist.com.au`**, a
Microsoft 365 shared mailbox, through Microsoft Graph. Set up and tested end to end on 2026-11-02.

## How it flows

A feature calls `IEmailSender`, which writes a row to `email.outgoing_emails` and wakes the background
sender (`EmailDelivery`). The sender hands due rows to the `IEmailTransport` and retries failures with
backoff (1, 2, 4 … 256 minutes, then every six hours, giving up after 40 attempts with a `Critical`
log). A Graph outage or a lapsed certificate delays email and never loses it.

The transport is chosen at startup:

| `Email:Graph` configured? | Environment | Transport |
|---|---|---|
| yes | any | `GraphEmailTransport`: real email, a copy in the mailbox's Sent Items |
| no | Development | `DevelopmentEmailTransport`: the email is written to the API's log |
| no | otherwise | `UnconfiguredEmailTransport`: emails wait in the table until it is configured |

## Configuration

| Key | Value |
|---|---|
| `Email:Graph:TenantId` | the tenant's Directory (tenant) ID |
| `Email:Graph:ClientId` | the `GSBC Church App mailer` app registration's Application (client) ID |
| `Email:Graph:Sender` | `app@baptist.com.au` |
| `Email:Graph:Certificate` | PEM: the certificate followed by its unencrypted private key |

None of these go in the repo. **Development:** the AppHost's user-secrets (`--id
gsbc-churchapp-apphost`); the AppHost passes every `Email:Graph:*` value to the API, so restart it after
changing them. Removing them puts development back on the log. **Production:** a cluster secret through
`gsbc.argo`, as environment variables `Email__Graph__TenantId`, `__ClientId`, `__Sender` and
`__Certificate`.

## The Microsoft 365 side

Done once by a tenant admin, following GSBC.Accounting's `expenses@` setup:

1. **Shared mailbox** `app@baptist.com.au` (display name `GSBC App`), sign-in blocked. No licence.
2. **App registration** `GSBC Church App mailer`, single tenant, **no API permissions**. Never grant
   `Mail.Send` in Entra: it covers every mailbox in the tenant, and Exchange scoping cannot take it away.
3. **Two certificates** on the registration, `development` and `production`, each self-signed and valid
   for two years, so either can be revoked alone. Only the public `.crt` is uploaded; the private keys
   stay with the owner. Made with:

   ```bash
   openssl req -x509 -newkey rsa:2048 -sha256 -days 730 -nodes \
     -subj "/CN=GSBC Church App mailer (prod)" -keyout mailer-prod.key -out mailer-prod.crt
   ```

4. **Exchange's RBAC for Applications**, scoped to the one mailbox:

   ```powershell
   New-ServicePrincipal -AppId <client id> -ObjectId <ENTERPRISE APPLICATION object id> -DisplayName 'GSBC Church App mailer'
   New-ManagementScope -Name 'GSBC Church App mailbox' -RecipientRestrictionFilter "PrimarySmtpAddress -eq 'app@baptist.com.au'"
   New-ManagementRoleAssignment -App <client id> -Role 'Application Mail.Send' -CustomResourceScope 'GSBC Church App mailbox'
   Test-ServicePrincipalAuthorization -Identity <client id> -Resource app@baptist.com.au     # InScope True
   Test-ServicePrincipalAuthorization -Identity <client id> -Resource <any other mailbox>    # InScope False
   ```

## UAT

UAT sends real email too, with the mailer's **development** certificate, so either environment can be
revoked alone: `email-secret` in the `church-app-uat` namespace, the same four keys
(`gsbc.argo`'s `clusters/mini/README.md`). It's optional, as below. Links in UAT's emails go to
`https://uat-app.baptist.com.au` (`Accounts:SiteUrl`).

## Going to production (later)

Not done yet: the API isn't on the cluster. When it goes there through `gsbc.argo`:

1. Create a secret in the API's namespace with the four values. The certificate value is
   `mailer-prod.crt` followed by `mailer-prod.key` (kept in the owner's password manager):

   ```bash
   kubectl -n <namespace> create secret generic email-secret \
     --from-literal=Email__Graph__TenantId='<tenant id>' \
     --from-literal=Email__Graph__ClientId='<client id>' \
     --from-literal=Email__Graph__Sender='app@baptist.com.au' \
     --from-literal=Email__Graph__Certificate="$(cat mailer-prod.crt mailer-prod.key)"
   ```

2. Give the API deployment that secret as environment variables (`envFrom`, `optional: true`: without
   it the API still starts and emails wait in the table until it exists).
3. Restart the API, send an invitation, and check `email.outgoing_emails` (below).

Nothing changes on the Microsoft 365 side: the production certificate is already on the registration.

## Renewing a certificate

Before the production certificate expires (late 2028; Entra shows the date), make a new one with the
command above, upload its `.crt` beside the old one, replace `Email__Graph__Certificate` in the cluster
secret, restart the API, and once email flows, delete the old certificate in Entra. Workload identity
federation (Entra trusting the cluster's service-account tokens) would remove this chore; it needs the
k3s service-account issuer published over HTTPS, and is a possible later change.

## When an email has not arrived

```sql
select id, "to", subject, sent_at, attempts, next_attempt_at, last_error
from email.outgoing_emails order by id desc limit 20;
```

| `last_error` contains | Means |
|---|---|
| `ErrorAccessDenied` | The Exchange scoping is missing, still propagating (up to two hours), or names another address than `Email:Graph:Sender` |
| `AADSTS700027` | The certificate isn't on the registration, has expired, or its private key doesn't match |
| `AADSTS700016` | The client or tenant ID is wrong |
| `ErrorInvalidUser` | `Email:Graph:Sender` doesn't exist in the tenant |
| a PEM or `CryptographicException` message | `Email:Graph:Certificate` isn't the certificate followed by its private key |

To resend after fixing the cause: `update email.outgoing_emails set attempts = 0, next_attempt_at =
now(), gave_up_at = null where id = …`.
