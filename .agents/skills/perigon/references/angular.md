# Angular Reference

This reference belongs to the Perigon skill and covers Angular 21+ frontend work for this template.

## When to use
- Build CRUD pages, forms, routes, shared components, and Material-based layouts.
- Generate typed request clients from backend OpenAPI through Perigon.
- Implement or review frontend changes in a Perigon-based project.

## Project structure

```sh
src/
  ├── main.ts
  ├── app/
  │   ├── app.config.ts
  │   ├── app.routes.ts
  │   ├── layout/
  │   ├── pages/
  │   ├── share/
  │   │   ├── components/
  │   │   ├── pipe/
  │   │   ├── auth.guard.ts
  │   │   ├── custom-paginator-intl.ts
  │   │   └── i18n-keys.ts
  │   └── services/
  ├── assets/i18n/
  ├── environments/
  ├── styles/
  └── proxy.conf.json
```

## Core rules
- Prefer standalone components and Angular Material.
- Prefer signals and typed forms.
- Use i18n keys instead of hard-coded user-facing strings.
- Keep generated request contracts untouched and regenerate them when the backend changes.
- Avoid inline styles and prefer shared styles / Material tokens.

## Recommended workflow
1. Generate or refresh request clients from backend OpenAPI through Perigon.
2. Create the page or component structure.
3. Configure route/menu and shared UI pieces.
4. Implement the TS / HTML / SCSS logic and validate the interaction states.
5. Run `pnpm build` (and optionally `pnpm start`) to verify the result.

## UX and validation checklist
- Check layout density, empty / loading / error states, and responsive behavior.
- Prefer Material components and existing theme styles over custom visual overrides.
- Keep user-visible strings in i18n and avoid hard-coded text.

## Verification
- Run `pnpm build` after frontend changes.
- Prefer `pnpm start` for live validation when the page is non-trivial.
