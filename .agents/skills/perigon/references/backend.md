# Backend Reference

This reference belongs to the Perigon skill and covers the Perigon backend architecture used by this template.

## When to use
- Design entities, managers, DTOs, controllers, and service defaults.
- Wire business logic, migrations, and service-layer structure.
- Implement or review backend logic in a Perigon-based solution.


## Core rules
- Keep business logic in managers instead of controllers.
- Prefer `ManagerBase<T>` / `ManagerBase` and `RestControllerBase` patterns.
- Keep controller layers focused on input validation, authorization, and HTTP response shaping.
- Use shared utilities from `src/Perigon` before adding local duplicates.
- Use Aspire and service defaults where relevant to the runtime model.
- Avoid direct `DbContext` access in controllers; prefer manager or factory-based flows.

## Development flow
1. Define entities, DbContext, and shared services.
2. Implement modules and managers / DTOs.
3. Implement controllers and API endpoints.
4. Run build validation (`dotnet build`) for the affected project or solution.
5. If entities are changed, review the existing migration workflow and scripts instead of manually altering migrations.

## Verification
- Run `dotnet build` after backend changes.
- If the change touches the domain model, review migration and startup behavior before concluding.
