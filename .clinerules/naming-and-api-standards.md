## Brief overview
This guideline outlines the project structure, file naming conventions, and API handling architecture for the AICC Mobile application.

## Naming Conventions
- Services must follow the suffix standard `.service.ts`. For example, authentication service should be named `auth.service.ts` rather than `authService.ts`.
- Ensure consistency in module and folder naming by adhering to standard lowercase with hyphens or dots as applicable.

## API Architecture and Reuse
- Avoid creating redundant Axios instances (e.g., `axiosInstance.ts`).
- Always reuse and extend the central API client located in `aicc_mobile/src/api/axiosClient.ts`.
- Implement authentication interceptors, automatic refresh token logic (401 interceptor), and headers handling directly inside the existing `axiosClient.ts`.
- Token state should be synchronized with the global store (e.g. `useStore` inside `aicc_mobile/src/store/useAuthStore.ts`) to make it instantly accessible for the interceptor.

## Communication Style
- Comments in the code must be in Vietnamese, brief, and highly technical.
- Do not explain theoretical concepts in responses; focus purely on clean, production-ready code with concise inline comments.