# Nexa — Email Template System

A reusable, brand-consistent template system for every transactional email Nexa
sends. One shared base layout (header, card, CTA button, footer) is composed with
small per-template content fragments, in English and Arabic, with an HTML body and
a plain-text alternative for every send. The first fully-built template is
**Email Confirmation**; the system is designed so adding the next one (Password
Reset, Welcome, Invitation, ...) means authoring files, not writing code.

Rendering (`IEmailTemplateService`) is deliberately kept separate from delivery
(`IEmailService`) — the renderer never knows about SMTP, and the SMTP sender never
knows about template files. See "Architecture" below.

## Directory structure

```
WebApi/EmailTemplates/
├── Layouts/
│   ├── base-en.html   ← shared HTML chrome, LTR (header, card, button, footer)
│   ├── base-ar.html   ← shared HTML chrome, RTL
│   ├── base-en.txt    ← shared plain-text chrome, LTR
│   └── base-ar.txt    ← shared plain-text chrome, RTL
├── EmailConfirmation/
│   ├── meta.json           ← static per-language copy (subject, title, button label, preview text)
│   ├── en-body.html         ← required: intro/explanation (English, HTML)
│   ├── en-secondary.html    ← optional: fallback link, expiry, security note (English, HTML)
│   ├── ar-body.html         ← required (Arabic, HTML)
│   ├── ar-secondary.html    ← optional (Arabic, HTML)
│   ├── en-body.txt          ← required (English, plain text)
│   ├── en-secondary.txt     ← optional (English, plain text)
│   ├── ar-body.txt          ← required (Arabic, plain text)
│   └── ar-secondary.txt     ← optional (Arabic, plain text)
└── {NextTemplateKey}/
    └── ... same shape
```

Everything lives under `WebApi/EmailTemplates/` (resolved from `IHostEnvironment.ContentRootPath`,
which is the `WebApi/` project directory) — not `wwwroot`. These are server-side
source files read directly off disk, never served as static assets.

## Architecture

- **`IEmailTemplateService`** (`Application/Interfaces/Services`) — the only contract
  Application code depends on. `RenderAsync(templateKey, language, placeholders, ct)`
  returns `(Subject, HtmlBody, PlainTextBody)`. It knows nothing about SMTP or any
  other transport.
- **`EmailTemplateService`** (`Infrastructure/Services/Email`) — the concrete
  file-based renderer described in this document. Swappable for a different engine
  later without touching a single job handler, because handlers only depend on the
  interface.
- **`IEmailService`** (`Application/Interfaces/Services`) — `SendAsync(to, subject,
  htmlBody, plainTextBody, attachments, ct)`. The only thing that knows how to
  actually deliver mail (currently `SmtpEmailService`, via `System.Net.Mail`, sending
  a real multipart/alternative message when `plainTextBody` is supplied).
- **Job handlers** (`Infrastructure/Jobs/Handlers/*Handler.cs`) — glue. Each one
  builds a small dictionary of *dynamic* data (a display name, a link, a date) from
  its background-job payload, calls `RenderAsync`, then `SendAsync`. No template
  markup, no styling, ever lives in a handler.

This is why a rebrand, a copy change, or a new template never touches C#: the
handler's job is only to supply *data*, not *presentation*.

## Design system

Values live inline in the layout files (email clients don't reliably support
external stylesheets or CSS variables) — this section is the single source of
truth for what they should be if you're editing a layout.

| Token | Value | Notes |
|---|---|---|
| Max content width | `600px` | The universal safe width across clients/devices |
| Page background | `#F4F5F7` | Sits behind the card |
| Card background | `#FFFFFF` | |
| Card border | `1px solid #E5E7EB` | Renders even where box-shadow is ignored |
| Card shadow | `0 1px 3px rgba(16,24,40,0.06)` | Cosmetic only — Outlook ignores it silently, never breaks |
| Card corner radius | `12px` | Some clients (older Outlook) render square corners instead — acceptable degrade |
| Primary accent | `#4F46E5` (indigo) | Button background, header accent dot, links |
| Heading text | `#111827` | |
| Body text | `#374151` | |
| Muted/footer text | `#6B7280` / `#9CA3AF` | |
| Secondary-section background | `#F9FAFB`, border `#E5E7EB` | The "note" card for security/help text |
| Font stack (LTR) | `-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif` | System fonts only — no web-font dependency |
| Font stack (RTL) | `Tahoma, Arial, 'Segoe UI', sans-serif` | Tahoma first: the most reliable high-quality Arabic glyph coverage on Windows/Outlook |
| Button | Solid fill, `8px` radius, `14px 36px` padding, `16px`/`600` weight text | Built with the table+padding technique — see "Email-client limitations" |

**Visual rhythm**: generous padding (`40px` inside the card, `24–36px` around it),
one accent color used sparingly (button + header dot + links only — never large
colored blocks), no gradients, no background images, no animation, no JavaScript.

## Placeholder reference

Two kinds of values flow through a render, and they're treated differently on
purpose:

| Kind | Examples | Escaped? | Set by |
|---|---|---|---|
| **Dynamic data** (per-send) | `DisplayName`, `ConfirmationLink`, `PrimaryButtonUrl`, `ChangeTime` | Yes, in HTML (`WebUtility.HtmlEncode`) — not in plain text | The job handler, via the `placeholders` dictionary passed to `RenderAsync` |
| **Static copy** (per-template, per-language) | `Title`, `Subject`, `Preheader`, `PrimaryButtonText` | N/A — authored HTML/text, not user data | `meta.json` |
| **Brand-level** (global) | `CompanyName`, `FooterContent`, `CurrentYear` | N/A | `EmailTemplateService` itself, from `EmailBrandingOptions` (`Email:Branding` config) |

Layout-level placeholders every `Layouts/base-{lang}.{html,txt}` file uses:

- `{{Title}}` — the heading at the top of the card.
- `{{Preheader}}` — the hidden inbox-preview snippet (HTML only; plain text has no
  equivalent concept and ignores this).
- `{{BodyContent}}` — the template's intro/explanation (from `{lang}-body.{ext}`).
- `{{PrimaryButtonText}}` — button label (from `meta.json`).
- `{{PrimaryButtonUrl}}` — button destination. **Always supplied by the handler**,
  never by a template file — this is the one reserved name that's genuinely dynamic
  data, not static copy, because the destination differs per send (a confirmation
  link, a reset link, an invite-accept link, ...).
- `{{SecondaryContent}}` — the muted "note" section (from `{lang}-secondary.{ext}`,
  optional; renders as an empty string if that file doesn't exist for a template).
- `{{FooterContent}}` — support contact line, computed once from
  `EmailBrandingOptions.SupportEmail` so every template's footer matches exactly.
- `{{CompanyName}}` — from `EmailBrandingOptions.CompanyName`. Available to
  `meta.json` copy too (e.g. a subject line can read `"Welcome to {{CompanyName}}"`).
- `{{CurrentYear}}` — computed at render time for the copyright line.

Any other key in a handler's `placeholders` dictionary (e.g. `{{DisplayName}}`,
`{{ConfirmationLink}}`) is available inside `{lang}-body.{ext}` and
`{lang}-secondary.{ext}` — substitute it there like any other token.

**There is no conditional or loop syntax** — this is deliberately a linear
`{{Key}}` → value string-replace, not a real templating engine (see
"Anti-over-engineering" below for why). Every layout placeholder must always
resolve to *something* (empty string is fine; a literal unreplaced `{{Token}}`
leaking into a sent email is not). Concretely: **every template must supply a
non-empty `PrimaryButtonUrl`** — v1's base layout always renders the button block,
so there is no "template with no call to action" case yet. If you need one, that's
a deliberate, documented layout change, not a workaround.

## `meta.json` schema

```json
{
  "SubjectEn": "Confirm your email address",
  "SubjectAr": "تأكيد بريدك الإلكتروني",
  "PreviewEn": "One quick step to activate your {{CompanyName}} account.",
  "PreviewAr": "خطوة سريعة واحدة لتفعيل حسابك.",
  "TitleEn": "Confirm your email address",
  "TitleAr": "تأكيد بريدك الإلكتروني",
  "ButtonTextEn": "Confirm My Email",
  "ButtonTextAr": "تأكيد بريدي الإلكتروني"
}
```

All eight fields are required. `{{CompanyName}}` (and any other brand-level
placeholder) can be used inside these strings — it's resolved before subject/preview
interpolation happens.

## Localization: RTL / LTR

Nexa supports English (LTR) and Arabic (RTL) end to end, not English-with-Arabic-
bolted-on:

- **Two physical layout files per format**, not one file with a direction toggle —
  `base-en.html`/`base-ar.html` (and the `.txt` equivalents). This is a deliberate
  choice: RTL email layout has real structural differences (text-align, the header's
  logo/dot ordering, margin direction) that are easier to get right and review as
  a dedicated file than as conditional CSS bolted onto a single "universal" template.
- `base-ar.html` sets `dir="rtl"` on both `<html>` and `<body>`, adds `dir="rtl"` to
  every table that carries directional content, and sets explicit `text-align:right`
  on text-bearing cells — Outlook in particular doesn't reliably infer text alignment
  from `dir` alone, so it's stated explicitly rather than assumed.
- The Arabic font stack leads with **Tahoma**, not the LTR system-font stack — Tahoma
  has the most reliable Arabic glyph coverage on Windows/Outlook, which is where
  custom/system fonts most often fail to render Arabic correctly.
- `SystemLanguageExtensions` (`Shared/Enums/System/SystemLanguageExtensions.cs`)
  centralizes `IsRightToLeft()` / `ToLanguageCode()` / `ToDirection()` /
  `FromLanguageCode()` — every place that needs to answer "is this Arabic?" or "what
  file suffix?" uses these, rather than re-deriving `isArabic ? "ar" : "en"` ad hoc
  (which is exactly how the pre-existing code had drifted before this pass).
- **Adding a third language later**: add `base-{code}.html`/`.txt`, extend
  `SystemLanguages` and `SystemLanguageExtensions`, and add the new language's
  fields to every template's `meta.json` (`TitleFr`, `SubjectFr`, ...) plus
  `{code}-body.{ext}` / `{code}-secondary.{ext}` files. No renderer code changes.

## How to add a new template

1. Pick the `templateKey` — it already exists as a constant in
   `Application/Common/Constants/JobTypes.cs` for every template listed as "future"
   in this system (`WelcomeEmail`, `PasswordResetEmail`, `PasswordChangedEmail`,
   `EmailChangeRequested`, `EmailChanged`, `OrganizationInvitationEmail`). Add a new
   constant there if it's genuinely new.
2. Create `WebApi/EmailTemplates/{TemplateKey}/meta.json` (see schema above).
3. Create the four content fragments: `en-body.html`, `en-body.txt`, `ar-body.html`,
   `ar-body.txt` (required), and `en-secondary.{html,txt}` / `ar-secondary.{html,txt}`
   (optional — omit the pair entirely if the template has no secondary/security note).
   Use `{{PlaceholderName}}` for any dynamic value the handler will supply.
4. In the corresponding job handler (`Infrastructure/Jobs/Handlers/*Handler.cs`),
   build the `placeholders` dictionary from the payload — **always include
   `PrimaryButtonUrl`** pointing at whatever link this email's action is (the other
   six existing handlers already do this for their respective links).
5. Call `templateService.RenderAsync(JobTypes.YourKey, payload.Language, placeholders, ct)`
   and pass all three return values into `emailService.SendAsync(to, subject, htmlBody, plainTextBody, ct: ct)`.
6. Add sample data for it to `EmailPreviewSampleData.For(...)`
   (`WebApi/Endpoints/Dev/EmailTemplatePreviewEndpoints.cs`) so it's previewable
   immediately.
7. Preview it (see below) in both languages before considering it done.

No step touches `EmailTemplateService.cs`, the base layouts, or any other
template's files.

## Preview process

A Development-only endpoint renders any template with realistic sample data —
**no email is ever sent**:

```
GET /dev/email-templates/{templateKey}?lang=en|ar&format=html|text
```

Examples:
- `http://localhost:5098/dev/email-templates/EmailConfirmation?lang=en&format=html` — view in a browser
- `http://localhost:5098/dev/email-templates/EmailConfirmation?lang=ar&format=html` — Arabic/RTL
- `curl "http://localhost:5098/dev/email-templates/EmailConfirmation?lang=en&format=html" > preview.html` — save to a file
- `?format=text` — the plain-text alternative

Only mapped when `app.Environment.IsDevelopment()` (`WebApi/Program.cs`) — there is
no route registered at all in Staging/Production, not just an auth check.

**Mobile preview**: there is deliberately one HTML file per language, not separate
desktop/mobile templates — responsiveness comes from the `@media (max-width: 600px)`
rules already in `base-{lang}.html` (tighter padding, full-width container). Resize
your browser window below 600px, or open the same preview URL on a phone, to see the
mobile layout; it's the same markup, not a different one.

## Email-client limitations (and how this system works around them)

| Limitation | Mitigation used here |
|---|---|
| No external CSS, unreliable `<style>` block support | Every structural/color style is inline. The `<style>` block only carries `@media` responsive rules and MSO (Outlook) resets — things that have no inline equivalent — as progressive enhancement, never as the only source of critical styling. |
| Outlook (Windows) uses the Word rendering engine — no `border-radius`, unreliable `box-shadow`, no flexbox/grid | Layout is 100% `<table>`-based. Rounded corners and shadow are decorative-only extras that degrade to square/flat, never break the layout. `<!--[if mso]>` conditional comments wrap an Outlook-specific fixed-width table fallback. |
| CSS Grid / Flexbox | Not used anywhere. |
| JavaScript | Not used, and could not be — virtually no mail client executes it. |
| Custom/web fonts | Not relied on. System font stacks only (see Design System), so text never falls back to a generic serif if a font fails to load. |
| Large images / background images | None used. The "logo" is a styled text wordmark plus a CSS-colored dot, not an image — it can never fail to load, is unaffected by images-blocked-by-default clients (the single biggest reason marketing-style HTML emails render broken), and needs no CDN. |
| Buttons via `<a>` + CSS padding vs. real `<button>` | Uses the widely-supported "bulletproof button" pattern: an outer `<table>` cell carrying the background color/radius, with the `<a>` inside carrying the padding — renders correctly (if with square corners on old Outlook) everywhere, without needing VML. |
| Dark mode / `prefers-color-scheme` | `<meta name="color-scheme" content="light">` and `supported-color-schemes` pin the template to light mode explicitly — several clients (notably Outlook/Windows Mail) otherwise auto-invert colors in ways that can wreck contrast (e.g. white-on-white). A deliberate, safe choice for v1; true dark-mode support is a separate, larger effort not attempted here. |
| Gmail clipping very long emails | Not a concern at this template's length; keep future templates reasonably concise for the same reason. |

## Security rules

- **Never log a raw token.** The confirmation/reset/invite link is built and handed
  to the email pipeline once; nothing in `EmailTemplateService`, the job handlers, or
  `SmtpEmailService` logs the rendered HTML, the link, or any placeholder value —
  only high-level structured fields (recipient, template key, job id) appear in logs
  elsewhere in the codebase. Don't add a `logger.LogDebug(htmlBody)`-style line while
  extending this system.
- **HTML-encode dynamic data, never static copy.** `EmailTemplateService` encodes
  every `placeholders` value (`WebUtility.HtmlEncode`) before substituting it into
  `{lang}-body.html` / `{lang}-secondary.html` — a display name containing `<` or `&`
  can't break the layout or inject markup. It does **not** encode `meta.json` copy or
  the already-rendered fragment when merging into the layout, because that's our own
  authored HTML, not user data — encoding it again would double-escape entities like
  `&amp;`. If you add a new reserved placeholder that carries user data, route it
  through the same escaped path, not the layout-merge phase.
- **No SVG uploads anywhere near this system.** Unrelated to email templates
  directly, but the same "don't render user-influenced markup unescaped" principle
  is why `UploadPolicies` (logo/profile picture) excludes SVG — an SVG can embed
  `<script>`/event-handler content.
- **Plain-text alternative isn't optional cosmetics.** Every send goes out as a real
  multipart/alternative message (`SmtpEmailService`) — beyond accessibility and
  text-only mail clients, HTML-only email is a well-known spam-score penalty.

## Anti-over-engineering note

This is a linear placeholder-substitution renderer, not a general templating
engine (no conditionals, no loops, no partial includes beyond the single
layout+body+secondary composition). That's intentional: Nexa's own architecture
rules push back on introducing a pluggable-engine abstraction (Scriban, Handlebars,
Razor, ...) without a concrete second-implementation need, and every template this
system needs to support today (Email Confirmation now; Password Reset, Welcome,
Invitation, ... later) fits the fixed "header / title / body / button / secondary /
footer" shape. If a future template genuinely needs real conditional logic (e.g. an
itemized payment receipt table with a variable number of rows), that's the signal to
revisit this decision — not before.
