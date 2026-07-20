# React/React Native Performance Optimization Rule

Do NOT prematurely or blindly overuse `useMemo` and `useCallback`. Avoid wrapping every function or computed value unless strictly necessary. It degrades readability and introduces overhead for shallow dependency checks.

## 🛑 When NOT to use (Avoid wrapping):
1. **Simple Handlers:** Do not wrap simple state setters, toggles, or direct event handlers (e.g., `const toggle = () => setIsOpen(!isOpen)`).
2. **Primitive Values / Cheap Calculations:** Do not wrap primitive values, basic string concatenations, or simple math operations.
3. **Un-memoized Children:** Do not wrap callbacks passed to native components (like `<View>`, `<TextInput>`, `<TouchableOpacity>`) or custom components that are NOT optimized with `React.memo`.
4. **Initial Render Logic:** Do not wrap values that only run once or don't trigger heavy re-renders.

## 🟢 When to use (Strictly Allowed):
1. **Memoized Child Components:** Passing props to heavy child components that are explicitly wrapped in `React.memo` or extend `PureComponent`, where reference equality checks (`===`) actually prevent re-renders.
2. **Hook Dependencies:** The function or value is required in the dependency array of other hooks (like `useEffect`, `useMemo`, or other `useCallback`).
3. **Expensive Computations:** The data transformation involves heavy processing, such as filtering, sorting, or mapping over a large array (e.g., > 100 items).

## 🤖 Enforcement Action:
- When writing or refactoring React/React Native code, audit every hook. If a `useMemo` or `useCallback` does not meet the "When to use" criteria, write plain functions/variables instead.
- If removing existing hooks, ensure you don't break dependencies of other active `useEffect` hooks.